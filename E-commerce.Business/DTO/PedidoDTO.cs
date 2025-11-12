using Dunder_Store.Entities;

namespace Dunder_Store.DTO
{
    public class PedidoDTO
    {
        public string clientecpf { get; set; } = string.Empty;
        public List<PedidoProdutoDTO> produtos { get; set; } = new();
        public string? cupomCodigo { get; set; } // opcional
    }
}
