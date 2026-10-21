using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using SistemaEmpresarial.Acceso;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SistemaEmpresarial.Data;
using SistemaEmpresarial.GraphQL;
using SistemaEmpresarial.Models;
using Xunit;

namespace SistemaEmpresarial.Tests
{
    // E07. Reversión de un fallo: se provoca una excepción después del flush
    // y antes del commit. La base debe conservar la lista previa.
    public class RolePermissionAtomicityTests
    {
        // Simula un fallo después de que EF envió los cambios a la base (flush),
        // pero antes de que la transacción de la operación confirme.
        private sealed class FalloDespuesDeFlush : SaveChangesInterceptor
        {
            public override ValueTask<int> SavedChangesAsync(
                SaveChangesCompletedEventData eventData,
                int result,
                CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("Fallo controlado después del flush");
        }

        private sealed record Datos(string ActorId, string CompanyId, string RolId, IReadOnlyDictionary<string, string> Permisos);

        private static DbContextOptions<AppDbContext> Opciones(SqliteConnection conexion, bool conFallo)
        {
            var builder = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conexion);

            if (conFallo)
                builder.AddInterceptors(new FalloDespuesDeFlush());

            return builder.Options;
        }

        private static async Task<Datos> SembrarAsync(AppDbContext db)
        {
            var empresa = new Company { Name = "Empresa de prueba" };
            var usuario = new User { Name = "Ana de prueba", Email = "ana@prueba.test", PasswordHash = "no-se-usa" };
            var membresia = new CompanyUser { CompanyId = empresa.Id, UserId = usuario.Id, IsAdmin = false };

            var operador = new Role { Code = "OPERADOR", Name = "Operador" };
            var capturista = new Role { Code = "CAPTURISTA", Name = "Capturista" };

            var permisos = new[]
            {
                new Permission { Code = "roles.read", Name = "Ver roles" },
                new Permission { Code = "roles.write", Name = "Administrar roles" },
                new Permission { Code = "accounts.read", Name = "Consultar cuentas" },
                new Permission { Code = "accounts.write", Name = "Crear cuentas" },
                new Permission { Code = "concepts.read", Name = "Consultar conceptos" },
            };

            db.Companies.Add(empresa);
            db.Users.Add(usuario);
            db.CompanyUsers.Add(membresia);
            db.Roles.AddRange(operador, capturista);
            db.Permissions.AddRange(permisos);
            await db.SaveChangesAsync();

            var porCodigo = permisos.ToDictionary(p => p.Code, p => p.Id);

            // Operador: puede leer y administrar roles
            db.RolePermissions.Add(new RolePermission { RoleId = operador.Id, PermissionId = porCodigo["roles.read"] });
            db.RolePermissions.Add(new RolePermission { RoleId = operador.Id, PermissionId = porCodigo["roles.write"] });

            // Capturista, lista inicial: accounts.read y concepts.read
            db.RolePermissions.Add(new RolePermission { RoleId = capturista.Id, PermissionId = porCodigo["accounts.read"] });
            db.RolePermissions.Add(new RolePermission { RoleId = capturista.Id, PermissionId = porCodigo["concepts.read"] });

            db.CompanyUserRoles.Add(new CompanyUserRole { CompanyUserId = membresia.Id, RoleId = operador.Id });
            await db.SaveChangesAsync();

            return new Datos(membresia.Id, empresa.Id, capturista.Id, porCodigo);
        }

        [Fact]
        public async Task Fallo_despues_del_flush_revierte_todas_las_relaciones()
        {
            using var conexion = new SqliteConnection("DataSource=:memory:");
            conexion.Open();

            Datos datos;

            using (var preparacion = new AppDbContext(Opciones(conexion, conFallo: false)))
            {
                await preparacion.Database.EnsureCreatedAsync();
                datos = await SembrarAsync(preparacion);
            }

            // Lista deseada: activa accounts.write (alta) y desactiva concepts.read (baja).
            // Ambas escrituras llegan a la base antes del fallo.
            // El actor se identifica con su token, no con un id en la operación
            Environment.SetEnvironmentVariable(TokenService.VariableSecreto, "clave-de-prueba-solo-para-tests-0123456789");
            var token = TokenService.CrearToken(datos.ActorId, datos.CompanyId);
            var http = new DefaultHttpContext();
            http.Request.Headers.Authorization = $"Bearer {token}";
            var accesor = new HttpContextAccessor { HttpContext = http };

            var entrada = new SetRolePermissionsInput(
                datos.RolId,
                new List<string> { datos.Permisos["accounts.read"], datos.Permisos["accounts.write"] });

            using (var ctx = new AppDbContext(Opciones(conexion, conFallo: true)))
            {
                var mutacion = new Mutation();

                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    mutacion.SetRolePermissionsAsync(entrada, accesor, ctx));
            }

            // Verificación desde una sesión nueva
            using (var verificacion = new AppDbContext(Opciones(conexion, conFallo: false)))
            {
                var relaciones = await verificacion.RolePermissions
                    .Include(rp => rp.Permission)
                    .Where(rp => rp.RoleId == datos.RolId)
                    .ToListAsync();

                var activos = relaciones
                    .Where(rp => rp.IsActive)
                    .Select(rp => rp.Permission.Code)
                    .OrderBy(c => c)
                    .ToList();

                Assert.Equal(new[] { "accounts.read", "concepts.read" }, activos);
                Assert.DoesNotContain(relaciones, rp => rp.Permission.Code == "accounts.write");
            }
        }
    }
}
