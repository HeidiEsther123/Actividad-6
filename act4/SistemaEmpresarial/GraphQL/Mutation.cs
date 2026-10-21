using HotChocolate;
using Microsoft.EntityFrameworkCore;
using SistemaEmpresarial.Acceso;
using SistemaEmpresarial.Data;
using SistemaEmpresarial.Models;
using System.Security.Claims;

namespace SistemaEmpresarial.GraphQL
{
    // Definición de los inputs requeridos por el esquema
    public record CreateCompanyInput(
        string Name,
        string? LegalName,
        string? TaxId,
        string? Email,
        string? Phone
    );

    public record UpdateCompanyInput(
        string Id,
        string? Name,
        string? LegalName,
        string? TaxId,
        string? Email,
        string? Phone,
        bool? IsActive
    );

    // Input para crear el administrador principal
    public class CreateAdminInput
    {
        public string CompanyId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // Input para crear un usuario normal
    public record CreateUserInput(
        string CompanyId,
        string Name,
        string Email,
        string Password
    );

    // Input para crear una cuenta
    public record CreateAccountInput(
        string CompanyId,
        AccountType AccountType,
        string Name,
        string? BankName,
        string? AccountNumber,
        string? Clabe,
        string? CardLastDigits,
        string? ShortDescription,
        string? LongDescription
    );

    // Input para crear un concepto
    public record CreateConceptInput(
        string CompanyId,
        ConceptType ConceptType,
        string Name,
        string? ShortDescription,
        string? LongDescription
    );

    // Input para asignar un concepto a una cuenta
    public record AssignConceptToAccountInput(
        string AccountId,
        string ConceptId
    );

    // Input para registrar un movimiento financiero
    public record CreateTransactionInput(
        string AccountId,
        string ConceptId,
        TransactionType TransactionType,
        decimal Amount,
        DateTime? TransactionDate,
        string? ShortDescription,
        string? LongDescription
    );

    // Inputs de roles y permisos
    public record CreateRoleInput(
        string Code,
        string Name,
        string? Description
    );

    public record CreatePermissionInput(
        string Code,
        string Name,
        string? Description
    );

    public record AssignPermissionToRoleInput(
        string RoleId,
        string PermissionId
    );

    public record AssignRoleInput(
        string CompanyUserId,
        string RoleId
    );

    public partial class Mutation
    {
        // Validar formato del correo electrónico
        private bool IsValidEmail(string email)
        {
            try
            {
                var address = new System.Net.Mail.MailAddress(email);
                return address.Address == email;
            }
            catch
            {
                return false;
            }
        }
        public async Task<Company> CreateCompanyAsync(
            CreateCompanyInput input,
            AppDbContext context)
        {
            var company = new Company
            {
                Name = input.Name,
                LegalName = input.LegalName,
                TaxId = input.TaxId,
                Email = input.Email,
                Phone = input.Phone
            };

            context.Companies.Add(company);
            await context.SaveChangesAsync();

            return company;
        }

        public async Task<Company> UpdateCompanyAsync(
            UpdateCompanyInput input,
            AppDbContext context)
        {
            var company = await context.Companies.FindAsync(input.Id);

            if (company == null)
                throw new Exception("Empresa no encontrada");

            if (input.Name != null)
                company.Name = input.Name;

            if (input.LegalName != null)
                company.LegalName = input.LegalName;

            if (input.TaxId != null)
                company.TaxId = input.TaxId;

            if (input.Email != null)
                company.Email = input.Email;

            if (input.Phone != null)
                company.Phone = input.Phone;

            if (input.IsActive.HasValue)
                company.IsActive = input.IsActive.Value;

            company.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return company;
        }

        public async Task<Company> DeactivateCompanyAsync(
            string id,
            AppDbContext context)
        {
            var company = await context.Companies.FindAsync(id);

            if (company == null)
                throw new Exception("Empresa no encontrada");

            company.IsActive = false;
            company.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return company;
        }

