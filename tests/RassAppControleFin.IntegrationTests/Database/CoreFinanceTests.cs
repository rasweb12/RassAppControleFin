using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RassAppControleFin.Application.Features.Finance;
using RassAppControleFin.Domain.Entities.Finance;
using RassAppControleFin.Infrastructure.Configuration;
using RassAppControleFin.Infrastructure.Persistence;
using RassAppControleFin.Infrastructure.Persistence.Seed;
using RassAppControleFin.MultiTenancy;
using RassAppControleFin.Security;
using RassAppControleFin.SharedKernel.Results;
using Xunit;

namespace RassAppControleFin.IntegrationTests.Database;

[Collection(PostgresCollection.Name)]
public sealed class CoreFinanceTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Transfer_is_atomic_idempotent_and_balanced()
    {
        await using var db = fixture.CreateContext();
        var user = new FinanceUser(DevelopmentSeed.Alice);
        var handler = new FinanceMutationHandlers(
            new FinanceUnitFactory(db, TransactionFactory(db, user), new TestTenant(DevelopmentSeed.TenantA), user),
            user, new TestTenant(DevelopmentSeed.TenantA));
        var source = await handler.Handle(new OpenAccountCommand(Guid.NewGuid(), "Conta A", "BRL", 1000m), default);
        var destination = await handler.Handle(new OpenAccountCommand(Guid.NewGuid(), "Conta B", "BRL", 500m), default);
        Assert.True(source.IsSuccess, source.Error?.Code);
        Assert.True(destination.IsSuccess, destination.Error?.Code);

        var command = new TransferCommand(Guid.NewGuid(), source.Value.ResourceId, destination.Value.ResourceId,
            300m, "BRL", DateTimeOffset.UtcNow, "Teste critico", source.Value.Version, destination.Value.Version);
        var transfer = await handler.Handle(command, default);
        Assert.True(transfer.IsSuccess, transfer.Error?.Code);
        Assert.Equal(700m, transfer.Value.SourceBalance);
        Assert.Equal(800m, transfer.Value.DestinationBalance);

        var replay = await handler.Handle(command, default);
        Assert.True(replay.IsSuccess, replay.Error?.Code);
        Assert.Equal(transfer.Value, replay.Value);

        var mismatch = await handler.Handle(command with { Amount = 299m }, default);
        Assert.False(mismatch.IsSuccess);
        Assert.Equal("OPERATION_REUSED", mismatch.Error!.Code);

        var verification = await TransactionFactory(db, user).BeginAsync(default);
        Assert.True(verification.IsSuccess);
        await using var scope = verification.Value;
        var ids = new[] { source.Value.ResourceId, destination.Value.ResourceId };
        var accounts = await db.Accounts.Where(x => ids.Contains(x.Id)).OrderBy(x => x.Name).ToListAsync();
        Assert.Collection(accounts,
            x => Assert.Equal(700m, x.Balance),
            x => Assert.Equal(800m, x.Balance));
        var ledgerCount = await db.Database.SqlQuery<int>($"""
            SELECT count(*)::integer AS "Value" FROM app."LedgerEntries"
            WHERE "TransferId" = {transfer.Value.ResourceId}
            """).SingleAsync();
        var ledgerTotal = await db.Database.SqlQuery<decimal>($"""
            SELECT coalesce(sum("Delta"), 0) AS "Value" FROM app."LedgerEntries"
            WHERE "TransferId" = {transfer.Value.ResourceId}
            """).SingleAsync();
        Assert.Equal(2, ledgerCount);
        Assert.Equal(0m, ledgerTotal);
    }

    [Fact]
    public async Task Sync_pull_is_paged_and_isolated_by_the_runtime_user()
    {
        await using var db = fixture.CreateContext();
        var alice = new FinanceUser(DevelopmentSeed.Alice);
        var aliceFactory = new FinanceUnitFactory(db, TransactionFactory(db, alice),
            new TestTenant(DevelopmentSeed.TenantA), alice);
        var handler = new FinanceMutationHandlers(aliceFactory, alice,
            new TestTenant(DevelopmentSeed.TenantA));
        // Start after a local marker so retained development outbox rows cannot affect this paging assertion.
        var createdAfter = DateTimeOffset.UtcNow.AddSeconds(-1);

        var firstAccount = await handler.Handle(new OpenAccountCommand(Guid.NewGuid(), "Sync A", "BRL", 10m), default);
        var secondAccount = await handler.Handle(new OpenAccountCommand(Guid.NewGuid(), "Sync B", "BRL", 20m), default);
        Assert.True(firstAccount.IsSuccess, firstAccount.Error?.Code);
        Assert.True(secondAccount.IsSuccess, secondAccount.Error?.Code);

        var aliceStore = new FinanceSyncReadStore(db, aliceFactory);
        var firstPage = await aliceStore.PullAsync(createdAfter, Guid.Empty, 1, default);
        Assert.True(firstPage.IsSuccess, firstPage.Error?.Code);
        Assert.Single(firstPage.Value.Changes);
        Assert.True(firstPage.Value.HasMore);

        var marker = firstPage.Value.Changes.Single();
        var secondPage = await aliceStore.PullAsync(marker.OccurredAt, marker.CursorId, 10, default);
        Assert.True(secondPage.IsSuccess, secondPage.Error?.Code);
        var returnedIds = firstPage.Value.Changes.Concat(secondPage.Value.Changes)
            .Select(change => change.ResourceId).ToHashSet();
        Assert.Contains(firstAccount.Value.ResourceId, returnedIds);
        Assert.Contains(secondAccount.Value.ResourceId, returnedIds);

        var bob = new FinanceUser(DevelopmentSeed.Bob);
        var bobFactory = new FinanceUnitFactory(db, TransactionFactory(db, bob),
            new TestTenant(DevelopmentSeed.TenantA), bob);
        var bobStore = new FinanceSyncReadStore(db, bobFactory);
        var bobPage = await bobStore.PullAsync(null, null, 10, default);
        Assert.True(bobPage.IsSuccess, bobPage.Error?.Code);
        Assert.DoesNotContain(bobPage.Value.Changes, change =>
            change.ResourceId == firstAccount.Value.ResourceId ||
            change.ResourceId == secondAccount.Value.ResourceId);
    }

    [Fact]
    public async Task Financial_rls_hides_another_user_rows()
    {
        await using var db = fixture.CreateContext();
        var alice = new FinanceUser(DevelopmentSeed.Alice);
        var handler = new FinanceMutationHandlers(
            new FinanceUnitFactory(db, TransactionFactory(db, alice), new TestTenant(DevelopmentSeed.TenantA), alice),
            alice, new TestTenant(DevelopmentSeed.TenantA));
        var created = await handler.Handle(new OpenAccountCommand(Guid.NewGuid(), "Alice isolada", "BRL", 1m), default);
        Assert.True(created.IsSuccess, created.Error?.Code);

        var bob = new FinanceUser(DevelopmentSeed.Bob);
        var denied = await TransactionFactory(db, bob).BeginAsync(default);
        Assert.True(denied.IsSuccess);
        await using var scope = denied.Value;
        Assert.Empty(await db.Accounts.Where(x => x.Id == created.Value.ResourceId).ToListAsync());
    }

    [Fact]
    public async Task Budget_and_goal_are_scoped_and_queryable()
    {
        await using var db = fixture.CreateContext();
        var alice = new FinanceUser(DevelopmentSeed.Alice);
        var factory = new FinanceUnitFactory(db, TransactionFactory(db, alice), new TestTenant(DevelopmentSeed.TenantA), alice);
        var handler = new FinanceMutationHandlers(factory, alice, new TestTenant(DevelopmentSeed.TenantA));
        var periodStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var budget = await handler.Handle(new CreateBudgetCommand(Guid.NewGuid(), null, 400m, "BRL",
            periodStart, periodStart.AddMonths(1)), default);
        var goal = await handler.Handle(new CreateGoalCommand(Guid.NewGuid(), "Reserva Sprint", 1000m, "BRL",
            periodStart.AddMonths(6)), default);
        Assert.True(budget.IsSuccess, budget.Error?.Code);
        Assert.True(goal.IsSuccess, goal.Error?.Code);

        var progress = await handler.Handle(new AddGoalProgressCommand(Guid.NewGuid(), goal.Value.ResourceId,
            goal.Value.Version, 250m, "BRL"), default);
        Assert.True(progress.IsSuccess, progress.Error?.Code);

        var read = new FinanceReadStore(db, factory);
        var budgets = await read.BudgetsAsync(new ListBudgetsQuery(), default);
        var goals = await read.GoalsAsync(new ListGoalsQuery(), default);
        Assert.Contains(budgets.Value.Items, x => x.Id == budget.Value.ResourceId && x.Amount == 400m);
        Assert.Contains(goals.Value.Items, x => x.Id == goal.Value.ResourceId && x.CurrentAmount == 250m);

        var bob = new FinanceUser(DevelopmentSeed.Bob);
        var denied = await TransactionFactory(db, bob).BeginAsync(default);
        Assert.True(denied.IsSuccess);
        await using var scope = denied.Value;
        Assert.Empty(await db.Budgets.Where(x => x.Id == budget.Value.ResourceId).ToListAsync());
        Assert.Empty(await db.Goals.Where(x => x.Id == goal.Value.ResourceId).ToListAsync());
    }

    [Fact]
    public async Task Posted_expense_creates_one_budget_alert_per_threshold()
    {
        await using var db = fixture.CreateContext();
        var alice = new FinanceUser(DevelopmentSeed.Alice);
        var factory = new FinanceUnitFactory(db, TransactionFactory(db, alice), new TestTenant(DevelopmentSeed.TenantA), alice);
        var handler = new FinanceMutationHandlers(factory, alice, new TestTenant(DevelopmentSeed.TenantA));
        var now = DateTimeOffset.UtcNow;
        var account = await handler.Handle(new OpenAccountCommand(Guid.NewGuid(), "Alerta Orcamento", "BRL", 1000m), default);
        var category = await handler.Handle(new CreateCategoryCommand(Guid.NewGuid(), "Alimentacao alerta", "Expense"), default);
        var budget = await handler.Handle(new CreateBudgetCommand(Guid.NewGuid(), null, 400m, "BRL",
            new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero), new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero).AddMonths(1)), default);
        Assert.True(account.IsSuccess && category.IsSuccess && budget.IsSuccess);

        var first = await handler.Handle(new CreateTransactionCommand(Guid.NewGuid(), account.Value.ResourceId,
            category.Value.ResourceId, 320m, "BRL", "Expense", "Posted", now, "Alerta 80", account.Value.Version), default);
        Assert.True(first.IsSuccess, first.Error?.Code);
        var prefix = $"budget.{budget.Value.ResourceId:N}.";
        Assert.Single(await db.FinancialNotifications.Where(x => x.Type == prefix + "80").ToListAsync());

        var second = await handler.Handle(new CreateTransactionCommand(Guid.NewGuid(), account.Value.ResourceId,
            category.Value.ResourceId, 100m, "BRL", "Expense", "Posted", now, "Alerta 100", first.Value.SourceVersion!.Value), default);
        Assert.True(second.IsSuccess, second.Error?.Code);
        Assert.Single(await db.FinancialNotifications.Where(x => x.Type == prefix + "80").ToListAsync());
        Assert.Single(await db.FinancialNotifications.Where(x => x.Type == prefix + "100").ToListAsync());
    }

    [Fact]
    public async Task Migration_contains_planning_and_dashboard()
    {
        await using var db = fixture.CreateAdminContext();
        var applied = await db.Database.GetAppliedMigrationsAsync();
        Assert.Contains(applied, x => x.EndsWith("PlanningAndDashboard", StringComparison.Ordinal));
    }

    private static TenantTransactionFactory TransactionFactory(AppDbContext db, ICurrentUser user)
        => new(db, new TestTenant(DevelopmentSeed.TenantA), user, Options.Create(new DatabaseOptions()));

    private sealed record TestTenant(Guid? TenantId) : ITenantContext
    {
        public bool IsResolved => TenantId is not null;
    }
    private sealed class FinanceUser(string subject) : IUserContext
    {
        public bool IsAuthenticated => true;
        public string Subject => subject;
        public IReadOnlySet<string> Permissions { get; } = new HashSet<string>
        {
            RassAppControleFin.Security.Permissions.AccountsRead, RassAppControleFin.Security.Permissions.AccountsWrite,
            RassAppControleFin.Security.Permissions.TransfersWrite, RassAppControleFin.Security.Permissions.TransactionsRead,
            RassAppControleFin.Security.Permissions.TransactionsWrite, RassAppControleFin.Security.Permissions.CategoriesRead,
            RassAppControleFin.Security.Permissions.CategoriesWrite, RassAppControleFin.Security.Permissions.CreditCardsRead,
            RassAppControleFin.Security.Permissions.CreditCardsWrite
        };
    }
}
