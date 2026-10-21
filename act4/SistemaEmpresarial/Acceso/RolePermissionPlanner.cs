using HotChocolate;
using Microsoft.EntityFrameworkCore;
using SistemaEmpresarial.Data;
using SistemaEmpresarial.GraphQL;
using SistemaEmpresarial.Models;

namespace SistemaEmpresarial.Acceso
{
    // Validación y cálculo compartidos por previewRolePermissions y setRolePermissions.
    // Así la previsualización y la aplicación no pueden dar resultados distintos.
    public static class RolePermissionPlanner
    {
        public const string CodigoAdministrador = "ADMINISTRADOR";

        public static async Task<RolePermissionPlan> CalcularAsync(
            AppDbContext db,
            string roleId,
            IReadOnlyList<string> permissionIds)
        {
            var ids = permissionIds.ToList();

            // 1. Rol existente y activo
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null || !role.IsActive)
                throw new GraphQLException("El rol no existe o está inactivo");

            // Regla del ejercicio: no dejar al administrador sin su lista
            if (role.Code == CodigoAdministrador)
                throw new GraphQLException("En este laboratorio no se modifica la lista del rol ADMINISTRADOR");

            // 2. IDs repetidos: se rechazan, no se eliminan en silencio
            var repetidos = ids
                .GroupBy(id => id, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (repetidos.Count > 0)
                throw new GraphQLException($"IDs de permiso repetidos: {string.Join(", ", repetidos)}");

            // 3. Existencia y estado activo de cada permiso
            var permisos = await db.Permissions
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();

            var inexistentes = ids.Except(permisos.Select(p => p.Id)).ToList();

            if (inexistentes.Count > 0)
                throw new GraphQLException($"Permisos inexistentes: {string.Join(", ", inexistentes)}");

            var inactivos = permisos.Where(p => !p.IsActive).Select(p => p.Code).ToList();

            if (inactivos.Count > 0)
                throw new GraphQLException($"Permisos inactivos: {string.Join(", ", inactivos)}");

            // 4. Comparación con la configuración actual
            // Un [] es válido: desactiva todas las relaciones activas del rol.
            var relaciones = await db.RolePermissions
                .Include(rp => rp.Permission)
                .Where(rp => rp.RoleId == role.Id)
                .ToListAsync();

            var activos = relaciones
                .Where(rp => rp.IsActive)
                .Select(rp => rp.Permission)
                .OrderBy(p => p.Code)
                .ToList();

            var deseados = permisos.OrderBy(p => p.Code).ToList();
            var idsActivos = activos.Select(p => p.Id).ToHashSet();
            var idsDeseados = deseados.Select(p => p.Id).ToHashSet();

            return new RolePermissionPlan
            {
                Role = role,
                Current = activos,
                ToActivate = deseados.Where(p => !idsActivos.Contains(p.Id)).ToList(),
                ToDeactivate = activos.Where(p => !idsDeseados.Contains(p.Id)).ToList(),
                Unchanged = deseados.Where(p => idsActivos.Contains(p.Id)).ToList(),
                FinalCount = deseados.Count,
                Relaciones = relaciones.ToDictionary(rp => rp.PermissionId)
            };
        }
    }
}
