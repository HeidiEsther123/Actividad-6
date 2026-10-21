using HotChocolate;
using HotChocolate.Types;
using SistemaEmpresarial.Models;

namespace SistemaEmpresarial.GraphQL
{
    // Lista final de permisos de un rol. permissionIds: [ID!]! (la lista vacía sí es válida)
    public record SetRolePermissionsInput(
        [property: ID] string RoleId,
        [property: GraphQLType(typeof(NonNullType<ListType<NonNullType<IdType>>>))] IReadOnlyList<string> PermissionIds);

    // Resultado de previewRolePermissions: calcula, no escribe
    public class RolePermissionPlan
    {
        public Role Role { get; init; } = null!;
        public IReadOnlyList<Permission> Current { get; init; } = Array.Empty<Permission>();
        public IReadOnlyList<Permission> ToActivate { get; init; } = Array.Empty<Permission>();
        public IReadOnlyList<Permission> ToDeactivate { get; init; } = Array.Empty<Permission>();
        public IReadOnlyList<Permission> Unchanged { get; init; } = Array.Empty<Permission>();
        public int FinalCount { get; init; }

        // Uso interno: relaciones existentes del rol indexadas por PermissionId (activas o no)
        [GraphQLIgnore]
        public IReadOnlyDictionary<string, RolePermission> Relaciones { get; init; } =
            new Dictionary<string, RolePermission>();

        [GraphQLIgnore]
        public bool HasChanges => ToActivate.Count > 0 || ToDeactivate.Count > 0;
    }

    // Resultado de setRolePermissions
    public class RolePermissionUpdate
    {
        public Role Role { get; init; } = null!;
        public IReadOnlyList<Permission> Activated { get; init; } = Array.Empty<Permission>();
        public IReadOnlyList<Permission> Deactivated { get; init; } = Array.Empty<Permission>();
        public IReadOnlyList<Permission> Unchanged { get; init; } = Array.Empty<Permission>();
        public int FinalCount { get; init; }
        public bool Changed { get; init; }
    }
}
