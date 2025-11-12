using Dunder_Store.Database;
using Dunder_Store.Entities;
using Dunder_Store.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Dunder_Store.Data.Repositories
{
    public class PedidoRepository : IPedidoRepository
    {
        private readonly ProdutosDbContext _dbContext;

        public PedidoRepository(ProdutosDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Pedido>> GetAllAsync()
        {
            return await _dbContext.Pedidos.ToListAsync();
        }

        public async Task<Pedido?> GetByIdAsync(Guid id)
        {
            return await _dbContext.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task AddAsync(Pedido pedido)
        {
            await _dbContext.Pedidos.AddAsync(pedido);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Pedido pedido)
        {
            _dbContext.Pedidos.Update(pedido);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Pedido pedido)
        {
            _dbContext.Pedidos.Remove(pedido);
            await _dbContext.SaveChangesAsync();
        }
    }
}
