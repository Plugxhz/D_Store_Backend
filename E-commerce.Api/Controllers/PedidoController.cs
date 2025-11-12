using Dunder_Store.Database;
using Dunder_Store.DTO;
using Dunder_Store.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Dunder_Store.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PedidoController : ControllerBase
    {
        private readonly ProdutosDbContext dbContext;

        private Guid? GetLoggedClienteId()
        {
            var id = User?.FindFirst("Id")?.Value ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(id, out var guid)) return guid;
            return null;
        }

        public PedidoController(ProdutosDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Paginador<Pedido>>> GetPedidos(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanhoPagina = 10)
        {
            if (pagina <= 0) pagina = 1;
            if (tamanhoPagina <= 0) tamanhoPagina = 10;

            var totalItens = await dbContext.Pedidos.CountAsync();

            var pedidos = await dbContext.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.PedidoProdutos).ThenInclude(pp => pp.Produto)
                .OrderByDescending(p => p.DataPedido)
                .Skip((pagina - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .ToListAsync();

            if (!pedidos.Any())
                return NoContent();

            var resultado = new Paginador<Pedido>(pedidos, totalItens, pagina, tamanhoPagina);
            return Ok(resultado);
        }

        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> GetPedido(Guid id)
        {
            var pedido = await dbContext.Pedidos
                .Include(p => p.PedidoProdutos).ThenInclude(pp => pp.Produto)
                .Include(p => p.Cliente)
                .Include(p => p.Cupom)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
                return NotFound("Pedido não encontrado.");

            var retorno = new
            {
                pedido.Id,
                pedido.ClienteId,
                ClienteNome = pedido.Cliente.Nome,
                pedido.Status,
                DataPedido = pedido.DataPedido,
                Produtos = pedido.PedidoProdutos.Select(pp => new
                {
                    pp.ProdutoId,
                    pp.Produto.Nome,
                    pp.Produto.Preco,
                    pp.Quantidade,
                    ValorTotalProduto = pp.ValorTotal
                }),
                ValorTotalSemDesconto = pedido.ValorTotalSemDesconto,
                Cupom = pedido.Cupom != null ? new
                {
                    pedido.Cupom.Codigo,
                    pedido.Cupom.DescontoPercentual,
                    pedido.Cupom.DataExpiracao
                } : null,
                ValorTotalComDesconto = pedido.ValorTotal
            };

            return Ok(retorno);
        }


        [HttpGet("cliente/{clienteId:guid}")]
        [Authorize(Roles = "Cliente,Admin")]
        public async Task<ActionResult<Paginador<Pedido>>> GetPedidosPorCliente(
            Guid clienteId,
            [FromQuery] PedidoStatus? status = null,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanhoPagina = 10)
        {
            if (User.IsInRole("Cliente"))
            {
                var userId = GetLoggedClienteId();
                if (userId == null || userId.Value != clienteId)
                    return Forbid();
            }
            var cliente = await dbContext.Clientes.FindAsync(clienteId);
            if (cliente == null)
                return BadRequest("Cliente não encontrado.");

            var query = dbContext.Pedidos
                .Include(p => p.PedidoProdutos).ThenInclude(pp => pp.Produto)
                .Include(p => p.Cliente)
                .Where(p => p.ClienteId == clienteId);

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            var totalItens = await query.CountAsync();

            var pedidos = await query
                .OrderByDescending(p => p.DataPedido)
                .Skip((pagina - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .ToListAsync();

            if (!pedidos.Any())
                return NoContent();

            var resultado = new Paginador<Pedido>(pedidos, totalItens, pagina, tamanhoPagina);
            return Ok(resultado);
        }

        [HttpPost]
        [Authorize(Roles = "Cliente,Admin")]
        public async Task<ActionResult<Pedido>> CreatePedido(PedidoDTO novoPedidoDTO)
        {
            if (novoPedidoDTO.produtos == null || !novoPedidoDTO.produtos.Any())
                return BadRequest("É necessário enviar a lista de produtos.");

            var cliente = await dbContext.Clientes.FirstOrDefaultAsync(c => c.Cpf == novoPedidoDTO.clientecpf);
            if (cliente == null)
                return BadRequest("Cliente inválido.");

            if (User.IsInRole("Cliente"))
            {
                var userId = GetLoggedClienteId();
                if (userId == null || userId.Value != cliente.Id)
                    return Forbid();
            }

            var codigos = novoPedidoDTO.produtos.Select(p => p.CodigoDeBarra).ToList();
            var produtosEncontrados = await dbContext.Produtos.Where(p => codigos.Contains(p.CodigoDeBarra)).ToListAsync();
            if (produtosEncontrados.Count != novoPedidoDTO.produtos.Count)
                return BadRequest("Um ou mais produtos não foram encontrados.");

            var novoPedido = new Pedido(cliente, DateTime.Now)
            {
                Status = PedidoStatus.Carrinho
            };

            // adicionar produtos
            foreach (var produtoDTO in novoPedidoDTO.produtos)
            {
                var produto = produtosEncontrados.FirstOrDefault(p => p.CodigoDeBarra == produtoDTO.CodigoDeBarra);
                if (produto != null)
                {
                    novoPedido.PedidoProdutos.Add(new PedidoProduto
                    {
                        ProdutoId = produto.Id,
                        Produto = produto,
                        Quantidade = produtoDTO.Quantidade
                    });
                }
            }

            // aplicar cupom
            if (!string.IsNullOrWhiteSpace(novoPedidoDTO.cupomCodigo))
            {
                var cupom = await dbContext.Cupons.FirstOrDefaultAsync(c =>
                    c.Codigo.ToUpper() == novoPedidoDTO.cupomCodigo.Trim().ToUpper() &&
                    c.Ativo && c.DataExpiracao >= DateTime.Now);

                if (cupom == null)
                    return BadRequest("Cupom inválido ou expirado.");

                novoPedido.CupomId = cupom.Id;
                novoPedido.Cupom = cupom;
            }

            await dbContext.Pedidos.AddAsync(novoPedido);
            await dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPedido), new { id = novoPedido.Id }, novoPedido);
        }


        [HttpGet("carrinho/{clienteId:guid}")]
        [Authorize(Roles = "Cliente,Admin")]
        public async Task<ActionResult<object>> GetCarrinhoPorCliente(Guid clienteId)
        {
            if (User.IsInRole("Cliente"))
            {
                var userId = GetLoggedClienteId();
                if (userId == null || userId.Value != clienteId)
                    return Forbid();
            }
            var carrinho = await dbContext.Pedidos
                .Include(p => p.PedidoProdutos).ThenInclude(pp => pp.Produto)
                .Include(p => p.Cliente)
                .Include(p => p.Cupom)
                .FirstOrDefaultAsync(p => p.ClienteId == clienteId && p.Status == PedidoStatus.Carrinho);

            if (carrinho == null)
            {
                var cliente = await dbContext.Clientes.FindAsync(clienteId);
                if (cliente == null)
                    return NotFound("Cliente não encontrado.");

                carrinho = new Pedido(cliente, DateTime.Now)
                {
                    Status = PedidoStatus.Carrinho
                };

                await dbContext.Pedidos.AddAsync(carrinho);
                await dbContext.SaveChangesAsync();
            }

            // Montar retorno customizado
            var retorno = new
            {
                carrinho.Id,
                carrinho.ClienteId,
                ClienteNome = carrinho.Cliente.Nome,
                carrinho.Status,
                Produtos = carrinho.PedidoProdutos.Select(pp => new
                {
                    pp.ProdutoId,
                    pp.Produto.Nome,
                    pp.Produto.Preco,
                    pp.Quantidade,
                    ValorTotalProduto = pp.ValorTotal
                }),
                ValorTotalSemDesconto = carrinho.ValorTotalSemDesconto,
                Cupom = carrinho.Cupom != null ? new
                {
                    carrinho.Cupom.Codigo,
                    carrinho.Cupom.DescontoPercentual,
                    carrinho.Cupom.DataExpiracao
                } : null,
                ValorTotalComDesconto = carrinho.ValorTotal
            };

            return Ok(retorno);
        }

        [HttpGet("receita-mensal")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<decimal>> GetReceitaMensal()
        {
            var agora = DateTime.Now;
            var inicioMes = new DateTime(agora.Year, agora.Month, 1);
            var fimMes = inicioMes.AddMonths(1);

            var pedidosFinalizados = await dbContext.Pedidos
                .Include(p => p.PedidoProdutos)
                .ThenInclude(pp => pp.Produto)
                .Where(p => p.Status == PedidoStatus.Finalizado &&
                            p.DataPedido >= inicioMes &&
                            p.DataPedido < fimMes)
                .ToListAsync();

            // Aqui soma em memória, já que ValorTotal é calculado
            var receita = pedidosFinalizados.Sum(p => p.ValorTotal);

            return Ok(receita);
        }

        [HttpGet("produtos-mais-vendidos")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<object>>> GetProdutosMaisVendidos()
        {
            var produtosMaisVendidos = await dbContext.PedidoProdutos
                .Where(pp => pp.Pedido.Status == PedidoStatus.Finalizado)
                .GroupBy(pp => new { pp.ProdutoId, pp.Produto.Nome, pp.Produto.Preco })
                .Select(g => new
                {
                    ProdutoId = g.Key.ProdutoId,
                    Nome = g.Key.Nome,
                    Preco = g.Key.Preco,
                    QuantidadeVendida = g.Sum(x => x.Quantidade)
                })
                .OrderByDescending(x => x.QuantidadeVendida)
                .Take(5)
                .ToListAsync();

            return Ok(produtosMaisVendidos);
        }



        [HttpPost("finalizar/{id:guid}")]
        [Authorize(Roles = "Cliente,Admin")]
        public async Task<IActionResult> FinalizarPedido(Guid id)
        {
            var pedido = await dbContext.Pedidos.FindAsync(id);
            if (pedido == null)
                return NotFound("Pedido não encontrado.");

            if (User.IsInRole("Cliente"))
            {
                var userId = GetLoggedClienteId();
                if (userId == null || pedido.ClienteId != userId.Value)
                    return Forbid();
            }

            if (pedido.Status != PedidoStatus.Carrinho)
                return BadRequest("Pedido já está finalizado.");

            pedido.Status = PedidoStatus.Finalizado;
            pedido.DataPedido = DateTime.Now;

            await dbContext.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Cliente,Admin")]
        public async Task<IActionResult> UpdatePedido(Guid id, PedidoDTO pedidoAtualizadoDTO)
        {
            var pedido = await dbContext.Pedidos
                .Include(p => p.PedidoProdutos).ThenInclude(pp => pp.Produto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
                return NotFound("Pedido não encontrado.");

            if (User.IsInRole("Cliente"))
            {
                var userId = GetLoggedClienteId();
                if (userId == null || pedido.ClienteId != userId.Value)
                    return Forbid();
            }

            if (pedido.Status != PedidoStatus.Carrinho)
                return BadRequest("Não é possível alterar um pedido que já foi finalizado.");

            if (pedidoAtualizadoDTO.produtos == null || !pedidoAtualizadoDTO.produtos.Any())
                return BadRequest("É necessário enviar a lista de produtos.");

            dbContext.PedidoProdutos.RemoveRange(pedido.PedidoProdutos);
            pedido.PedidoProdutos.Clear();

            var codigos = pedidoAtualizadoDTO.produtos.Select(p => p.CodigoDeBarra).ToList();
            var produtosEncontrados = await dbContext.Produtos.Where(p => codigos.Contains(p.CodigoDeBarra)).ToListAsync();

            foreach (var produtoDTO in pedidoAtualizadoDTO.produtos)
            {
                var produto = produtosEncontrados.FirstOrDefault(p => p.CodigoDeBarra == produtoDTO.CodigoDeBarra);
                if (produto != null)
                {
                    pedido.PedidoProdutos.Add(new PedidoProduto
                    {
                        Produto = produto,
                        Quantidade = produtoDTO.Quantidade
                    });
                }
            }

            await dbContext.SaveChangesAsync();
            return NoContent();
        }

        [HttpPatch("{id:guid}/cupom")]
        [Authorize(Roles = "Cliente,Admin")]
        public async Task<IActionResult> AtualizarCupom(Guid id, [FromForm] string cupomCodigo)
        {
            var pedido = await dbContext.Pedidos
                .Include(p => p.Cupom)
                .Include(p => p.PedidoProdutos).ThenInclude(pp => pp.Produto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
                return NotFound("Pedido não encontrado.");

            if (User.IsInRole("Cliente"))
            {
                var userId = GetLoggedClienteId();
                if (userId == null || pedido.ClienteId != userId.Value)
                    return Forbid();
            }

            if (pedido.Status != PedidoStatus.Carrinho)
                return BadRequest("Não é possível alterar um pedido já finalizado.");

            if (string.IsNullOrWhiteSpace(cupomCodigo))
            {
                pedido.CupomId = null;
                pedido.Cupom = null;
            }
            else
            {
                var cupom = await dbContext.Cupons.FirstOrDefaultAsync(c =>
                    c.Codigo.ToUpper() == cupomCodigo.Trim().ToUpper() &&
                    c.Ativo && c.DataExpiracao >= DateTime.Now);

                if (cupom == null)
                    return BadRequest("Cupom inválido ou expirado.");

                pedido.CupomId = cupom.Id;
                pedido.Cupom = cupom;
            }

            await dbContext.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePedido(Guid id)
        {
            var pedido = await dbContext.Pedidos
                .Include(p => p.PedidoProdutos)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
                return NotFound("Pedido não encontrado.");

            dbContext.PedidoProdutos.RemoveRange(pedido.PedidoProdutos);
            dbContext.Pedidos.Remove(pedido);
            await dbContext.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("cliente/{clienteId:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePedidosCliente(Guid clienteId)
        {
            var cliente = await dbContext.Clientes.FindAsync(clienteId);
            if (cliente == null)
                return BadRequest("Cliente não encontrado.");

            var pedidos = await dbContext.Pedidos
                .Include(p => p.PedidoProdutos)
                .Where(p => p.ClienteId == clienteId)
                .ToListAsync();

            if (!pedidos.Any())
                return NoContent();

            foreach (var pedido in pedidos)
            {
                dbContext.PedidoProdutos.RemoveRange(pedido.PedidoProdutos);
                dbContext.Pedidos.Remove(pedido);
            }

            await dbContext.SaveChangesAsync();
            return NoContent();
        }
    }
}
