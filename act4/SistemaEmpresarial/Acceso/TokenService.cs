using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HotChocolate;
using Microsoft.IdentityModel.Tokens;

namespace SistemaEmpresarial.Acceso
{
    // Firma y valida el JWT de la sesión. El secreto vive en la variable de ambiente JWT_SECRET.
    public static class TokenService
    {
        public const int MinutosVigencia = 30;
        public const string VariableSecreto = "JWT_SECRET";
        private const string Algoritmo = SecurityAlgorithms.HmacSha256;

        private static SymmetricSecurityKey ObtenerLlave()
        {
            var secreto = Environment.GetEnvironmentVariable(VariableSecreto);

            if (string.IsNullOrWhiteSpace(secreto) || secreto.Length < 32)
                throw new InvalidOperationException($"{VariableSecreto} no está configurado o es demasiado corto");

            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secreto));
        }

        // Solo membresía y empresa. No lleva contraseña, hash ni permisos.
        public static string CrearToken(string companyUserId, string companyId)
        {
            var ahora = DateTime.UtcNow;

            var token = new JwtSecurityToken(
                claims: new[]
                {
                    new Claim("companyUserId", companyUserId),
                    new Claim("companyId", companyId)
                },
                notBefore: ahora,
                expires: ahora.AddMinutes(MinutosVigencia),
                signingCredentials: new SigningCredentials(ObtenerLlave(), Algoritmo));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Firma alterada, token vencido o algoritmo distinto de HS256: se rechaza.
        public static (string CompanyUserId, string CompanyId) Validar(string token)
        {
            var parametros = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = ObtenerLlave(),
                ValidAlgorithms = new[] { Algoritmo },
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var principal = new JwtSecurityTokenHandler().ValidateToken(token, parametros, out _);

                var companyUserId = principal.FindFirst("companyUserId")?.Value;
                var companyId = principal.FindFirst("companyId")?.Value;

                if (string.IsNullOrEmpty(companyUserId) || string.IsNullOrEmpty(companyId))
                    throw new GraphQLException("El token no es válido");

                return (companyUserId, companyId);
            }
            catch (GraphQLException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new GraphQLException("El token no es válido");
            }
        }
    }
}
