using Microsoft.EntityFrameworkCore;
using SistemaEmpresarial.Data;
using SistemaEmpresarial.Models;

namespace SistemaEmpresarial.Acceso
{
    public static class AccessSeeder
    {
        // Si la base ya existía antes de esta actividad, EnsureCreated no agrega tablas nuevas.
        // Este script las crea solo si faltan. En una base nueva no hace nada.
        private const string CreateTablesSql = @"
CREATE TABLE IF NOT EXISTS ""Roles"" (
    ""Id"" text NOT NULL,
    ""Code"" text NOT NULL,
    ""Name"" text NOT NULL,
    ""Description"" text NULL,
    ""IsActive"" boolean NOT NULL DEFAULT TRUE,
    ""CreatedAt"" timestamp with time zone NOT NULL,
    CONSTRAINT ""PK_Roles"" PRIMARY KEY (""Id"")
);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Roles_Code"" ON ""Roles"" (""Code"");

CREATE TABLE IF NOT EXISTS ""Permissions"" (
    ""Id"" text NOT NULL,
    ""Code"" text NOT NULL,
    ""Name"" text NOT NULL,
    ""Description"" text NULL,
    ""IsActive"" boolean NOT NULL DEFAULT TRUE,
    ""CreatedAt"" timestamp with time zone NOT NULL,
    CONSTRAINT ""PK_Permissions"" PRIMARY KEY (""Id"")
);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Permissions_Code"" ON ""Permissions"" (""Code"");

CREATE TABLE IF NOT EXISTS ""RolePermissions"" (
    ""Id"" text NOT NULL,
    ""RoleId"" text NOT NULL,
    ""PermissionId"" text NOT NULL,
    ""IsActive"" boolean NOT NULL DEFAULT TRUE,
    ""CreatedAt"" timestamp with time zone NOT NULL,
    CONSTRAINT ""PK_RolePermissions"" PRIMARY KEY (""Id""),
    CONSTRAINT ""FK_RolePermissions_Roles_RoleId""
        FOREIGN KEY (""RoleId"") REFERENCES ""Roles"" (""Id"") ON DELETE RESTRICT,
    CONSTRAINT ""FK_RolePermissions_Permissions_PermissionId""
        FOREIGN KEY (""PermissionId"") REFERENCES ""Permissions"" (""Id"") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_RolePermissions_RoleId_PermissionId""
    ON ""RolePermissions"" (""RoleId"", ""PermissionId"");
CREATE INDEX IF NOT EXISTS ""IX_RolePermissions_PermissionId""
    ON ""RolePermissions"" (""PermissionId"");

CREATE TABLE IF NOT EXISTS ""CompanyUserRoles"" (
    ""Id"" text NOT NULL,
    ""CompanyUserId"" text NOT NULL,
    ""RoleId"" text NOT NULL,
    ""IsActive"" boolean NOT NULL DEFAULT TRUE,
    ""CreatedAt"" timestamp with time zone NOT NULL,
    CONSTRAINT ""PK_CompanyUserRoles"" PRIMARY KEY (""Id""),
    CONSTRAINT ""FK_CompanyUserRoles_CompanyUsers_CompanyUserId""
        FOREIGN KEY (""CompanyUserId"") REFERENCES ""CompanyUsers"" (""Id"") ON DELETE CASCADE,
    CONSTRAINT ""FK_CompanyUserRoles_Roles_RoleId""
        FOREIGN KEY (""RoleId"") REFERENCES ""Roles"" (""Id"") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_CompanyUserRoles_CompanyUserId""
    ON ""CompanyUserRoles"" (""CompanyUserId"") WHERE ""IsActive"" = TRUE;
CREATE INDEX IF NOT EXISTS ""IX_CompanyUserRoles_RoleId""
    ON ""CompanyUserRoles"" (""RoleId"");
";

        // Los cuatro roles del documento
        private static readonly (string Code, string Name, string Description)[] RolesIniciales =
        {
            ("ADMINISTRADOR", "Administrador", "Administrador principal de la empresa"),
            ("CONTADOR", "Contador", "Arma cuentas y conceptos y registra movimientos"),
            ("CAPTURISTA", "Capturista", "Registra movimientos"),
            ("CONSULTA", "Consulta", "Solo lee"),
        };

        // Los diez permisos del documento
        private static readonly (string Code, string Name, string Description)[] PermisosIniciales =
        {
            (Perm.CompanyUsersRead, "Ver usuarios", "Ver las membresías de la empresa"),
            (Perm.CompanyUsersWrite, "Administrar usuarios", "Crear usuarios y desactivar membresías"),
            (Perm.AccountsRead, "Consultar cuentas", "Consultar cuentas"),
            (Perm.AccountsWrite, "Crear cuentas", "Crear cuentas"),
            (Perm.ConceptsRead, "Consultar conceptos", "Consultar conceptos"),
            (Perm.ConceptsWrite, "Crear conceptos", "Crear conceptos y asignarlos a una cuenta"),
            (Perm.TransactionsRead, "Consultar movimientos", "Consultar movimientos"),
            (Perm.TransactionsWrite, "Registrar movimientos", "Registrar un ingreso o un egreso"),
            (Perm.RolesRead, "Ver roles", "Ver roles y la matriz de permisos"),
            (Perm.RolesWrite, "Administrar roles", "Asignar o quitar permisos de un rol, y asignar un rol a una membresía"),
        };

        // Matriz inicial: rol → permisos
        private static readonly Dictionary<string, string[]> Matriz = new()
        {
            ["ADMINISTRADOR"] = new[]
            {
                Perm.CompanyUsersRead, Perm.CompanyUsersWrite,
                Perm.AccountsRead, Perm.AccountsWrite,
                Perm.ConceptsRead, Perm.ConceptsWrite,
                Perm.TransactionsRead, Perm.TransactionsWrite,
                Perm.RolesRead, Perm.RolesWrite,
            },
            ["CONTADOR"] = new[]
            {
                Perm.CompanyUsersRead,
                Perm.AccountsRead, Perm.AccountsWrite,
                Perm.ConceptsRead, Perm.ConceptsWrite,
                Perm.TransactionsRead, Perm.TransactionsWrite,
                Perm.RolesRead,
            },
            ["CAPTURISTA"] = new[]
            {
                Perm.AccountsRead,
                Perm.ConceptsRead,
                Perm.TransactionsRead, Perm.TransactionsWrite,
            },
            ["CONSULTA"] = new[]
            {
                Perm.CompanyUsersRead,
                Perm.AccountsRead,
                Perm.ConceptsRead,
                Perm.TransactionsRead,
            },
        };

        public static void Seed(AppDbContext db)
        {
            db.Database.ExecuteSqlRaw(CreateTablesSql);

            // 1. Roles: solo si la tabla está vacía (un reinicio no duplica)
            if (!db.Roles.Any())
            {
                foreach (var r in RolesIniciales)
                    db.Roles.Add(new Role { Code = r.Code, Name = r.Name, Description = r.Description });

                db.SaveChanges();
            }

            // 2. Permisos: solo si la tabla está vacía
            if (!db.Permissions.Any())
            {
                foreach (var p in PermisosIniciales)
                    db.Permissions.Add(new Permission { Code = p.Code, Name = p.Name, Description = p.Description });

                db.SaveChanges();
            }

            // 3. Matriz: solo para los roles que todavía no tienen ningún permiso
            var permisosPorCodigo = db.Permissions.ToDictionary(p => p.Code, p => p.Id);

            foreach (var rol in db.Roles.ToList())
            {
                if (!Matriz.TryGetValue(rol.Code, out var codigos))
                    continue;

                if (db.RolePermissions.Any(rp => rp.RoleId == rol.Id))
                    continue;

                foreach (var codigo in codigos)
                {
                    if (permisosPorCodigo.TryGetValue(codigo, out var permisoId))
                        db.RolePermissions.Add(new RolePermission { RoleId = rol.Id, PermissionId = permisoId });
                }
            }

            db.SaveChanges();

            // 4. Administradores que ya existían antes de esta actividad reciben el rol ADMINISTRADOR
            var admin = db.Roles.FirstOrDefault(r => r.Code == "ADMINISTRADOR");

            if (admin != null)
            {
                var sinRol = db.CompanyUsers
                    .Where(cu => cu.IsAdmin && cu.IsActive &&
                                 !db.CompanyUserRoles.Any(cur => cur.CompanyUserId == cu.Id && cur.IsActive))
                    .ToList();

                foreach (var cu in sinRol)
                    db.CompanyUserRoles.Add(new CompanyUserRole { CompanyUserId = cu.Id, RoleId = admin.Id });

                db.SaveChanges();
            }
        }
    }
}
