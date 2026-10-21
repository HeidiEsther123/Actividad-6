using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HotChocolate;
using Microsoft.IdentityModel.Tokens;
using SistemaEmpresarial.Acceso;
using Xunit;

namespace SistemaEmpresarial.Tests
{
    // Casos de la Actividad 6 a nivel de token: firma válida, alterada, vencida y algoritmo.
    public class TokenServiceTests
    {
        private const string Secreto = "clave-de-prueba-solo-para-tests-0123456789";

        public TokenServiceTests()
        {
            Environment.SetEnvironmentVariable(TokenService.VariableSecreto, Secreto);
        }

        [Fact]
        public void Token_valido_devuelve_membresia_y_empresa()
        {
            var token = TokenService.CrearToken("membresia-ana", "empresa-1");

            var (companyUserId, companyId) = TokenService.Validar(token);

            Assert.Equal("membresia-ana", companyUserId);
            Assert.Equal("empresa-1", companyId);
        }

        [Fact]
        public void El_token_no_lleva_contrasena_ni_permisos()
        {
            var token = TokenService.CrearToken("membresia-ana", "empresa-1");
            var payload = new JwtSecurityTokenHandler().ReadJwtToken(token).Payload.SerializeToJson();

            Assert.DoesNotContain("password", payload, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("permission", payload, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Token_con_datos_alterados_se_rechaza()
        {
            var token = TokenService.CrearToken("membresia-maria", "empresa-1");
            var partes = token.Split('.');

            // Cambia el payload por uno que dice otra membresía, manteniendo la firma original
            var payloadFalso = Base64Url(new { companyUserId = "membresia-ana", companyId = "empresa-1", exp = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds() });
            var alterado = $"{partes[0]}.{payloadFalso}.{partes[2]}";

            Assert.Throws<GraphQLException>(() => TokenService.Validar(alterado));
        }

        [Fact]
        public void Token_vencido_se_rechaza()
        {
            var llave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secreto));
            var vencido = new JwtSecurityToken(
                claims: new[] { new Claim("companyUserId", "membresia-ana"), new Claim("companyId", "empresa-1") },
                notBefore: DateTime.UtcNow.AddHours(-2),
                expires: DateTime.UtcNow.AddHours(-1),
                signingCredentials: new SigningCredentials(llave, SecurityAlgorithms.HmacSha256));

            var token = new JwtSecurityTokenHandler().WriteToken(vencido);

            Assert.Throws<GraphQLException>(() => TokenService.Validar(token));
        }

        [Fact]
        public void Token_firmado_con_otro_secreto_se_rechaza()
        {
            var otraLlave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("otro-secreto-que-no-es-del-servidor-0123456789"));
            var falso = new JwtSecurityToken(
                claims: new[] { new Claim("companyUserId", "membresia-ana"), new Claim("companyId", "empresa-1") },
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new SigningCredentials(otraLlave, SecurityAlgorithms.HmacSha256));

            var token = new JwtSecurityTokenHandler().WriteToken(falso);

            Assert.Throws<GraphQLException>(() => TokenService.Validar(token));
        }

        private static string Base64Url(object objeto)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(objeto);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
