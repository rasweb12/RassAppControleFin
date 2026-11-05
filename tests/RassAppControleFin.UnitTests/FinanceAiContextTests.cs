using RassAppControleFin.Application.AI;
using RassAppControleFin.Application.Abstractions.Persistence;
using RassAppControleFin.Application.Features.Finance;
using RassAppControleFin.MultiTenancy;
using RassAppControleFin.Security;
using RassAppControleFin.SharedKernel.Results;
using Xunit;

namespace RassAppControleFin.UnitTests;

public sealed class FinanceAiContextTests
{
    [Fact]
    public async Task Comparison_question_requests_only_two_realized_months_and_preserves_dashboard_totals()
    {
        var read = new SnapshotStore();
        var service = new FinanceContextService(read, new Tenant(), new User());

        var result = await service.GetAsync("Compare minhas financas dos ultimos dois meses.", "2026-11", default);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(new FinanceAiContextQuery(new DateOnly(2026, 11, 1), 2, 0), read.LastQuery);
        Assert.Equal(1000m, result.Value.Aggregates["realized.month.2026-10.BRL.income"]);
        Assert.Equal(600m, result.Value.Aggregates["realized.month.2026-10.BRL.expense"]);
        Assert.Equal(250m, result.Value.Aggregates["realized.category.2026-10.BRL.Alimentacao.expense"]);
        Assert.Equal(1200m, result.Value.Aggregates["account.BRL.Conta_corrente.balance"]);
    }

    [Fact]
    public async Task Forecast_question_separates_pending_rows_from_realized_values()
    {
        var read = new SnapshotStore();
        var service = new FinanceContextService(read, new Tenant(), new User());

        var result = await service.GetAsync("Quais despesas estao previstas para os proximos seis meses?", "2026-11", default);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(new FinanceAiContextQuery(new DateOnly(2026, 11, 1), 1, 6), read.LastQuery);
        Assert.Equal(150m, result.Value.Aggregates["forecast.month.2026-12.BRL.expense"]);
        Assert.Equal(150m, result.Value.Aggregates["forecast.category.2026-12.BRL.Aluguel.expense"]);
        Assert.DoesNotContain(result.Value.Aggregates.Keys, key => key.StartsWith("realized.month.2026-12", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Spending_analysis_requests_the_requested_history_and_category_increase_has_two_months_to_compare()
    {
        var read = new SnapshotStore();
        var service = new FinanceContextService(read, new Tenant(), new User());

        var savings = await service.GetAsync("Onde posso economizar considerando os ultimos seis meses?", "2026-11", default);
        Assert.True(savings.IsSuccess, savings.Error?.Code);
        Assert.Equal(new FinanceAiContextQuery(new DateOnly(2026, 11, 1), 6, 0), read.LastQuery);

        var increase = await service.GetAsync("Quais categorias tiveram maior aumento?", "2026-11", default);
        Assert.True(increase.IsSuccess, increase.Error?.Code);
        Assert.Equal(new FinanceAiContextQuery(new DateOnly(2026, 11, 1), 2, 0), read.LastQuery);
    }

    private sealed class SnapshotStore : IFinanceAiReadStore
    {
        public FinanceAiContextQuery? LastQuery { get; private set; }
        public Task<Result<FinanceAiSnapshot>> AiSnapshotAsync(FinanceAiContextQuery query, CancellationToken ct)
        {
            LastQuery = query;
            return Task.FromResult(Result<FinanceAiSnapshot>.Success(new FinanceAiSnapshot(
                [new(new DateOnly(2026, 10, 1), "BRL", 1000m, 600m)],
                [new(new DateOnly(2026, 10, 1), "BRL", "Alimentacao", 250m)],
                [new("BRL", "Conta corrente", 1200m)],
                [new(new DateOnly(2026, 12, 1), "BRL", "Expense", "Aluguel", "Conta corrente", 150m)])));
        }
    }

    private sealed class Tenant : ITenantContext
    {
        public Guid? TenantId { get; } = Guid.NewGuid();
        public bool IsResolved => true;
    }
    private sealed class User : IUserContext
    {
        public bool IsAuthenticated => true;
        public string Subject => "ai-context-test";
        public IReadOnlySet<string> Permissions { get; } = new HashSet<string>();
    }
}
