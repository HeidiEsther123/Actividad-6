using HotChocolate;
using SistemaEmpresarial.Acceso;
using SistemaEmpresarial.Data;
using SistemaEmpresarial.Models;

namespace SistemaEmpresarial.GraphQL
{
    public partial class Mutation
    {
        // Establece exactamente la lista recibida, en una sola transacción.
        // Repetir la misma lista no cambia nada: changed = false.
        public async Task<RolePermissionUpdate> SetRolePermissionsAsync(
            SetRolePermissionsInput input,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.RolesWrite);

            await using var transaccion = await context.Database.BeginTransactionAsync();

            try
            {
                // Validar y calcular contra la base actual, dentro de la transacción
                var plan = await RolePermissionPlanner.CalcularAsync(context, input.RoleId, input.PermissionIds);

                if (plan.HasChanges)
                {
                    foreach (var permiso in plan.ToActivate)
                    {
                        if (plan.Relaciones.TryGetValue(permiso.Id, out var existente))
                        {
                            // Reactiva la misma fila; no inserta duplicados
                            existente.IsActive = true;
                        }
                        else
                        {
                            context.RolePermissions.Add(new RolePermission
                            {
                                RoleId = plan.Role.Id,
                                PermissionId = permiso.Id,
                                IsActive = true
                            });
                        }
                    }

                    foreach (var permiso in plan.ToDeactivate)
                        plan.Relaciones[permiso.Id].IsActive = false;

                    await context.SaveChangesAsync();
                }

                await transaccion.CommitAsync();

                return new RolePermissionUpdate
                {
                    Role = plan.Role,
                    Activated = plan.ToActivate,
                    Deactivated = plan.ToDeactivate,
                    Unchanged = plan.Unchanged,
                    FinalCount = plan.FinalCount,
                    Changed = plan.HasChanges
                };
            }
            catch
            {
                await transaccion.RollbackAsync();
                throw;
            }
        }
    }
}
