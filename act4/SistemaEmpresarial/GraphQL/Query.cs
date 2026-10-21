using HotChocolate;
using Microsoft.EntityFrameworkCore;
using SistemaEmpresarial.Acceso;
using SistemaEmpresarial.Data;
using SistemaEmpresarial.Models;

namespace SistemaEmpresarial.GraphQL
{
    public partial class Query
    {
        public IQueryable<Company> GetCompanies(
            AppDbContext context,
            bool? activeOnly = null)
        {
            if (activeOnly.HasValue && activeOnly.Value)
                return context.Companies.Where(c => c.IsActive);

            return context.Companies;
        }

        public Company? GetCompany(
            string id,
            AppDbContext context)
        {
            return context.Companies.FirstOrDefault(c => c.Id == id);
        }

        public async Task<IQueryable<Account>> GetAccounts(
            string companyId,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context,
            AccountType? accountType = null,
            bool? activeOnly = null)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            var actor = await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.AccountsRead);
            AccessControl.ExigirMismaEmpresa(actor, companyId);

            var query = context.Accounts
                .Include(a => a.Company)
                .Where(a => a.CompanyId == companyId);

            if (accountType.HasValue)
                query = query.Where(a => a.AccountType == accountType.Value);

            if (activeOnly.HasValue && activeOnly.Value)
                query = query.Where(a => a.IsActive);

            return query;
        }

        public async Task<IQueryable<Concept>> GetConcepts(
            string companyId,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context,
            ConceptType? conceptType = null,
            bool? activeOnly = null)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            var actor = await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.ConceptsRead);
            AccessControl.ExigirMismaEmpresa(actor, companyId);

            var query = context.Concepts
                .Where(c => c.CompanyId == companyId);

            if (conceptType.HasValue)
                query = query.Where(c => c.ConceptType == conceptType.Value);

            if (activeOnly.HasValue && activeOnly.Value)
                query = query.Where(c => c.IsActive);

            return query;
        }

        public async Task<IQueryable<AccountConcept>> GetAccountConcepts(
            string accountId,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            var actor = await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.ConceptsRead);

            var account = await context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);

            if (account == null)
                throw new GraphQLException("La cuenta no existe");

            AccessControl.ExigirMismaEmpresa(actor, account.CompanyId);

            return context.AccountConcepts
                .Include(ac => ac.Account)
                .Include(ac => ac.Concept)
                .Where(ac => ac.AccountId == accountId);
        }

        // Los movimientos que se devuelven son los de la empresa de la membresía que consulta
        public async Task<IQueryable<Transaction>> GetTransactions(
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context,
            string? accountId = null,
            TransactionType? transactionType = null,
            int? limit = null,
            int? offset = null)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            var actor = await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.TransactionsRead);

            var query = context.Transactions
                .Include(t => t.Account)
                .Include(t => t.Concept)
                .Include(t => t.CapturedByUser)
                .Where(t =>
                    t.IsActive &&
                    t.Account != null &&
                    t.Account.CompanyId == actor.CompanyId);

            if (!string.IsNullOrWhiteSpace(accountId))
                query = query.Where(t => t.AccountId == accountId);

            if (transactionType.HasValue)
                query = query.Where(t => t.TransactionType == transactionType.Value);

            query = query.OrderByDescending(t => t.TransactionDate);

            if (offset.HasValue && offset.Value > 0)
                query = query.Skip(offset.Value);

            if (limit.HasValue && limit.Value > 0)
                query = query.Take(limit.Value);

            return query;
        }

        // ---------- Roles y permisos ----------

        public async Task<IQueryable<Role>> GetRoles(
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context,
            bool? activeOnly = null)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.RolesRead);

            if (activeOnly.HasValue && activeOnly.Value)
                return context.Roles.Where(r => r.IsActive).OrderBy(r => r.Code);

            return context.Roles.OrderBy(r => r.Code);
        }

        public async Task<Role?> GetRole(
            string id,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.RolesRead);

            return await context.Roles.FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IQueryable<Permission>> GetPermissions(
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context,
            bool? activeOnly = null)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.RolesRead);

            if (activeOnly.HasValue && activeOnly.Value)
                return context.Permissions.Where(p => p.IsActive).OrderBy(p => p.Code);

            return context.Permissions.OrderBy(p => p.Code);
        }

        // Permisos activos de un rol
        public async Task<IQueryable<RolePermission>> GetRolePermissions(
            string roleId,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.RolesRead);

            return context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .Where(rp => rp.RoleId == roleId && rp.IsActive)
                .OrderBy(rp => rp.Permission.Code);
        }

        // Membresías de la empresa del actor (para conocer los companyUserId)
        public async Task<IQueryable<CompanyUser>> GetCompanyUsers(
            string companyId,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            var actor = await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.CompanyUsersRead);
            AccessControl.ExigirMismaEmpresa(actor, companyId);

            return context.CompanyUsers
                .Include(cu => cu.User)
                .Include(cu => cu.Company)
                .Where(cu => cu.CompanyId == companyId);
        }

        // Rol activo de cada membresía de la empresa del actor
        public async Task<IQueryable<CompanyUserRole>> GetCompanyUserRoles(
            string companyId,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context)
        {
            var actorCompanyUserId = await ActorDesdeToken.LeerAsync(context, httpContextAccessor);
            var actor = await AccessControl.ExigirPermisoAsync(context, actorCompanyUserId, Perm.CompanyUsersRead);
            AccessControl.ExigirMismaEmpresa(actor, companyId);

            return context.CompanyUserRoles
                .Include(cur => cur.CompanyUser).ThenInclude(cu => cu.User)
                .Include(cur => cur.Role)
                .Where(cur => cur.IsActive && cur.CompanyUser.CompanyId == companyId);
        }
    }
}
