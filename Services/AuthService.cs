// SistemaDoacaoSangue/Services/AuthService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SistemaDoacaoSangue.Data;
using SistemaDoacaoSangue.Models;
using BCrypt.Net;
using System.Threading.Tasks;

namespace SistemaDoacaoSangue.Services
{
    public interface IAuthService
    {
        string Authenticate(string username, string password);
        string GenerateJwtToken(Usuario user);
    }

    public class AuthService : IAuthService
    {
        private readonly SistemaDoacaoSangueContext Sangue_Context;
        private readonly string _key;

        public AuthService(SistemaDoacaoSangueContext context, IConfiguration configuration)
        {
            Sangue_Context = context;
            _key = configuration.GetValue<string>("Jwt:Key");
        }

        public string Authenticate(string username, string password)
        {
            // Busque o usuário no banco de dados
            var user = Sangue_Context.Usuarios.SingleOrDefault(x => x.Username == username);

            // Verifique se o usuário existe e se a senha está correta
            if (user == null || !VerifyPasswordHash(password, user.SenhaHash))
                return null;

            // Gere o token JWT
            return GenerateJwtToken(user); // Use o método separado
        }

        public string GenerateJwtToken(Usuario user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_key);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private bool VerifyPasswordHash(string password, string storedHash)
        {
            // Implemente a verificação da senha. Exemplo com BCrypt:
            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }
    }
}
