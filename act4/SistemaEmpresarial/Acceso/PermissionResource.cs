using HotChocolate;

namespace SistemaEmpresarial.Acceso
{
    // Recursos del catálogo de permisos (filtro de permissionCatalog)
    public enum PermissionResource
    {
        [GraphQLName("COMPANY_USERS")] CompanyUsers,
        [GraphQLName("ACCOUNTS")] Accounts,
        [GraphQLName("CONCEPTS")] Concepts,
        [GraphQLName("TRANSACTIONS")] Transactions,
        [GraphQLName("ROLES")] Roles
    }

    // Traduce el recurso al prefijo ya existente del código del permiso.
    // No crea permisos nuevos.
    public static class PermissionResourceMap
    {
        public static string PrefijoDe(PermissionResource recurso) => recurso switch
        {
            PermissionResource.CompanyUsers => "company.users.",
            PermissionResource.Accounts => "accounts.",
            PermissionResource.Concepts => "concepts.",
            PermissionResource.Transactions => "transactions.",
            PermissionResource.Roles => "roles.",
            _ => throw new ArgumentOutOfRangeException(nameof(recurso))
        };
    }
}
