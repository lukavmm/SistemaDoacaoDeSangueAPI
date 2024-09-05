using HotChocolate;
using SistemaDoacaoSangue.Models;
using SistemaDoacaoSangue.Services;
using SistemaDoacaoSangue.DTO;
using SistemaDoacaoSangue.Data;
using SistemaDoacaoSangue.Functions;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

public class Query
{
    // Você pode definir consultas GraphQL aqui, se necessário
    [UseDbContext(typeof(SistemaDoacaoSangueContext))]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public async Task<PerfilDTO> getperfil(string tipoUsuario, int codUser, [ScopedService] SistemaDoacaoSangueContext context)
    {
        PerfilDTO perfil = new PerfilDTO();

        if (tipoUsuario == "doador")
        {
            // Buscar dados na tabela 'Doadores'
            var doador = await context.Doadores.FirstOrDefaultAsync(d => d.UsuarioId == codUser);
            if (doador != null)
            {
                perfil.NomeCompleto = doador.NomeCompleto;
                perfil.DataNascimento = doador.DataNascimento;
                perfil.TipoSanguineo = doador.TipoSanguineo;
                perfil.Sexo = doador.Sexo;
                perfil.Endereco = doador.Endereco;
                perfil.Telefone = doador.Telefone;
                perfil.DataUltimaDoacao = doador.DataUltimaDoacao;
                perfil.Cpf = doador.Cpf;
                perfil.Peso = doador.Peso;
            }
        }
        else if (tipoUsuario == "hemocentro")
        {
            // Buscar dados na tabela 'Hemocentros'
            var hemocentro = await context.Hemocentros.FirstOrDefaultAsync(h => h.UsuarioId == codUser);
            if (hemocentro != null)
            {
                perfil.NomeHemocentro = hemocentro.NomeHemocentro;
                perfil.Cpnj = hemocentro.Cnpj;
                perfil.Telefone = hemocentro.Telefone;
                perfil.Endereco = hemocentro.Endereco;
            }
        }

        // Buscar dados na tabela 'Usuario'
        var usuario = await context.Usuarios.FirstOrDefaultAsync(u => u.Id == codUser);
        if (usuario != null)
        {
            perfil.Username = usuario.Username;
            perfil.Email = usuario.Email;
            perfil.DataAtualizacao = usuario.AtualizadoEm;
        }

        return perfil;
    }
}
