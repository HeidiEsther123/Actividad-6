using Microsoft.EntityFrameworkCore;
using SistemaEmpresarial.Models;

namespace SistemaEmpresarial.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Company> Companies { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<CompanyUser> CompanyUsers { get; set; }

        public DbSet<Account> Accounts { get; set; }

        public DbSet<Concept> Concepts { get; set; }

        public DbSet<AccountConcept> AccountConcepts { get; set; }

        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<Role> Roles { get; set; }

        public DbSet<Permission> Permissions { get; set; }

        public DbSet<RolePermission> RolePermissions { get; set; }

        public DbSet<CompanyUserRole> CompanyUserRoles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Un correo electrónico no puede estar registrado más de una vez
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Relación entre CompanyUser y Company
            modelBuilder.Entity<CompanyUser>()
                .HasOne(cu => cu.Company)
                .WithMany()
                .HasForeignKey(cu => cu.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación entre CompanyUser y User
            modelBuilder.Entity<CompanyUser>()
                .HasOne(cu => cu.User)
                .WithMany()
                .HasForeignKey(cu => cu.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Una empresa solo puede tener un administrador principal
            modelBuilder.Entity<CompanyUser>()
                .HasIndex(cu => cu.CompanyId)
                .IsUnique()
                .HasFilter("\"IsAdmin\" = TRUE");

            // Relación entre Account y Company
            modelBuilder.Entity<Account>()
                .HasOne(a => a.Company)
                .WithMany()
                .HasForeignKey(a => a.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // No se permiten dos cuentas con el mismo nombre dentro de la misma empresa
            modelBuilder.Entity<Account>()
                .HasIndex(a => new { a.CompanyId, a.Name })
                .IsUnique();

            // Relación entre Concept y Company
            modelBuilder.Entity<Concept>()
                .HasOne(c => c.Company)
                .WithMany()
                .HasForeignKey(c => c.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // No se permiten dos conceptos con el mismo nombre y tipo dentro de la misma empresa
            modelBuilder.Entity<Concept>()
                .HasIndex(c => new { c.CompanyId, c.Name, c.ConceptType })
                .IsUnique();

            // Relación entre AccountConcept y Account
            modelBuilder.Entity<AccountConcept>()
                .HasOne(ac => ac.Account)
                .WithMany()
                .HasForeignKey(ac => ac.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación entre AccountConcept y Concept
            modelBuilder.Entity<AccountConcept>()
                .HasOne(ac => ac.Concept)
                .WithMany()
                .HasForeignKey(ac => ac.ConceptId)
                .OnDelete(DeleteBehavior.Cascade);

            // Una misma cuenta no puede tener el mismo concepto asignado dos veces
            modelBuilder.Entity<AccountConcept>()
                .HasIndex(ac => new { ac.AccountId, ac.ConceptId })
                .IsUnique();

            // Relación entre Transaction y Account
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Account)
                .WithMany()
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación entre Transaction y Concept
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Concept)
                .WithMany()
                .HasForeignKey(t => t.ConceptId)
                .OnDelete(DeleteBehavior.Restrict);

            // Usuario que capturó el movimiento
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.CapturedByUser)
                .WithMany()
                .HasForeignKey(t => t.CapturedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Evita montos negativos o cero a nivel de modelo/BD cuando sea soportado.
            modelBuilder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasPrecision(18, 2);

            // ---------- Roles y permisos (Actividad 3) ----------

            // El código del rol y el del permiso son únicos
            modelBuilder.Entity<Role>()
                .HasIndex(r => r.Code)
                .IsUnique();

            modelBuilder.Entity<Permission>()
                .HasIndex(p => p.Code)
                .IsUnique();

            // Relación entre RolePermission y Role
            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany()
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación entre RolePermission y Permission
            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany()
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Una pareja rol-permiso no se repite (se reactiva en lugar de duplicarse)
            modelBuilder.Entity<RolePermission>()
                .HasIndex(rp => new { rp.RoleId, rp.PermissionId })
                .IsUnique();

            // Relación entre CompanyUserRole y CompanyUser
            modelBuilder.Entity<CompanyUserRole>()
                .HasOne(cur => cur.CompanyUser)
                .WithMany()
                .HasForeignKey(cur => cur.CompanyUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación entre CompanyUserRole y Role
            modelBuilder.Entity<CompanyUserRole>()
                .HasOne(cur => cur.Role)
                .WithMany()
                .HasForeignKey(cur => cur.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cada membresía tiene un solo rol activo
            modelBuilder.Entity<CompanyUserRole>()
                .HasIndex(cur => cur.CompanyUserId)
                .IsUnique()
                .HasFilter("\"IsActive\" = TRUE");
        }
    }
}