using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RassAppControleFin.Domain.Entities;
using RassAppControleFin.Domain.Entities.Finance;

namespace RassAppControleFin.Infrastructure.Persistence.Configurations;

internal sealed class FinanceConfigurations : IEntityTypeConfiguration<Account>, IEntityTypeConfiguration<Category>,
    IEntityTypeConfiguration<CreditCard>, IEntityTypeConfiguration<FinancialTransaction>, IEntityTypeConfiguration<Transfer>,
    IEntityTypeConfiguration<Budget>, IEntityTypeConfiguration<Goal>, IEntityTypeConfiguration<ExpenseReductionGoal>,
    IEntityTypeConfiguration<LedgerEntry>, IEntityTypeConfiguration<CardMovementRow>,
    IEntityTypeConfiguration<SyncOperationReceipt>, IEntityTypeConfiguration<FinancialOutboxRow>
{
    private static void Aggregate<T>(EntityTypeBuilder<T> b, string table) where T : FinancialAggregate
    {
        b.HasBaseType((Type?)null);
        b.ToTable(table, t =>
        {
            t.HasCheckConstraint("CK_" + table + "_Version", "\"Version\" > 0");
            t.HasCheckConstraint("CK_" + table + "_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
        });
        b.HasKey(x => new { x.TenantId, x.UserId, x.Id });
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.UserId).HasMaxLength(256);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.Ignore(x => x.DomainEvents);
        b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
    private static void Row<T>(EntityTypeBuilder<T> b, string table) where T : class, IFinancialResource
    {
        b.ToTable(table, t => t.HasCheckConstraint("CK_" + table + "_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256"));
        b.Property(x => x.UserId).HasMaxLength(256);
        b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
    private static void Currency<T>(EntityTypeBuilder<T> b, string table) where T : class
    {
        b.Property<string>("Currency").HasMaxLength(3);
        b.ToTable(table, t => t.HasCheckConstraint("CK_" + table + "_Currency", "\"Currency\" IN ('BRL','USD','EUR')"));
    }
    public void Configure(EntityTypeBuilder<Account> b)
    {
        Aggregate(b, "Accounts"); Currency(b, "Accounts");
        b.Property(x => x.Name).HasMaxLength(120);
        b.Property(x => x.Balance).HasPrecision(16, 2);
        b.ToTable("Accounts", t => t.HasCheckConstraint("CK_Accounts_Balance", "\"Balance\" >= 0"));
        b.ToTable("Accounts", t => t.HasCheckConstraint("CK_Accounts_Name", "length(btrim(\"Name\")) BETWEEN 1 AND 120"));
    }
    public void Configure(EntityTypeBuilder<Category> b)
    {
        Aggregate(b, "Categories");
        b.Property(x => x.Name).HasMaxLength(120);
        b.Property(x => x.Flow).HasConversion<string>().HasMaxLength(7);
        b.ToTable("Categories", t =>
        {
            t.HasCheckConstraint("CK_Categories_Flow", "\"Flow\" IN ('Income','Expense')");
            t.HasCheckConstraint("CK_Categories_Name", "length(btrim(\"Name\")) BETWEEN 1 AND 120");
        });
    }
    public void Configure(EntityTypeBuilder<CreditCard> b)
    {
        Aggregate(b, "CreditCards"); Currency(b, "CreditCards");
        b.Property(x => x.Name).HasMaxLength(120);
        b.Property(x => x.Limit).HasPrecision(16, 2);
        b.Property(x => x.Outstanding).HasPrecision(16, 2);
        b.ToTable("CreditCards", t =>
        {
            t.HasCheckConstraint("CK_CreditCards_Balances", "\"Limit\" > 0 AND \"Outstanding\" BETWEEN 0 AND \"Limit\"");
            t.HasCheckConstraint("CK_CreditCards_Days", "\"ClosingDay\" BETWEEN 1 AND 28 AND \"DueDay\" BETWEEN 1 AND 28");
            t.HasCheckConstraint("CK_CreditCards_Name", "length(btrim(\"Name\")) BETWEEN 1 AND 120");
        });
    }
    public void Configure(EntityTypeBuilder<FinancialTransaction> b)
    {
        Aggregate(b, "Transactions"); Currency(b, "Transactions");
        b.Ignore(x => x.PostedDelta);
        b.Property(x => x.Amount).HasPrecision(16, 2);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.Flow).HasConversion<string>().HasMaxLength(7);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(9);
        b.HasOne<Account>().WithMany().HasForeignKey(x => new { x.TenantId, x.UserId, x.AccountId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Category>().WithMany().HasForeignKey(x => new { x.TenantId, x.UserId, x.CategoryId }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.UserId, x.OccurredAt, x.Id });
        b.ToTable("Transactions", t =>
        {
            t.HasCheckConstraint("CK_Transactions_Amount", "\"Amount\" > 0");
            t.HasCheckConstraint("CK_Transactions_Flow", "\"Flow\" IN ('Income','Expense')");
            t.HasCheckConstraint("CK_Transactions_Status", "\"Status\" IN ('Pending','Posted','Cancelled')");
        });
    }
    public void Configure(EntityTypeBuilder<Transfer> b)
    {
        Aggregate(b, "Transfers"); Currency(b, "Transfers");
        b.Property(x => x.Amount).HasPrecision(16, 2);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.Status).HasMaxLength(9);
        b.HasIndex(x => new { x.TenantId, x.OperationId }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.UserId, x.OccurredAt, x.Id });
        b.HasOne<Account>().WithMany().HasForeignKey(x => new { x.TenantId, x.UserId, x.SourceAccountId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Account>().WithMany().HasForeignKey(x => new { x.TenantId, x.UserId, x.DestinationAccountId }).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("Transfers", t =>
        {
            t.HasCheckConstraint("CK_Transfers_Amount", "\"Amount\" > 0");
            t.HasCheckConstraint("CK_Transfers_Accounts", "\"SourceAccountId\" <> \"DestinationAccountId\"");
            t.HasCheckConstraint("CK_Transfers_Status", "\"Status\" = 'Posted'");
        });
    }
    public void Configure(EntityTypeBuilder<Budget> b)
    {
        Aggregate(b, "Budgets"); Currency(b, "Budgets");
        b.Property(x => x.Amount).HasPrecision(16, 2);
        b.HasOne<Category>().WithMany().HasForeignKey(x => new { x.TenantId, x.UserId, x.CategoryId }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.UserId, x.PeriodStart, x.PeriodEnd });
        b.ToTable("Budgets", t =>
        {
            t.HasCheckConstraint("CK_Budgets_Amount", "\"Amount\" > 0");
            t.HasCheckConstraint("CK_Budgets_Period", "\"PeriodStart\" < \"PeriodEnd\"");
        });
    }
    public void Configure(EntityTypeBuilder<Goal> b)
    {
        Aggregate(b, "Goals"); Currency(b, "Goals");
        b.Property(x => x.Name).HasMaxLength(120);
        b.Property(x => x.TargetAmount).HasPrecision(16, 2);
        b.Property(x => x.CurrentAmount).HasPrecision(16, 2);
        b.HasIndex(x => new { x.TenantId, x.UserId, x.Status });
        b.ToTable("Goals", t =>
        {
            t.HasCheckConstraint("CK_Goals_Amounts", "\"TargetAmount\" > 0 AND \"CurrentAmount\" BETWEEN 0 AND \"TargetAmount\"");
            t.HasCheckConstraint("CK_Goals_Status", "\"Status\" IN ('Active','Completed','Archived')");
            t.HasCheckConstraint("CK_Goals_Name", "length(btrim(\"Name\")) BETWEEN 1 AND 120");
        });
    }
    public void Configure(EntityTypeBuilder<ExpenseReductionGoal> b)
    {
        Aggregate(b, "ExpenseReductionGoals"); Currency(b, "ExpenseReductionGoals");
        b.Property(x => x.ReferenceAmount).HasPrecision(16, 2);
        b.Property(x => x.TargetAmount).HasPrecision(16, 2);
        b.HasOne<Category>().WithMany().HasForeignKey(x => new { x.TenantId, x.UserId, x.CategoryId }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.UserId, x.PeriodStart, x.PeriodEnd });
        b.ToTable("ExpenseReductionGoals", t =>
        {
            t.HasCheckConstraint("CK_ExpenseReductionGoals_Amounts", "\"ReferenceAmount\" > \"TargetAmount\" AND \"TargetAmount\" >= 0");
            t.HasCheckConstraint("CK_ExpenseReductionGoals_Period", "\"PeriodStart\" < \"PeriodEnd\"");
        });
    }
    public void Configure(EntityTypeBuilder<LedgerEntry> b)
    {
        Row(b, "LedgerEntries");
        b.HasKey(x => new { x.TenantId, x.UserId, x.Id });
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Delta).HasPrecision(16, 2);
        b.Property(x => x.Kind).HasMaxLength(16);
        b.HasOne<Account>().WithMany().HasForeignKey(x => new { x.TenantId, x.UserId, x.AccountId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Transfer>().WithMany().HasForeignKey(x => new { x.TenantId, x.UserId, x.TransferId }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.UserId, x.AccountId });
        b.HasIndex(x => new { x.TenantId, x.UserId, x.TransferId });
        b.ToTable("LedgerEntries", t =>
        {
            t.HasCheckConstraint("CK_LedgerEntries_Kind", "\"Kind\" IN ('Opening','Transaction','Adjustment','Reversal','Transfer','CardPayment')");
            t.HasCheckConstraint("CK_LedgerEntries_Delta", "\"Delta\" <> 0");
            t.HasCheckConstraint("CK_LedgerEntries_Transfer", "(\"Kind\" = 'Transfer') = (\"TransferId\" IS NOT NULL) AND (\"TransferId\" IS NULL OR \"ResourceId\" = \"TransferId\")");
        });
    }
    public void Configure(EntityTypeBuilder<CardMovementRow> b)
    {
        Row(b, "CardMovements"); Currency(b, "CardMovements");
        b.HasKey(x => new { x.TenantId, x.UserId, x.Id });
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Amount).HasPrecision(16, 2);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.Kind).HasMaxLength(8);
        b.HasOne<CreditCard>().WithMany().HasForeignKey(x => new { x.TenantId, x.UserId, x.CardId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Account>().WithMany().HasForeignKey(x => new { x.TenantId, x.UserId, x.AccountId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Category>().WithMany().HasForeignKey(x => new { x.TenantId, x.UserId, x.CategoryId }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.UserId, x.CardId, x.OccurredAt, x.Id });
        b.ToTable("CardMovements", t =>
        {
            t.HasCheckConstraint("CK_CardMovements_Amount", "\"Amount\" > 0");
            t.HasCheckConstraint("CK_CardMovements_Shape", "(\"Kind\"='Purchase' AND \"CategoryId\" IS NOT NULL AND \"AccountId\" IS NULL) OR (\"Kind\"='Payment' AND \"AccountId\" IS NOT NULL AND \"CategoryId\" IS NULL)");
        });
    }
    public void Configure(EntityTypeBuilder<SyncOperationReceipt> b)
    {
        Row(b, "SyncOperations");
        b.HasKey(x => new { x.TenantId, x.OperationId });
        b.Property(x => x.OperationId).ValueGeneratedNever();
        b.Property(x => x.PayloadHash).HasMaxLength(64);
        b.Property(x => x.ResponseJson).HasColumnType("jsonb");
        b.ToTable("SyncOperations", t => t.HasCheckConstraint("CK_SyncOperations_Hash", "length(\"PayloadHash\")=64"));
    }
    public void Configure(EntityTypeBuilder<FinancialOutboxRow> b)
    {
        Row(b, "FinancialOutbox");
        b.HasKey(x => new { x.TenantId, x.UserId, x.Id });
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.EventType).HasMaxLength(64);
        b.HasIndex(x => new { x.TenantId, x.UserId, x.CreatedAt, x.Id });
    }
}
