using Dunder_Store.Database;
using Dunder_Store.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Dunder_Store.Data.Repositories
{
    public class PedidoProdutoRepository : IPedidoProdutoRepository
    {
        private readonly ProdutosDbContext _dbContext;

        public PedidoProdutoRepository(ProdutosDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task RemoverPorProdutoIdAsync(Guid produtoId)
        {
            var itens = await _dbContext.PedidoProdutos
                            .Where(pp => pp.ProdutoId == produtoId)
                            .ToListAsync();

            if (itens.Any())
            {
                _dbContext.PedidoProdutos.RemoveRange(itens);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
