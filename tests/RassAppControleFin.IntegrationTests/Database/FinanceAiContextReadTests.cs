using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RassAppControleFin.Application.Abstractions.Persistence;
using RassAppControleFin.Application.Features.Finance;
using RassAppControleFin.Domain.Entities.Finance;
using RassAppControleFin.Infrastructure.Configuration;
using RassAppControleFin.Infrastructure.Persistence;
using RassAppControleFin.Infrastructure.Persistence.Seed;
using RassAppControleFin.MultiTenancy;
using RassAppControleFin.Security;
using Xunit;

namespace RassAppControleFin.IntegrationTests.Database;

/// <summary>
/// Exercises the read projection with synthetic, tenant-scoped records in the dedicated test database.
/// It is intentionally part of the PostgreSQL collection: it must never target a published environment.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class FinanceAiContextReadTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Snapshot_separates_posted_history_from_pending_forecasts_and_hides_other_users()
    {
        await using var db = fixture.CreateContext();
        var user = new FinanceUser(DevelopmentSeed.Alice);
        var tenant = new TestTenant(DevelopmentSeed.TenantA);
        var factory = new FinanceUnitFactory(db, new TenantTransactionFactory(db, tenant, user,
            Options.Create(new DatabaseOptions())), tenant, user);
        var handler = new FinanceMutationHandlers(factory, user, tenant);
        var marker = Guid.NewGuid().ToString("N")[..8];

        var account = await handler.Handle(new OpenAccountCommand(Guid.NewGuid(), $"IA Conta {marker}", "BRL", 2000m), default);
        var income = await handler.Handle(new CreateCategoryCommand(Guid.NewGuid(), $"IA Receita {marker}", "Income"), default);
        var expense = await handler.Handle(new CreateCategoryCommand(Guid.NewGuid(), $"IA Despesa {marker}", "Expense"), default);
        Assert.True(account.IsSuccess && income.IsSuccess && expense.IsSuccess);

        var posted = await handler.Handle(new CreateTransactionCommand(Guid.NewGuid(), account.Value.ResourceId,
            income.Value.ResourceId, 1000m, "BRL", "Income", "Posted",
            new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.Zero), "synthetic", account.Value.Version), default);
        Assert.True(posted.IsSuccess, posted.Error?.Code);
        var spent = await handler.Handle(new CreateTransactionCommand(Guid.NewGuid(), account.Value.ResourceId,
            expense.Value.ResourceId, 320m, "BRL", "Expense", "Posted",
            new DateTimeOffset(2026, 10, 11, 0, 0, 0, TimeSpan.Zero), "synthetic", posted.Value.SourceVersion!.Value), default);
        Assert.True(spent.IsSuccess, spent.Error?.Code);
        var pending = await handler.Handle(new CreateTransactionCommand(Guid.NewGuid(), account.Value.ResourceId,
            expense.Value.ResourceId, 150m, "BRL", "Expense", "Pending",
            new DateTimeOffset(2026, 12, 10, 0, 0, 0, TimeSpan.Zero), "synthetic", spent.Value.SourceVersion!.Value), default);
        Assert.True(pending.IsSuccess, pending.Error?.Code);

        var read = new FinanceReadStore(db, factory, NullLogger<FinanceReadStore>.Instance);
        var snapshot = await read.AiSnapshotAsync(new FinanceAiContextQuery(new DateOnly(2026, 11, 1), 2, 2), default);
        var dashboard = await read.DashboardAsync(new DashboardQuery(
            new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero)), default);

        Assert.True(snapshot.IsSuccess, snapshot.Error?.Code);
        Assert.True(dashboard.IsSuccess, dashboard.Error?.Code);
        Assert.Contains(snapshot.Value.RealizedMonths, row => row.Month == new DateOnly(2026, 10, 1)
            && row.Currency == "BRL" && row.Income == 1000m && row.Expense == 320m);
        Assert.Contains(snapshot.Value.RealizedCategories, row => row.Category == $"IA Despesa {marker}" && row.Expense == 320m);
        Assert.Contains(snapshot.Value.Forecasts, row => row.Month == new DateOnly(2026, 12, 1)
            && row.Flow == "Expense" && row.Amount == 150m);
        Assert.DoesNotContain(snapshot.Value.Forecasts, row => row.Amount == 320m);
        var monthly = snapshot.Value.RealizedMonths.Single(row => row.Month == new DateOnly(2026, 10, 1) && row.Currency == "BRL");
        var dashboardMetric = dashboard.Value.Metrics.Single(row => row.Currency == "BRL");
        Assert.Equal(dashboardMetric.Income, monthly.Income);
        Assert.Equal(dashboardMetric.Expense, monthly.Expense);
    }

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
            RassAppControleFin.Security.Permissions.CategoriesRead, RassAppControleFin.Security.Permissions.CategoriesWrite,
            RassAppControleFin.Security.Permissions.TransactionsRead, RassAppControleFin.Security.Permissions.TransactionsWrite
        };
    }
}
