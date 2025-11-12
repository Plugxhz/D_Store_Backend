using System;
using System.Threading.Tasks;

namespace Dunder_Store.Interfaces.IRepositories
{
    public interface IPedidoProdutoRepository
    {
        Task RemoverPorProdutoIdAsync(Guid produtoId);
    }
}
