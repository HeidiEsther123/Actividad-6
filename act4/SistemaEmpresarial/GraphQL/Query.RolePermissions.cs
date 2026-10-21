using HotChocolate;
using SistemaEmpresarial.Acceso;
using SistemaEmpresarial.Data;
using SistemaEmpresarial.Models;

namespace SistemaEmpresarial.GraphQL
{
    public partial class Query
    {
        // Solo permisos activos; sin resource (o null) devuelve todo el catálogo activo
        public async Task<IQueryable<Permission>> GetPermissionCatalog(
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context,
            PermissionResource? resource = null)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.RolesRead);

            var consulta = context.Permissions.Where(p => p.IsActive);

            if (resource.HasValue)
            {
                var prefijo = PermissionResourceMap.PrefijoDe(resource.Value);
                consulta = consulta.Where(p => p.Code.StartsWith(prefijo));
            }

            return consulta.OrderBy(p => p.Code);
        }

        // Valida y calcula. No inserta, actualiza ni desactiva filas.
        public async Task<RolePermissionPlan?> PreviewRolePermissions(
            SetRolePermissionsInput input,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.RolesRead);

            return await RolePermissionPlanner.CalcularAsync(context, input.RoleId, input.PermissionIds);
        }
    }
}
