using Dunder_Store.Entities;
using Dunder_Store.Interfaces.IRepositories;
using Dunder_Store.Interfaces.IServices;

namespace Dunder_Store.Services
{
    public class PedidoService : IPedidoService
    {
        private readonly IPedidoRepository _pedidoRepository;

        public PedidoService(IPedidoRepository pedidoRepository)
        {
            _pedidoRepository = pedidoRepository;
        }

        public async Task<IEnumerable<Pedido>> GetAllAsync()
        {
            return await _pedidoRepository.GetAllAsync();
        }

        public async Task<Pedido?> GetByIdAsync(string id)
        {
            if (!Guid.TryParse(id, out var guidId))
                return null; // ou lançar uma exceção, se preferir

            return await _pedidoRepository.GetByIdAsync(guidId);
        }

        public async Task<Pedido> CriarPedidoAsync(Pedido pedido)
        {
            await _pedidoRepository.AddAsync(pedido);
            return pedido;
        }

        public async Task AtualizarPedidoAsync(Pedido pedido)
        {
            await _pedidoRepository.UpdateAsync(pedido);
        }

        public async Task RemoverPedidoAsync(string id)
        {
            if (!Guid.TryParse(id, out var guidId))
                return; // ou lançar uma exceção, se preferir

            var pedido = await _pedidoRepository.GetByIdAsync(guidId);
            if (pedido != null)
            {
                await _pedidoRepository.DeleteAsync(pedido);
            }
        }
    }
}
