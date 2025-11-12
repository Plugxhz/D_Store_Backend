using Dunder_Store.DTO;
using Dunder_Store.Entities;
using Dunder_Store.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dunder_Store.Database;
using Microsoft.AspNetCore.Authorization;

namespace Dunder_Store.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProdutoController : ControllerBase
    {
        private readonly IProdutoService _produtoService;
        private readonly ProdutosDbContext _dbContext;

        public ProdutoController(IProdutoService produtoService, ProdutosDbContext dbContext)
        {
            _produtoService = produtoService;
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<Paginador<Produto>>> GetAllProdutos(
            string? nome = null,
            string? cor = null,
            string? tamanho = null,
            string? categoria = null,
            int pagina = 1,
            int itensPorPagina = 10)
        {
            var produtos = await _produtoService.GetAllAsync(nome, cor, tamanho, categoria, pagina, itensPorPagina);
            if (produtos == null) return NotFound("Nenhum produto encontrado.");
            return Ok(produtos);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<Produto>> GetProdutoById(Guid id)
        {
            var produto = await _produtoService.GetByIdAsync(id);
            if (produto is null) return NotFound("Produto não encontrado.");
            return Ok(produto);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Produto>> CreateProduto([FromForm] ProdutoDTO novoProdutoDTO)
        {
            if (novoProdutoDTO.imagem == null || novoProdutoDTO.imagem.Length == 0)
                return BadRequest("Imagem obrigatória.");

            var imagem = novoProdutoDTO.imagem;

            if (novoProdutoDTO.CategoriaId == null && !string.IsNullOrEmpty(novoProdutoDTO.CategoriaNome))
            {
                var categoria = await _dbContext.Categorias
                    .FirstOrDefaultAsync(c => c.Nome.ToLower() == novoProdutoDTO.CategoriaNome.ToLower());

                if (categoria == null)
                    return BadRequest($"Categoria '{novoProdutoDTO.CategoriaNome}' não existe.");

                novoProdutoDTO.CategoriaId = categoria.Id;
            }

            if (novoProdutoDTO.CategoriaId == null)
                return BadRequest("Categoria obrigatória. Informe o nome ou o ID.");

            string nomePasta = "produtos";
            string caminhoDaPasta = Path.Combine("wwwroot", nomePasta);
            Directory.CreateDirectory(caminhoDaPasta);

            string nomeArquivo = $"{Guid.NewGuid()}{Path.GetExtension(imagem.FileName)}";
            string caminhoCompleto = Path.Combine(caminhoDaPasta, nomeArquivo);

            using var stream = new FileStream(caminhoCompleto, FileMode.Create);
            await imagem.CopyToAsync(stream);

            string urlServidor = $"{Request.Scheme}://{Request.Host}";
            string imagemUrl = $"{urlServidor}/{nomePasta}/{nomeArquivo}";

            var novoProduto = new Produto
            {
                Nome = novoProdutoDTO.nome,
                Descricao = novoProdutoDTO.descricao,
                Preco = novoProdutoDTO.preco,
                CodigoDeBarra = novoProdutoDTO.codigoDeBarra,
                ImagemURL = imagemUrl,
                CategoriaId = novoProdutoDTO.CategoriaId.Value,
                Cor = novoProdutoDTO.cor,
                Tamanho = novoProdutoDTO.tamanho,
                ProdutoPaiId = novoProdutoDTO.produtoPaiId
            };

            var produtoCriado = await _produtoService.CriarProdutoAsync(novoProduto);
            return CreatedAtAction(nameof(GetProdutoById), new { id = produtoCriado.Id }, produtoCriado);
        }




        [HttpPatch("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduto(Guid id, [FromForm] ProdutoPatchDTO dto, IFormFile? novaImagem)
            {
            var produto = await _produtoService.GetByIdAsync(id);
            if (produto is null)
                return NotFound("Produto não encontrado.");

            // Atualiza categoria se foi informada pelo nome
            if (dto.CategoriaId == null && !string.IsNullOrEmpty(dto.CategoriaNome))
            {
                var categoria = await _dbContext.Categorias
                    .FirstOrDefaultAsync(c => c.Nome.ToLower() == dto.CategoriaNome.ToLower());

                if (categoria == null)
                    return BadRequest($"Categoria '{dto.CategoriaNome}' não existe.");

                dto.CategoriaId = categoria.Id;
            }

            // Atualiza campos básicos
            produto.Nome = dto.nome ?? produto.Nome;
            produto.Descricao = dto.descricao ?? produto.Descricao;
            produto.Preco = dto.preco.HasValue ? dto.preco.Value : produto.Preco;
            produto.CodigoDeBarra = dto.codigoDeBarra ?? produto.CodigoDeBarra;
            produto.Cor = dto.cor ?? produto.Cor;
            produto.Tamanho = dto.tamanho ?? produto.Tamanho;
            produto.CategoriaId = dto.CategoriaId ?? produto.CategoriaId;
            produto.ProdutoPaiId = dto.produtoPaiId ?? produto.ProdutoPaiId;

            // 🔹 Se foi enviada uma nova imagem, substitui a antiga
            if (novaImagem != null && novaImagem.Length > 0)
            {
                string nomePasta = "produtos";
                string caminhoDaPasta = Path.Combine("wwwroot", nomePasta);
                Directory.CreateDirectory(caminhoDaPasta);

                // Deleta imagem antiga, se existir
                if (!string.IsNullOrEmpty(produto.ImagemURL))
                {
                    var nomeAntigo = Path.GetFileName(new Uri(produto.ImagemURL).AbsolutePath);
                    var caminhoAntigo = Path.Combine(caminhoDaPasta, nomeAntigo);
                    if (System.IO.File.Exists(caminhoAntigo))
                        System.IO.File.Delete(caminhoAntigo);
                }

                // Salva nova imagem
                string nomeArquivo = $"{Guid.NewGuid()}{Path.GetExtension(novaImagem.FileName)}";
                string caminhoCompleto = Path.Combine(caminhoDaPasta, nomeArquivo);

                using var stream = new FileStream(caminhoCompleto, FileMode.Create);
                await novaImagem.CopyToAsync(stream);

                string urlServidor = $"{Request.Scheme}://{Request.Host}";
                produto.ImagemURL = $"{urlServidor}/{nomePasta}/{nomeArquivo}";
            }

            await _produtoService.AtualizarProdutoAsync(produto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduto(Guid id)
        {
            await _produtoService.RemoverProdutoAsync(id);
            return NoContent();
        }

        [HttpDelete("todos")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTodosProdutos()
        {
            await _produtoService.RemoverTodosProdutosAsync();
            return NoContent();
        }


    }
}
