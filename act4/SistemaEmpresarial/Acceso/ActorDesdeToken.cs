using HotChocolate;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SistemaEmpresarial.Data;

namespace SistemaEmpresarial.Acceso
{
    // Obtiene la membresía que llama a partir del token, no del texto de la operación.
    public static class ActorDesdeToken
    {
        private const string Prefijo = "Bearer ";

        public static async Task<string> LeerAsync(AppDbContext db, IHttpContextAccessor accesor)
        {
            var encabezado = accesor.HttpContext?.Request.Headers.Authorization.ToString() ?? string.Empty;

            if (!encabezado.StartsWith(Prefijo, StringComparison.Ordinal))
                throw new GraphQLException("Falta el token");

            var token = encabezado[Prefijo.Length..].Trim();
            var (companyUserId, companyId) = TokenService.Validar(token);

            var membresia = await db.CompanyUsers.FirstOrDefaultAsync(cu => cu.Id == companyUserId);

            if (membresia == null || !membresia.IsActive)
                throw new GraphQLException("La membresía no existe o no está activa");

            if (membresia.CompanyId != companyId)
                throw new GraphQLException("El token no corresponde a esa empresa");

            return companyUserId;
        }
    }
}
