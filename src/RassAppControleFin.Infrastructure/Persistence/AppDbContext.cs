using Microsoft.EntityFrameworkCore;
using RassAppControleFin.Domain.Entities;
using RassAppControleFin.Domain.Entities.Finance;

namespace RassAppControleFin.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    private Guid? _tenantId;
    private string? _userId;
    private string? _platformActor;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();
    public DbSet<CreditCardPurchase> CreditCardPurchases => Set<CreditCardPurchase>();
    public DbSet<CreditCardInvoice> CreditCardInvoices => Set<CreditCardInvoice>();
    public DbSet<CreditCardInstallment> CreditCardInstallments => Set<CreditCardInstallment>();
    public DbSet<FinancialTransaction> Transactions => Set<FinancialTransaction>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<ExpenseReductionGoal> ExpenseReductionGoals => Set<ExpenseReductionGoal>();
    public DbSet<FinancialNotification> FinancialNotifications => Set<FinancialNotification>();
    public DbSet<PayYourselfFirstPlan> PayYourselfFirstPlans => Set<PayYourselfFirstPlan>();
    public DbSet<PayYourselfFirstPlanRevision> PayYourselfFirstPlanRevisions => Set<PayYourselfFirstPlanRevision>();
    public DbSet<EmergencyFundPlan> EmergencyFundPlans => Set<EmergencyFundPlan>();
    public DbSet<InvestmentAsset> InvestmentAssets => Set<InvestmentAsset>();
    public DbSet<InvestmentTransaction> InvestmentTransactions => Set<InvestmentTransaction>();
    public DbSet<TargetPortfolioAllocation> TargetPortfolioAllocations => Set<TargetPortfolioAllocation>();
    public DbSet<PlatformRoleAssignment> PlatformRoleAssignments => Set<PlatformRoleAssignment>();

    internal void Authorize(Guid tenantId, string userId)
    {
        _tenantId = tenantId;
        _userId = userId;
    }

    internal void ClearAuthorization()
    {
        ChangeTracker.Clear();
        _tenantId = null;
        _userId = null;
        _platformActor = null;
    }

    internal void AuthorizePlatform(string actor) => _platformActor = actor;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("app");
        modelBuilder.Ignore<FinancialAggregate>();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.Entity<Tenant>().HasQueryFilter(tenant => tenant.Id == _tenantId);
        modelBuilder.Entity<UserProfile>().HasQueryFilter(profile =>
            profile.TenantId == _tenantId && profile.UserId == _userId);
        Scope<Account>(modelBuilder);
        Scope<Category>(modelBuilder);
        Scope<CreditCard>(modelBuilder);
        Scope<CreditCardPurchase>(modelBuilder);
        Scope<CreditCardInvoice>(modelBuilder);
        Scope<CreditCardInstallment>(modelBuilder);
        Scope<FinancialTransaction>(modelBuilder);
        Scope<Transfer>(modelBuilder);
        Scope<Budget>(modelBuilder);
        Scope<Goal>(modelBuilder);
        Scope<ExpenseReductionGoal>(modelBuilder);
        Scope<FinancialNotification>(modelBuilder);
        Scope<PayYourselfFirstPlan>(modelBuilder);
        Scope<PayYourselfFirstPlanRevision>(modelBuilder);
        Scope<EmergencyFundPlan>(modelBuilder);
        Scope<InvestmentAsset>(modelBuilder);
        Scope<InvestmentTransaction>(modelBuilder);
        Scope<TargetPortfolioAllocation>(modelBuilder);
        Scope<LedgerEntry>(modelBuilder);
        Scope<CardMovementRow>(modelBuilder);
        Scope<SyncOperationReceipt>(modelBuilder);
        Scope<FinancialOutboxRow>(modelBuilder);
    }

    private void Scope<T>(ModelBuilder builder) where T : class, IFinancialResource
        => builder.Entity<T>().HasQueryFilter(x => x.TenantId == _tenantId && x.UserId == _userId);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateWrites();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ValidateWrites();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ValidateWrites()
    {
        if (((_tenantId is null || _userId is null) && _platformActor is null)
            || Database.CurrentTransaction is null)
            throw new InvalidOperationException("An authorized tenant transaction is required.");

        foreach (var entry in ChangeTracker.Entries().Where(entry =>
            entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var matches = entry.Entity switch
            {
                Tenant tenant => tenant.Id == _tenantId,
                UserProfile profile => profile.TenantId == _tenantId && profile.UserId == _userId,
                // The service has already verified the actor's own assignment and set
                // the transaction-local RLS gate. A SuperAdmin may revoke another
                // administrator; limiting writes to GrantedBy would make that valid
                // revocation impossible.
                PlatformRoleAssignment => _platformActor is not null,
                IFinancialResource resource => resource.TenantId == _tenantId && resource.UserId == _userId,
                _ => false
            };
            if (!matches) throw new InvalidOperationException("Write outside the authorized scope.");
            // Ledger rows and transfers are immutable. A financial transaction keeps
            // its current lifecycle state/version; its monetary changes are recorded
            // separately as immutable adjustment or reversal ledger rows.
            if (entry.State != EntityState.Added &&
                (entry.Entity is Transfer ||
                 entry.Entity is IAppendOnlyFinancialRecord && entry.Entity is not FinancialTransaction))
                throw new InvalidOperationException("Financial history is append-only.");
        }
    }
}
