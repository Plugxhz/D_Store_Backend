using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dunder_Store.Entities
{
    public class Cliente
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } = Guid.NewGuid(); 

        public string Nome { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string Cep { get; set; } = string.Empty;
        public string NumEndereco { get; set; } = string.Empty;

        public List<Pedido> Pedidos { get; set; } = new();

        public Cliente(string nome, string cpf, string email, string senha, string cep, string numEndereco)
        {
            Nome = nome;
            Cpf = cpf;
            Email = email;
            Senha = senha;
            Cep = cep;
            NumEndereco = numEndereco;
        }

        private Cliente() { }
    }
}
