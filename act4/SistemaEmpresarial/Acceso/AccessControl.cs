using HotChocolate;
using Microsoft.EntityFrameworkCore;
using SistemaEmpresarial.Data;
using SistemaEmpresarial.Models;

namespace SistemaEmpresarial.Acceso
{
    // Códigos de permiso del catálogo (recurso.accion)
    public static class Perm
    {
        public const string CompanyUsersRead = "company.users.read";
        public const string CompanyUsersWrite = "company.users.write";
        public const string AccountsRead = "accounts.read";
        public const string AccountsWrite = "accounts.write";
        public const string ConceptsRead = "concepts.read";
        public const string ConceptsWrite = "concepts.write";
        public const string TransactionsRead = "transactions.read";
        public const string TransactionsWrite = "transactions.write";
        public const string RolesRead = "roles.read";
        public const string RolesWrite = "roles.write";
    }

    public static class AccessControl
    {
        // Cadena: membresía → rol activo → permisos activos del rol → ¿está el código?
        // Si falta algo lanza el error ANTES de que el resolver escriba, así no hay commit.
        // Devuelve la membresía que actúa para que el resolver conozca su empresa y su usuario.
        public static async Task<CompanyUser> ExigirPermisoAsync(
            AppDbContext db,
            string actorCompanyUserId,
            string codigo)
        {
            var actor = await db.CompanyUsers
                .Include(cu => cu.User)
                .FirstOrDefaultAsync(cu => cu.Id == actorCompanyUserId);

            if (actor == null || !actor.IsActive)
                throw new GraphQLException("La membresía no existe o está inactiva");

            if (actor.User == null || !actor.User.IsActive)
                throw new GraphQLException("El usuario de la membresía está inactivo");

            var asignacion = await db.CompanyUserRoles
                .FirstOrDefaultAsync(cur =>
                    cur.CompanyUserId == actorCompanyUserId &&
                    cur.IsActive);

            if (asignacion == null)
                throw new GraphQLException("La membresía no tiene un rol activo");

            var permitido = await db.RolePermissions.AnyAsync(rp =>
                rp.RoleId == asignacion.RoleId &&
                rp.IsActive &&
                rp.Role.IsActive &&
                rp.Permission.Code == codigo &&
                rp.Permission.IsActive);

            if (!permitido)
                throw new GraphQLException($"El rol no tiene el permiso {codigo}");

            return actor;
        }

        // Frontera entre empresas: el actor solo opera sobre datos de su propia empresa.
        public static void ExigirMismaEmpresa(CompanyUser actor, string companyId)
        {
            if (actor.CompanyId != companyId)
                throw new GraphQLException("La membresía no pertenece a la empresa del recurso");
        }
    }
}
