using Dunder_Store.Entities;

namespace Dunder_Store.Interfaces.IServices
{
    public interface IPedidoService
    {
        Task<IEnumerable<Pedido>> GetAllAsync();
        Task<Pedido?> GetByIdAsync(string id);
        Task<Pedido> CriarPedidoAsync(Pedido pedido);
        Task AtualizarPedidoAsync(Pedido pedido);
        Task RemoverPedidoAsync(string id);
    }
}
