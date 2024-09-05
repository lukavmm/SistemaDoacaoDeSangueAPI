using HotChocolate;
using HotChocolate.Data;
using SistemaDoacaoSangue.Models;
using SistemaDoacaoSangue.Services;
using SistemaDoacaoSangue.DTO;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity; // Para PasswordHasher
using System;
using SistemaDoacaoSangue.Data;
using SistemaDoacaoSangue.Payloads;
using SistemaDoacaoSangue.Functions;

public class Mutation
{
    private readonly SistemaDoacaoSangueContext _context;
    private readonly IAuthService _authService;

    public Mutation(SistemaDoacaoSangueContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    // Registro de usuário
    [Authorize]
    [UseDbContext(typeof(SistemaDoacaoSangueContext))]
    public async Task<MessagePayload> RegistrarUsuario(
        [ScopedService] SistemaDoacaoSangueContext context,
        RegistroDTO dadosUsuario,
        [GlobalState(nameof(ClaimsPrincipal))] ClaimsPrincipal claimsPrincipal)
    {
        var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            if (string.IsNullOrEmpty(dadosUsuario.Username))
                throw new InvalidOperationException("Informe um nome de usuário.");
            if (string.IsNullOrEmpty(dadosUsuario.Email))
                throw new InvalidOperationException("Informe um e-mail.");
            if (string.IsNullOrEmpty(dadosUsuario.Password))
                throw new InvalidOperationException("Informe uma senha.");

            // Verifica se o nome de usuário ou email já existe
            var usuarioExistente = await context.Usuarios
                .Where(u => u.Username == dadosUsuario.Username || u.Email == dadosUsuario.Email)
                .FirstOrDefaultAsync();

            if (usuarioExistente != null)
            {
                if (usuarioExistente.Username == dadosUsuario.Username)
                    throw new InvalidOperationException("Nome de usuário já está em uso.");
                if (usuarioExistente.Email == dadosUsuario.Email)
                    throw new InvalidOperationException("E-mail já está em uso.");
            }

            var passwordHasher = new PasswordHasher<Usuario>();

            var novoUsuario = new Usuario
            {
                Username = dadosUsuario.Username,
                Email = dadosUsuario.Email,
                SenhaHash = passwordHasher.HashPassword(null, dadosUsuario.Password),
                TipoUsuario = dadosUsuario.tipo_cadastro
            };

            context.Usuarios.Add(novoUsuario);
            await context.SaveChangesAsync();

            // Verifica o tipo de cadastro e preenche a tabela correspondente
            if (dadosUsuario.tipo_cadastro == "doador")
            {
                var novoDoador = new Doadore
                {
                    UsuarioId = novoUsuario.Id,
                    NomeCompleto = dadosUsuario.NomeCompleto,
                    DataNascimento = dadosUsuario.DataNascimento.Value,
                    TipoSanguineo = dadosUsuario.Tipo_sanguineo,
                    Sexo = dadosUsuario.Sexo,
                    Endereco = dadosUsuario.Endereco,
                    Telefone = dadosUsuario.Telefone,
                    DataUltimaDoacao = dadosUsuario.DataUltimaDoacao,
                    Cpf = dadosUsuario.CPF
                };

                context.Doadores.Add(novoDoador);
                await context.SaveChangesAsync();
            }
            else if (dadosUsuario.tipo_cadastro == "hemocentro")
            {
                var novoHemocentro = new Hemocentro
                {
                    UsuarioId = novoUsuario.Id,
                    NomeHemocentro = dadosUsuario.NomeCompleto,
                    Endereco = dadosUsuario.Endereco,
                    EmailContato = dadosUsuario.Email,
                    Telefone = dadosUsuario.Telefone,
                    Cnpj = dadosUsuario.CNPJ
                };

                context.Hemocentros.Add(novoHemocentro);
                await context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return new MessagePayload("Usuário cadastrado com sucesso!", "");
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync();
            return new MessagePayload("", ex.Message);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new MessagePayload("", ex.Message);
        }
    }

    // Login do usuário
    [UseDbContext(typeof(SistemaDoacaoSangueContext))]
    public async Task<AuthPayload> LoginUsuario(
        [ScopedService] SistemaDoacaoSangueContext context,
        UsersDTO dadosLogin)
        {
        var user = await context.Usuarios.SingleOrDefaultAsync(u => u.Username == dadosLogin.Username);

        if (user == null)
        {
            return new AuthPayload { Message = "Credenciais inválidas" };
        }

        var passwordHasher = new PasswordHasher<Usuario>();
        var result = passwordHasher.VerifyHashedPassword(user, user.SenhaHash, dadosLogin.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            return new AuthPayload { Message = "Credenciais inválidas" };
        }

        // Gerar o token JWT
        var token = _authService.GenerateJwtToken(user);

        // Aqui estamos garantindo que o UserPayload está sendo corretamente instanciado

        return new AuthPayload
        {
            Token = token,
            Message = null,
            Id = user.Id.ToString(),
            Tipo_usuario = user.TipoUsuario

        };
    }


}