        // Crear administrador principal de una empresa
        public async Task<User> CreateAdminAsync(
            CreateAdminInput input,
            AppDbContext context) 
        {
            // Verificar que la empresa exista
            var company = await context.Companies.FindAsync(input.CompanyId);

            if (company == null)
                throw new Exception("La empresa no existe");

            // Verificar que el correo tenga un formato válido
            var email = input.Email.Trim().ToLowerInvariant();

            if (!IsValidEmail(email))
                throw new Exception("El formato del correo electrónico no es válido");

            // Verificar que el correo no esté registrado
            var existingUser = await context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (existingUser != null)
                throw new Exception("El correo electrónico ya está registrado");

            // Verificar que la empresa todavía no tenga administrador
            var existingAdmin = await context.CompanyUsers
                .AnyAsync(cu =>
                    cu.CompanyId == input.CompanyId &&
                    cu.IsAdmin &&
                    cu.IsActive);

            if (existingAdmin)
                throw new Exception("La empresa ya tiene un administrador principal");

            // Crear el usuario
            var user = new User
            {
                Name = input.Name,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password),
                EmailVerified = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);

            // Relacionar el usuario con la empresa como administrador
            var companyUser = new CompanyUser
            {
                CompanyId = input.CompanyId,
                UserId = user.Id,
                IsAdmin = true,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };

            context.CompanyUsers.Add(companyUser);

            // El administrador principal conserva isAdmin y además recibe el rol ADMINISTRADOR
            var adminRole = await context.Roles
                .FirstOrDefaultAsync(r => r.Code == "ADMINISTRADOR" && r.IsActive);

            if (adminRole == null)
                throw new Exception("El rol ADMINISTRADOR no existe");

            context.CompanyUserRoles.Add(new CompanyUserRole
            {
                CompanyUserId = companyUser.Id,
                RoleId = adminRole.Id,
                IsActive = true
            });

            await context.SaveChangesAsync();

            return user;
        }

        // Crear usuario normal de una empresa
        public async Task<User> CreateUserAsync(
            CreateUserInput input,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            var actor = await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.CompanyUsersWrite);
            AccessControl.ExigirMismaEmpresa(actor, input.CompanyId);

            // Verificar que la empresa exista
            var company = await context.Companies.FindAsync(input.CompanyId);

            if (company == null)
                throw new Exception("La empresa no existe");

            // Verificar que el correo tenga un formato válido
            var email = input.Email.Trim().ToLowerInvariant();

            if (!IsValidEmail(email))
                throw new Exception("El formato del correo electrónico no es válido");

            // Verificar que el correo no esté registrado
            var existingUser = await context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (existingUser != null)
                throw new Exception("El correo electrónico ya está registrado");

            // Crear el usuario
            var user = new User
            {
                Name = input.Name,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password),
                EmailVerified = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);

            // Relacionar el usuario con la empresa como usuario normal
            var companyUser = new CompanyUser
            {
                CompanyId = input.CompanyId,
                UserId = user.Id,
                IsAdmin = false,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };

            context.CompanyUsers.Add(companyUser);

            await context.SaveChangesAsync();

