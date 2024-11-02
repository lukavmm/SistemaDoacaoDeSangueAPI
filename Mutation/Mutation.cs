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
    [Authorize]
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

    // Editar dados do usuario
    [Authorize]
    [UseDbContext(typeof(SistemaDoacaoSangueContext))]
    public MessagePayload EditarPerfil(
        [ScopedService] SistemaDoacaoSangueContext context,
        PerfilDTO dadosPerfil, int codUser, string tipoUsuario,
        [GlobalState(nameof(ClaimsPrincipal))] ClaimsPrincipal claimsPrincipal)
    {
        using var transaction =  context.Database.BeginTransaction();

        try
        {

            var user = context.Usuarios.FirstOrDefault(c => c.Id == codUser);

            if (dadosPerfil.Username == null)
            {
                throw new InvalidOperationException($"Informe um nome de usuário.");
            }
            if (dadosPerfil.Endereco == null)
            {
                throw new InvalidOperationException($"Informe um endereço.");
            }
            if (dadosPerfil.Telefone == null)
            {
                throw new InvalidOperationException($"Informe um telefone.");
            }
            if (dadosPerfil.Email == null)
            {
                throw new InvalidOperationException($"Informe um e-mail.");
            }
            user.Username = dadosPerfil.Username;
                user.Email = dadosPerfil.Email;
            context.SaveChanges();

            // Verificação dos campos do doador
            if (tipoUsuario == "doador")
            {
                var doador =  context.Doadores.FirstOrDefault(c => c.UsuarioId == codUser);


                if (dadosPerfil.NomeCompleto == null)
                {
                    throw new InvalidOperationException($"Informe um nome.");
                }
                if (dadosPerfil.Peso == null)
                {
                    throw new InvalidOperationException($"Informe um peso.");
                }
                if (dadosPerfil.DataUltimaDoacao == null)
                {
                    throw new InvalidOperationException($"Informe a data da última doação.");
                }
                //Atualiza dados do doador

                doador.NomeCompleto = dadosPerfil.NomeCompleto;
                doador.Peso = (decimal)dadosPerfil.Peso;
                doador.DataUltimaDoacao = dadosPerfil.DataUltimaDoacao;
                doador.Telefone = dadosPerfil.Telefone;
                doador.Endereco = dadosPerfil.Endereco;
                context.SaveChanges();

            }
            // Verificação dos campos do hemocentro
            else if (tipoUsuario == "hemocentro")
            {
                var hemocentro =  context.Hemocentros.FirstOrDefault(c => c.UsuarioId == codUser);

                if (dadosPerfil.NomeHemocentro == null)
                {
                    throw new InvalidOperationException($"Informe o nome do hemocentro.");
                }
                if (dadosPerfil.Cpnj == null)
                {
                    throw new InvalidOperationException($"Informe o Cnpj.");
                }

                    hemocentro.NomeHemocentro = dadosPerfil.NomeHemocentro;
                    hemocentro.Cnpj = dadosPerfil.Cpnj;
                    hemocentro.Endereco = dadosPerfil.Endereco;
                    hemocentro.EmailContato = dadosPerfil.Email;
                    hemocentro.Telefone = dadosPerfil.Telefone;
                context.SaveChanges();

            }

            // Salva as mudanças no banco de dados
             context.SaveChanges();

            // Confirma a transação
             transaction.Commit();

            return new MessagePayload("Perfil atualizado com sucesso.", null);
        }
        catch (InvalidOperationException ex)
        {
            // Reverte a transação em caso de erro de validação
            transaction.Rollback();
            return new MessagePayload(null, ex.Message);
        }
        catch (Exception ex)
        {
            // Reverte a transação em caso de erro inesperado
            transaction.Rollback();
            return new MessagePayload(null, "Erro inesperado ao atualizar perfil.");
        }


    }

    // Criar agendamento
    [Authorize]
    [UseDbContext(typeof(SistemaDoacaoSangueContext))]
    public MessagePayload CriarAgendamento(
        [ScopedService] SistemaDoacaoSangueContext context,
        AgendamentoDTO dadosAgendamento, int codUser,int codHemocentro, string tipoUsuario,
        [GlobalState(nameof(ClaimsPrincipal))] ClaimsPrincipal claimsPrincipal)
    {
        using var transaction = context.Database.BeginTransaction();

        try
        {
            var doador = context.Doadores.Where(x => x.UsuarioId == codUser).FirstOrDefault();

            var hemocentro = context.Hemocentros.Where(x => x.UsuarioId == codHemocentro).FirstOrDefault();

            if(tipoUsuario != "doador")
            {
                throw new InvalidOperationException("Apenas doadores podem realizar agendamentos.");
            }
            
            if(doador.DataUltimaDoacao != null)
            {
                var datadoacao = (dadosAgendamento.Data - doador.DataUltimaDoacao.Value).Days;
                
                if(datadoacao < 90)
                {
                    throw new InvalidOperationException($"O doador ainda não completou o intervalo mínimo de 90 dias. Restam {90 - datadoacao} dias.");
                }
            }

            var agendamentosNoMesmoHorario = context.Agendamentos.Where(a => a.HemocentroId == codHemocentro && a.Data == dadosAgendamento.Data && a.Hora == dadosAgendamento.Hora).Count();

            if(agendamentosNoMesmoHorario >= 50)
            {
                throw new InvalidOperationException($"O horário selecionado já está lotado. Por favor, escolha outro horário.");
            }

            var agendamentosExistente = context.Agendamentos.Where(a => a.DoadorId == doador.Id && a.Data == dadosAgendamento.Data && a.Hora == dadosAgendamento.Hora).Any();

            if (agendamentosExistente)
            {
                throw new InvalidOperationException($"Você já possui um agendamento para esta data.");
            }

            var novoAgendamento = new Agendamento
            {
                DoadorId = doador.Id,
                HemocentroId = hemocentro.Id,
                Data = dadosAgendamento.Data,
                Hora = dadosAgendamento.Hora,
                Status = dadosAgendamento.Status ?? "PENDENTE",
                Obs = dadosAgendamento.Obs,
                CriadoEm = DateTime.Now,
                AtualizadoEm = DateTime.Now,
            };
            context.Agendamentos.Add(novoAgendamento);
            
            // Salva as mudanças no banco de dados
            context.SaveChanges();

            // Confirma a transação
            transaction.Commit();

            return new MessagePayload("Agendamento realizado com sucesso!", null);
        }
        catch (InvalidOperationException ex)
        {
            // Reverte a transação em caso de erro de validação
            transaction.Rollback();
            return new MessagePayload(null, ex.Message);
        }
        catch (Exception ex)
        {
            // Reverte a transação em caso de erro inesperado
            transaction.Rollback();
            return new MessagePayload(null, "Erro inesperado ao agendar doação.");
        }

    }


    // Editar agendamento
    [Authorize]
    [UseDbContext(typeof(SistemaDoacaoSangueContext))]
    public MessagePayload EditarAgendamento(
        [ScopedService] SistemaDoacaoSangueContext context,
        AgendamentoDTO dadosAgendamento, int idAgendamento,
        [GlobalState(nameof(ClaimsPrincipal))] ClaimsPrincipal claimsPrincipal)
    {
        using var transaction = context.Database.BeginTransaction();

        try
        {
            var agendamento = context.Agendamentos.Where(a=> a.Id == idAgendamento).FirstOrDefault();

            if (agendamento == null)
            {
                throw new InvalidOperationException($"Agendamento não existente!");
            }

            agendamento.Data = dadosAgendamento.Data;
            agendamento.Hora = dadosAgendamento.Hora;
            agendamento.Status = dadosAgendamento.Status ?? agendamento.Status;
            agendamento.AtualizadoEm = DateTime.Now;

            // Salva as mudanças no banco de dados
            context.SaveChanges();

            // Confirma a transação
            transaction.Commit();

            return new MessagePayload("Agendamento alterado com sucesso!", null);
        }
        catch (InvalidOperationException ex)
        {
            // Reverte a transação em caso de erro de validação
            transaction.Rollback();
            return new MessagePayload(null, ex.Message);
        }
        catch (Exception ex)
        {
            // Reverte a transação em caso de erro inesperado
            transaction.Rollback();
            return new MessagePayload(null, "Erro inesperado ao agendar doação.");
        }

    }
}

