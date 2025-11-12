using Dunder_Store.DTO;
using Dunder_Store.Entities;
using Dunder_Store.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Dunder_Store.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClienteController : ControllerBase
    {
        private readonly IClienteService _clienteService;

        private Guid? GetLoggedClienteId()
        {
            var id = User?.FindFirst("Id")?.Value ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(id, out var guid)) return guid;
            return null;
        }

        public ClienteController(IClienteService clienteService)
        {
            _clienteService = clienteService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Paginador<ClienteDTOOutput>>> GetClientes(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanhoPagina = 10)
        {
            if (pagina <= 0) pagina = 1;
            if (tamanhoPagina <= 0) tamanhoPagina = 10;

            var clientes = await _clienteService.GetAllAsync();
            var totalItens = clientes.Count();

            var clientesPaginados = clientes
                .Skip((pagina - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .Select(c => new ClienteDTOOutput(c.Id, c.Nome, c.Cpf, c.Email, c.Cep, c.NumEndereco))
                .ToList();

            if (!clientesPaginados.Any())
                return NoContent();

            var resultado = new Paginador<ClienteDTOOutput>(
                clientesPaginados,
                totalItens,
                pagina,
                tamanhoPagina
            );

            return Ok(resultado);
        }

        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ClienteDTOOutput>> GetClienteId(Guid id)
        {
            var cliente = await _clienteService.GetByIdAsync(id);
            if (cliente == null)
                return NotFound();

            var clienteDTO = new ClienteDTOOutput(cliente.Id, cliente.Nome, cliente.Cpf, cliente.Email, cliente.Cep, cliente.NumEndereco);
            return Ok(clienteDTO);
        }

        [HttpGet("count")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<int>> GetTotalClientes()
        {
            var total = (await _clienteService.GetAllAsync()).Count();
            return Ok(total);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<ClienteDTOOutput>> CreateCliente([FromForm] ClienteDTOInput novoClienteDTO)
        {
            var clientes = await _clienteService.GetAllAsync();
            if (clientes.Any(c => c.Cpf == novoClienteDTO.cpf))
                return BadRequest("Já existe um cliente com este CPF");

            var novoCliente = new Cliente(
                novoClienteDTO.nome,
                novoClienteDTO.cpf,
                novoClienteDTO.email,
                novoClienteDTO.senha,
                novoClienteDTO.cep,
                novoClienteDTO.numEndereco
            );

            await _clienteService.CriarClienteAsync(novoCliente);

            var clienteDTO = new ClienteDTOOutput(novoCliente.Id, novoCliente.Nome, novoCliente.Cpf, novoCliente.Email, novoCliente.Cep, novoCliente.NumEndereco);
            return CreatedAtAction(nameof(GetClienteId), new { id = novoCliente.Id }, clienteDTO);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ClienteDTOOutput>> LoginCliente([FromForm] LoginDTO loginDTO)
        {
            var clientes = await _clienteService.GetAllAsync();
            var cliente = clientes.FirstOrDefault(c => c.Email == loginDTO.email && c.Senha == loginDTO.senha);

            if (cliente == null)
                return Unauthorized("Email ou senha inválidos");

            var clienteDTO = new ClienteDTOOutput(cliente.Id, cliente.Nome, cliente.Cpf, cliente.Email, cliente.Cep, cliente.NumEndereco);
            return Ok(clienteDTO);
        }

        [HttpGet("me")]
        [Authorize(Roles = "Cliente")]
        public async Task<ActionResult<ClienteDTOOutput>> GetMeuPerfil()
        {
            var userId = GetLoggedClienteId();
            if (userId == null)
                return Unauthorized();

            var cliente = await _clienteService.GetByIdAsync(userId.Value);
            if (cliente == null)
                return NotFound();

            var clienteDTO = new ClienteDTOOutput(cliente.Id, cliente.Nome, cliente.Cpf, cliente.Email, cliente.Cep, cliente.NumEndereco);
            return Ok(clienteDTO);
        }

        [HttpPatch("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCliente(Guid id, ClienteDTOInput clienteAtualizadoDTO)
        {
            var cliente = await _clienteService.GetByIdAsync(id);
            if (cliente == null)
                return NotFound();

            var clientes = await _clienteService.GetAllAsync();
            if (clientes.Any(c => c.Id != id && c.Cpf == clienteAtualizadoDTO.cpf))
                return BadRequest("Já existe um cliente com esse CPF");

            if (!string.IsNullOrWhiteSpace(clienteAtualizadoDTO.nome)) cliente.Nome = clienteAtualizadoDTO.nome;
            if (!string.IsNullOrWhiteSpace(clienteAtualizadoDTO.cpf)) cliente.Cpf = clienteAtualizadoDTO.cpf;
            if (!string.IsNullOrWhiteSpace(clienteAtualizadoDTO.email)) cliente.Email = clienteAtualizadoDTO.email;
            if (!string.IsNullOrWhiteSpace(clienteAtualizadoDTO.senha)) cliente.Senha = clienteAtualizadoDTO.senha;
            if (!string.IsNullOrWhiteSpace(clienteAtualizadoDTO.cep)) cliente.Cep = clienteAtualizadoDTO.cep;
            if (!string.IsNullOrWhiteSpace(clienteAtualizadoDTO.numEndereco)) cliente.NumEndereco = clienteAtualizadoDTO.numEndereco;

            await _clienteService.AtualizarClienteAsync(cliente);
            return NoContent();
        }

        [HttpPatch("me")]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> UpdateMeuPerfil([FromForm] ClienteDTOInput clienteAtualizadoDTO)
        {
            var userId = GetLoggedClienteId();
            if (userId == null)
                return Unauthorized();

            var cliente = await _clienteService.GetByIdAsync(userId.Value);
            if (cliente == null)
                return NotFound();

            var clientes = await _clienteService.GetAllAsync();
            if (!string.IsNullOrWhiteSpace(clienteAtualizadoDTO.cpf) && clientes.Any(c => c.Id != userId.Value && c.Cpf == clienteAtualizadoDTO.cpf))
                return BadRequest("Já existe um cliente com esse CPF");

            if (!string.IsNullOrWhiteSpace(clienteAtualizadoDTO.nome)) cliente.Nome = clienteAtualizadoDTO.nome;
            if (!string.IsNullOrWhiteSpace(clienteAtualizadoDTO.cpf)) cliente.Cpf = clienteAtualizadoDTO.cpf;
            if (!string.IsNullOrWhiteSpace(clienteAtualizadoDTO.email)) cliente.Email = clienteAtualizadoDTO.email;
            if (!string.IsNullOrWhiteSpace(clienteAtualizadoDTO.senha)) cliente.Senha = clienteAtualizadoDTO.senha;
            if (!string.IsNullOrWhiteSpace(clienteAtualizadoDTO.cep)) cliente.Cep = clienteAtualizadoDTO.cep;
            if (!string.IsNullOrWhiteSpace(clienteAtualizadoDTO.numEndereco)) cliente.NumEndereco = clienteAtualizadoDTO.numEndereco;

            await _clienteService.AtualizarClienteAsync(cliente);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCliente(Guid id)
        {
            await _clienteService.RemoverClienteAsync(id);
            return NoContent();
        }
    }
}
