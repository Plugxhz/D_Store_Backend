namespace Dunder_Store.DTO
{
    public class ClienteDTOInput
    {
        public string nome { get; set; } = string.Empty;
        public string cpf { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string senha { get; set; } = string.Empty;
        public string cep { get; set; } = string.Empty;
        public string numEndereco { get; set; } = string.Empty;
    }

    public class ClienteDTOOutput
    {
        public Guid Id { get; set; }  // ✅ Agora é Guid
        public string Nome { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Cep { get; set; } = string.Empty;
        public string NumEndereco { get; set; } = string.Empty;


        public ClienteDTOOutput(Guid id, string nome, string cpf, string email, string cep, string numEndereco)
        {
            Id = id;
            Nome = nome;
            Cpf = cpf;
            Email = email;
            Cep = cep;
            NumEndereco = numEndereco;
        }
    }
}