            return user;
        }
        // Crear una cuenta
        public async Task<Account> CreateAccountAsync(
            CreateAccountInput input,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            var actor = await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.AccountsWrite);
            AccessControl.ExigirMismaEmpresa(actor, input.CompanyId);

            // Verificar que la empresa exista
            var company = await context.Companies.FindAsync(input.CompanyId);

            if (company == null)
                throw new Exception("La empresa no existe");

            var name = input.Name.Trim();

            // Evitar cuentas duplicadas dentro de la misma empresa
            var duplicate = await context.Accounts.AnyAsync(a =>
                a.CompanyId == input.CompanyId &&
                a.Name.ToLower() == name.ToLower());

            if (duplicate)
                throw new Exception("Ya existe una cuenta con ese nombre para esta empresa");

            var account = new Account
            {
                CompanyId = input.CompanyId,
                AccountType = input.AccountType,
                Name = name,
                BankName = input.BankName,
                AccountNumber = input.AccountNumber,
                Clabe = input.Clabe,
                CardLastDigits = input.CardLastDigits,
                ShortDescription = input.ShortDescription,
                LongDescription = input.LongDescription
            };

            context.Accounts.Add(account);
            await context.SaveChangesAsync();

            account.Company = company;

            return account;
        }

        // Crear un concepto
        public async Task<Concept> CreateConceptAsync(
            CreateConceptInput input,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            var actor = await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.ConceptsWrite);
            AccessControl.ExigirMismaEmpresa(actor, input.CompanyId);

            // Verificar que la empresa exista
            var company = await context.Companies.FindAsync(input.CompanyId);

            if (company == null)
                throw new Exception("La empresa no existe");

            var name = input.Name.Trim();

            // Evitar conceptos duplicados (mismo nombre y tipo) dentro de la misma empresa
            var duplicate = await context.Concepts.AnyAsync(c =>
                c.CompanyId == input.CompanyId &&
                c.ConceptType == input.ConceptType &&
                c.Name.ToLower() == name.ToLower());

            if (duplicate)
                throw new Exception("Ya existe un concepto con ese nombre y tipo para esta empresa");

            var concept = new Concept
            {
                CompanyId = input.CompanyId,
                ConceptType = input.ConceptType,
                Name = name,
                ShortDescription = input.ShortDescription,
                LongDescription = input.LongDescription
            };

            context.Concepts.Add(concept);
            await context.SaveChangesAsync();

            concept.Company = company;

            return concept;
        }

        // Asignar un concepto a una cuenta
        public async Task<AccountConcept> AssignConceptToAccountAsync(
            AssignConceptToAccountInput input,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            var actor = await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.ConceptsWrite);

            // Verificar que la cuenta exista
            var account = await context.Accounts.FindAsync(input.AccountId);

            if (account == null)
                throw new Exception("La cuenta no existe");

            AccessControl.ExigirMismaEmpresa(actor, account.CompanyId);

            // Verificar que el concepto exista
            var concept = await context.Concepts.FindAsync(input.ConceptId);

            if (concept == null)
                throw new Exception("El concepto no existe");

            // La cuenta y el concepto deben pertenecer a la misma empresa
            if (account.CompanyId != concept.CompanyId)
                throw new Exception("La cuenta y el concepto deben pertenecer a la misma empresa");

            // Verificar si ya existe la relación (activa o inactiva)
            var existing = await context.AccountConcepts.FirstOrDefaultAsync(ac =>
                ac.AccountId == input.AccountId &&
                ac.ConceptId == input.ConceptId);

            if (existing != null)
            {
                if (existing.IsActive)
                    throw new Exception("El concepto ya está asignado a esta cuenta");

                // Reactivar una relación que había sido desactivada
                existing.IsActive = true;
                await context.SaveChangesAsync();

                existing.Account = account;
                existing.Concept = concept;

                return existing;
            }

            var accountConcept = new AccountConcept
            {
                AccountId = input.AccountId,
                ConceptId = input.ConceptId,
                IsActive = true
            };

            context.AccountConcepts.Add(accountConcept);
            await context.SaveChangesAsync();

            accountConcept.Account = account;
            accountConcept.Concept = concept;

            return accountConcept;
        }

        // Desactivar (eliminar lógicamente) la relación entre una cuenta y un concepto
        public async Task<bool> RemoveConceptFromAccountAsync(
            string accountId,
            string conceptId,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            var actor = await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.ConceptsWrite);

            var accountConcept = await context.AccountConcepts
                .Include(ac => ac.Account)
                .FirstOrDefaultAsync(ac =>
                    ac.AccountId == accountId &&
                    ac.ConceptId == conceptId);

            if (accountConcept == null)
                throw new Exception("La relación entre la cuenta y el concepto no existe");

            AccessControl.ExigirMismaEmpresa(actor, accountConcept.Account!.CompanyId);

            if (!accountConcept.IsActive)
                throw new Exception("La relación ya se encuentra desactivada");

            accountConcept.IsActive = false;
            await context.SaveChangesAsync();

            return true;
        }


        // Registrar un movimiento financiero
        public async Task<Transaction> CreateTransactionAsync(
            CreateTransactionInput input,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            // 1. Exigir el permiso antes de validar o escribir cualquier cosa
            var actor = await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.TransactionsWrite);

            // 2. Validar monto
            if (input.Amount <= 0)
                throw new Exception("El monto debe ser mayor que 0");

            // 3. Buscar cuenta
            var account = await context.Accounts
                .FirstOrDefaultAsync(a => a.Id == input.AccountId);

            if (account == null)
                throw new Exception("La cuenta no existe");

            if (!account.IsActive)
                throw new Exception("La cuenta está inactiva");

            // 4. Buscar concepto
            var concept = await context.Concepts
                .FirstOrDefaultAsync(c => c.Id == input.ConceptId);

            if (concept == null)
                throw new Exception("El concepto no existe");

            if (!concept.IsActive)
                throw new Exception("El concepto está inactivo");

            // 5. Cuenta y concepto deben pertenecer a la misma empresa
            if (account.CompanyId != concept.CompanyId)
                throw new Exception("La cuenta y el concepto deben pertenecer a la misma empresa");

            // 6. El concepto debe estar permitido para la cuenta
            var relation = await context.AccountConcepts
                .FirstOrDefaultAsync(ac =>
                    ac.AccountId == input.AccountId &&
                    ac.ConceptId == input.ConceptId &&
                    ac.IsActive);

            if (relation == null)
                throw new Exception("El concepto no está permitido para la cuenta seleccionada");

            // 7. El usuario que captura es el de la membresía que actúa (ya validado como activo)
            var user = actor.User!;

            // 8. La membresía debe pertenecer a la misma empresa que la cuenta
            if (actor.CompanyId != account.CompanyId)
                throw new Exception("El usuario no pertenece a la misma empresa que la cuenta");

            // 9. El tipo de movimiento debe coincidir con el tipo del concepto
            if (input.TransactionType == TransactionType.INCOME &&
                concept.ConceptType != ConceptType.INCOME)
                throw new Exception("El tipo INCOME no es compatible con el concepto seleccionado");

            if (input.TransactionType == TransactionType.EXPENSE &&
                concept.ConceptType != ConceptType.EXPENSE)
                throw new Exception("El tipo EXPENSE no es compatible con el concepto seleccionado");

            // 10. Crear movimiento
            var transaction = new Transaction
            {
                AccountId = account.Id,
                ConceptId = concept.Id,
                TransactionType = input.TransactionType,
                Amount = input.Amount,
                TransactionDate = input.TransactionDate ?? DateTime.UtcNow,
                CapturedAt = DateTime.UtcNow,
                CapturedBy = user.Id,
                ShortDescription = input.ShortDescription,
                LongDescription = input.LongDescription,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Transactions.Add(transaction);
            await context.SaveChangesAsync();

            transaction.Account = account;
            transaction.Concept = concept;
            transaction.CapturedByUser = user;

            return transaction;
        }

        // Eliminación lógica de un movimiento
        public async Task<Transaction> DeleteTransactionAsync(
            string id,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            var actor = await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.TransactionsWrite);

            var transaction = await context.Transactions
                .Include(t => t.Account)
                .Include(t => t.Concept)
                .Include(t => t.CapturedByUser)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
                throw new Exception("El movimiento no existe");

            if (!transaction.IsActive)
                throw new Exception("El movimiento ya está eliminado");

            // El usuario que elimina debe pertenecer a la empresa del movimiento
            var companyId = transaction.Account?.CompanyId;

            if (companyId == null)
                throw new Exception("La cuenta del movimiento no existe");

            if (actor.CompanyId != companyId)
                throw new Exception("El usuario no pertenece a la misma empresa que el movimiento");

            transaction.IsActive = false;
            transaction.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return transaction;
        }

        // ---------- Roles y permisos ----------

        public async Task<Role> CreateRoleAsync(
            CreateRoleInput input,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.RolesWrite);

            var code = input.Code.Trim().ToUpperInvariant();
            var name = input.Name.Trim();

            if (code.Length == 0 || name.Length == 0)
                throw new GraphQLException("El código y el nombre del rol son obligatorios");

            if (await context.Roles.AnyAsync(r => r.Code == code))
                throw new GraphQLException("Ya existe un rol con ese código");

            var role = new Role
            {
                Code = code,
                Name = name,
                Description = input.Description
            };

            context.Roles.Add(role);
            await context.SaveChangesAsync();

            return role;
        }

        // No hay borrado físico: el rol se desactiva
        public async Task<Role> DeactivateRoleAsync(
            string id,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.RolesWrite);

            var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
                throw new GraphQLException("El rol no existe");

            if (role.Code == "ADMINISTRADOR")
                throw new GraphQLException("El rol ADMINISTRADOR no se puede desactivar");

            if (!role.IsActive)
                throw new GraphQLException("El rol ya está desactivado");

            role.IsActive = false;
            await context.SaveChangesAsync();

            return role;
        }

        public async Task<Permission> CreatePermissionAsync(
            CreatePermissionInput input,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.RolesWrite);

            var code = input.Code.Trim().ToLowerInvariant();
            var name = input.Name.Trim();

            if (code.Length == 0 || name.Length == 0)
                throw new GraphQLException("El código y el nombre del permiso son obligatorios");

            if (await context.Permissions.AnyAsync(p => p.Code == code))
                throw new GraphQLException("Ya existe un permiso con ese código");

            var permission = new Permission
            {
                Code = code,
                Name = name,
                Description = input.Description
            };

            context.Permissions.Add(permission);
            await context.SaveChangesAsync();

            return permission;
        }

        // Falla si la pareja ya está activa; si estaba desactivada, se reactiva
        public async Task<RolePermission> AssignPermissionToRoleAsync(
            AssignPermissionToRoleInput input,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.RolesWrite);

            var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == input.RoleId);

            if (role == null || !role.IsActive)
                throw new GraphQLException("El rol no existe o está inactivo");

            var permission = await context.Permissions.FirstOrDefaultAsync(p => p.Id == input.PermissionId);

            if (permission == null || !permission.IsActive)
                throw new GraphQLException("El permiso no existe o está inactivo");

            var existing = await context.RolePermissions.FirstOrDefaultAsync(rp =>
                rp.RoleId == input.RoleId &&
                rp.PermissionId == input.PermissionId);

            if (existing != null)
            {
                if (existing.IsActive)
                    throw new GraphQLException("El rol ya tiene asignado ese permiso");

                existing.IsActive = true;
                await context.SaveChangesAsync();

                existing.Role = role;
                existing.Permission = permission;

                return existing;
            }

            var rolePermission = new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id,
                IsActive = true
            };

            context.RolePermissions.Add(rolePermission);
            await context.SaveChangesAsync();

            rolePermission.Role = role;
            rolePermission.Permission = permission;

            return rolePermission;
        }

        // Desactiva la pareja, no borra la fila
        public async Task<bool> RemovePermissionFromRoleAsync(
            string roleId,
            string permissionId,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.RolesWrite);

            var rolePermission = await context.RolePermissions.FirstOrDefaultAsync(rp =>
                rp.RoleId == roleId &&
                rp.PermissionId == permissionId);

            if (rolePermission == null)
                throw new GraphQLException("El rol no tiene asignado ese permiso");

            if (!rolePermission.IsActive)
                throw new GraphQLException("El permiso ya está desactivado para ese rol");

            rolePermission.IsActive = false;
            await context.SaveChangesAsync();

            return true;
        }

        // Deja un solo rol activo por membresía
        public async Task<CompanyUserRole> AssignRoleAsync(
            AssignRoleInput input,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            var actor = await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.RolesWrite);

            var target = await context.CompanyUsers
                .Include(cu => cu.User)
                .FirstOrDefaultAsync(cu => cu.Id == input.CompanyUserId);

            if (target == null || !target.IsActive)
                throw new GraphQLException("La membresía no existe o está inactiva");

            // La frontera entre empresas se mantiene: el actor solo asigna roles en su empresa
            AccessControl.ExigirMismaEmpresa(actor, target.CompanyId);

            var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == input.RoleId);

            if (role == null || !role.IsActive)
                throw new GraphQLException("El rol no existe o está inactivo");

            // isAdmin sigue siendo la regla de un solo administrador principal
            if (role.Code == "ADMINISTRADOR" && !target.IsAdmin)
                throw new GraphQLException("El rol ADMINISTRADOR es solo del administrador principal");

            if (target.IsAdmin && role.Code != "ADMINISTRADOR")
                throw new GraphQLException("El administrador principal conserva el rol ADMINISTRADOR");

            var actuales = await context.CompanyUserRoles
                .Where(cur => cur.CompanyUserId == target.Id && cur.IsActive)
                .ToListAsync();

            var mismo = actuales.FirstOrDefault(cur => cur.RoleId == role.Id);

            if (mismo != null && actuales.Count == 1)
            {
                mismo.CompanyUser = target;
                mismo.Role = role;
                return mismo;
            }

            // Primero se desactiva el rol anterior y luego se inserta el nuevo,
            // para no chocar con el índice de un solo rol activo por membresía
            await using var tx = await context.Database.BeginTransactionAsync();

            foreach (var anterior in actuales)
                anterior.IsActive = false;

            await context.SaveChangesAsync();

            var nuevo = new CompanyUserRole
            {
                CompanyUserId = target.Id,
                RoleId = role.Id,
                IsActive = true
            };

            context.CompanyUserRoles.Add(nuevo);
            await context.SaveChangesAsync();
            await tx.CommitAsync();

            nuevo.CompanyUser = target;
            nuevo.Role = role;

            return nuevo;
        }

    }
}
