using Dunder_Store.Entities;

namespace Dunder_Store.Interfaces.IRepositories
{
    public interface IPedidoRepository
    {
        Task<IEnumerable<Pedido>> GetAllAsync();
        Task<Pedido?> GetByIdAsync(Guid id); // ✅ alterado para Guid
        Task AddAsync(Pedido pedido);
        Task UpdateAsync(Pedido pedido);
        Task DeleteAsync(Pedido pedido);
    }
}
