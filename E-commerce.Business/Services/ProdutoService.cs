using Dunder_Store.Entities;
using Dunder_Store.Interfaces.IRepositories;
using Dunder_Store.Interfaces.IServices;

namespace Dunder_Store.Services
{
    public class ProdutoService : IProdutoService
    {
        private readonly IProdutoRepository _produtoRepository;
        private readonly IPedidoProdutoRepository _pedidoProdutoRepository;

        public ProdutoService(IProdutoRepository produtoRepository, IPedidoProdutoRepository pedidoProdutoRepository)
        {
            _produtoRepository = produtoRepository;
            _pedidoProdutoRepository = pedidoProdutoRepository;
        }

        public async Task<Paginador<Produto>> GetAllAsync(string? nome = null, string? cor = null, string? tamanho = null,
            string? categoria = null, int pagina = 1, int itensPorPagina = 10)
        {
            return await _produtoRepository.GetAllAsync(nome, cor, tamanho, categoria, pagina, itensPorPagina);
        }

        public async Task<Produto?> GetByIdAsync(Guid id) => await _produtoRepository.GetByIdAsync(id);

        public async Task<Produto> CriarProdutoAsync(Produto produto)
        {
            if (produto.ProdutoPaiId.HasValue)
            {
                var pai = await _produtoRepository.GetByIdAsync(produto.ProdutoPaiId.Value);
                if (pai == null) throw new Exception("Produto pai não existe.");
            }

            await _produtoRepository.AddAsync(produto);
            return produto;
        }

        public async Task AtualizarProdutoAsync(Produto produto)
        {
            var existente = await _produtoRepository.GetByIdAsync(produto.Id);
            if (existente == null) throw new Exception("Produto não encontrado.");

            existente.Nome = produto.Nome ?? existente.Nome;
            existente.Descricao = produto.Descricao ?? existente.Descricao;
            existente.Preco = produto.Preco > 0 ? produto.Preco : existente.Preco;
            existente.CodigoDeBarra = produto.CodigoDeBarra ?? existente.CodigoDeBarra;
            existente.Cor = produto.Cor ?? existente.Cor;
            existente.Tamanho = produto.Tamanho ?? existente.Tamanho;
            existente.CategoriaId = produto.CategoriaId != Guid.Empty ? produto.CategoriaId : existente.CategoriaId;

            await _produtoRepository.UpdateAsync(existente);

            if (existente.Variacoes != null && existente.Variacoes.Any())
            {
                foreach (var v in existente.Variacoes)
                {
                    v.CategoriaId = existente.CategoriaId;
                    v.Preco = existente.Preco;
                    await _produtoRepository.UpdateAsync(v);
                }
            }
        }

        public async Task RemoverTodosProdutosAsync()
        {
            var todosProdutos = await _produtoRepository.GetAllAsync(pagina: 1, itensPorPagina: int.MaxValue);

            foreach (var produto in todosProdutos.Itens)
            {
                // Remove relações em pedidos
                await _pedidoProdutoRepository.RemoverPorProdutoIdAsync(produto.Id);

                if (produto.Variacoes != null)
                {
                    foreach (var v in produto.Variacoes)
                    {
                        await _pedidoProdutoRepository.RemoverPorProdutoIdAsync(v.Id);
                        await _produtoRepository.DeleteAsync(v);
                    }
                }

                await _produtoRepository.DeleteAsync(produto);
            }
        }

        public async Task RemoverProdutoAsync(Guid id)
        {
            var produto = await _produtoRepository.GetByIdAsync(id);
            if (produto == null) throw new Exception("Produto não encontrado.");

            // Remove relações em pedidos para o produto e suas variações
            await _pedidoProdutoRepository.RemoverPorProdutoIdAsync(produto.Id);

            if (produto.Variacoes != null)
            {
                foreach (var v in produto.Variacoes)
                {
                    await _pedidoProdutoRepository.RemoverPorProdutoIdAsync(v.Id);
                    await _produtoRepository.DeleteAsync(v);
                }
            }

            await _produtoRepository.DeleteAsync(produto);
        }
    }
}
