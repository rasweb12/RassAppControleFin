using RassAppControleFin.Application.Abstractions.Persistence;
using RassAppControleFin.Application.Features.Finance;
using RassAppControleFin.Domain.Entities.Finance;
using RassAppControleFin.Domain.Investments;
using RassAppControleFin.MultiTenancy;
using RassAppControleFin.Security;
using RassAppControleFin.SharedKernel.Results;
using Xunit;

namespace RassAppControleFin.UnitTests;

public sealed class InvestmentPlanningTests
{
    [Fact]
    public async Task Goal_edit_rejects_stale_version_without_overwriting_progress_or_committing()
    {
        var fixture = PlanFixture.Create();
        var tenantId = Guid.NewGuid();
        var goal = Goal.Create(Guid.NewGuid(), tenantId, "user-1", "Sintética",
            RassAppControleFin.Domain.ValueObjects.Money.Create(200m, "BRL"), null, DateTimeOffset.UtcNow);
        goal.AddProgress(RassAppControleFin.Domain.ValueObjects.Money.Create(41m, "BRL"));
        fixture.Unit.Goal = goal;
        var handler = new FinanceMutationHandlers(new TestFinanceUnitFactory(fixture.Unit),
            new TestUser("user-1"), new TestTenant(tenantId));
        var originalVersion = goal.Version;
        var first = await handler.Handle(new UpdateGoalCommand(Guid.NewGuid(), goal.Id,
            originalVersion, "Primeira edição", 250m, "BRL", null), CancellationToken.None);
        var stale = await handler.Handle(new UpdateGoalCommand(Guid.NewGuid(), goal.Id,
            originalVersion, "Edição desatualizada", 300m, "BRL", null), CancellationToken.None);
        Assert.True(first.IsSuccess);
        Assert.Equal("VERSION_CONFLICT", stale.Error!.Code);
        Assert.Equal(1, fixture.Unit.CommitCount);
        Assert.Equal(originalVersion + 1, goal.Version);
        Assert.Equal("Primeira edição", goal.Name);
        Assert.Equal(250m, goal.TargetAmount);
        Assert.Equal(41m, goal.CurrentAmount);
    }
    [Fact]
    public void Same_day_plan_correction_updates_the_existing_monthly_revision()
    {
        var now = new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);
        var tenantId = Guid.NewGuid();
        var plan = PayYourselfFirstPlan.Create(Guid.NewGuid(), tenantId, "user-1", .20m, .30m,
            new DateOnly(2026, 11, 4), now);
        var revision = PayYourselfFirstPlanRevision.Record(Guid.NewGuid(), plan, now);

        plan.Change(.30m, null, new DateOnly(2026, 11, 4));
        revision.Revise(plan);

        Assert.Equal(.30m, revision.CurrentRate);
        Assert.Null(revision.GoalRate);
        Assert.Equal(new DateOnly(2026, 11, 4), revision.EffectiveFrom);
        Assert.Equal(2, revision.Version);
    }

    [Fact]
    public async Task Configure_plan_rejects_a_stale_version_without_changing_the_current_revision()
    {
        var fixture = PlanFixture.Create();
        var result = await fixture.Handler.Handle(fixture.Command with { Version = 0, CurrentRate = .30m }, default);

        Assert.False(result.IsSuccess);
        Assert.Equal("VERSION_CONFLICT", result.Error!.Code);
        Assert.Equal(1, fixture.Plan.Version);
        Assert.Equal(.20m, fixture.Plan.CurrentRate);
        Assert.Single(fixture.Unit.Revisions);
        Assert.Equal(1, fixture.Unit.Revisions[0].Version);
    }

    [Fact]
    public async Task Configure_plan_updates_the_same_day_revision_and_advances_both_versions()
    {
        var fixture = PlanFixture.Create();
        var result = await fixture.Handler.Handle(fixture.Command with { CurrentRate = .30m, GoalRate = null }, default);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(2, result.Value.Version);
        Assert.Equal(2, fixture.Plan.Version);
        Assert.Single(fixture.Unit.Revisions);
        Assert.Equal(2, fixture.Unit.Revisions[0].Version);
        Assert.Equal(.30m, fixture.Unit.Revisions[0].CurrentRate);
        Assert.Null(fixture.Unit.Revisions[0].GoalRate);
    }

    [Fact]
    public async Task Configure_plan_creates_a_new_revision_for_a_different_effective_date()
    {
        var fixture = PlanFixture.Create();
        var nextDay = fixture.Command.EffectiveFrom.AddDays(1);
        var result = await fixture.Handler.Handle(fixture.Command with { CurrentRate = .25m, EffectiveFrom = nextDay }, default);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(2, fixture.Plan.Version);
        Assert.Equal(2, fixture.Unit.Revisions.Count);
        Assert.Contains(fixture.Unit.Revisions, x => x.EffectiveFrom == fixture.Command.EffectiveFrom && x.Version == 1);
        Assert.Contains(fixture.Unit.Revisions, x => x.EffectiveFrom == nextDay && x.Version == 1 && x.CurrentRate == .25m);
    }

    [Fact]
    public void First_you_pay_uses_received_income_and_rounds_away_from_zero()
    {
        var plan = PayYourselfFirstStrategy.Create(0.20m, 0.30m, new DateOnly(2026, 10, 1));

        Assert.Equal(1987.89m, plan.PlannedAmount(9939.43m));
        Assert.Equal(7951.54m, plan.AvailableForExpenses(9939.43m));
        Assert.Equal(10m, plan.GoalGapPercentagePoints);
    }

    [Fact]
    public void Target_allocation_must_total_one_hundred_percent()
    {
        var allocations = new[]
        {
            new TargetAllocation("Fixed income", InvestmentAssetClass.FixedIncome, 70m),
            new TargetAllocation("FIIs", InvestmentAssetClass.RealEstateFund, 20m),
            new TargetAllocation("Equity", InvestmentAssetClass.Equity, 9m),
        };

        Assert.Throws<ArgumentException>(() => TargetAllocation.Validate(allocations));
    }

    [Fact]
    public void Allocation_preserves_cents_without_losing_money()
    {
        var allocations = new[]
        {
            new TargetAllocation("Fixed income", InvestmentAssetClass.FixedIncome, 70m),
            new TargetAllocation("FIIs", InvestmentAssetClass.RealEstateFund, 20m),
            new TargetAllocation("Equity", InvestmentAssetClass.Equity, 10m),
        };

        var result = InvestmentProjectionEngine.Allocate(1987.89m, allocations);

        Assert.Equal(1987.89m, result.Values.Sum());
        Assert.Equal(1391.52m, result["Fixed income"]);
        Assert.Equal(397.58m, result["FIIs"]);
        Assert.Equal(198.79m, result["Equity"]);
    }

    [Fact]
    public void Monthly_suggestion_prefers_received_income_and_warns_when_obligations_are_not_covered()
    {
        var strategy = PayYourselfFirstStrategy.Create(.20m, .30m, new DateOnly(2026, 11, 1));
        var allocations = new[]
        {
            new TargetAllocation("Emergency and fixed income", InvestmentAssetClass.FixedIncome, 70m),
            new TargetAllocation("FIIs", InvestmentAssetClass.RealEstateFund, 20m),
            new TargetAllocation("Equity", InvestmentAssetClass.Equity, 10m),
        };

        var projection = PayYourselfFirstMonthlyProjection.Create(strategy, 5000m, 4500m, 3800m, allocations);

        Assert.Equal(4500m, projection.IncomeBasis);
        Assert.Equal(900m, projection.SuggestedReserve);
        Assert.Equal(-200m, projection.ProjectedBalanceAfterObligations);
        Assert.False(projection.IsAffordable);
        Assert.Equal(900m, projection.Distribution.Values.Sum());
        Assert.Equal(630m, projection.Distribution["Emergency and fixed income"]);
    }

    [Fact]
    public void Monthly_suggestion_uses_expected_income_only_when_no_income_was_received()
    {
        var strategy = PayYourselfFirstStrategy.Create(.20m, null, new DateOnly(2026, 11, 1));
        var allocations = new[] { new TargetAllocation("Reserve", InvestmentAssetClass.FixedIncome, 100m) };

        var projection = PayYourselfFirstMonthlyProjection.Create(strategy, 2000m, 0m, 1200m, allocations);

        Assert.Equal(2000m, projection.IncomeBasis);
        Assert.Equal(400m, projection.SuggestedReserve);
        Assert.Equal(400m, projection.ProjectedBalanceAfterObligations);
        Assert.True(projection.IsAffordable);
    }

    [Fact]
    public void Projection_without_return_equals_initial_plus_contributions()
    {
        var result = InvestmentProjectionEngine.Project(ProjectionInput.Create(1000m, 100m, 12,
            ProjectionScenario.Create("Base", 0m)));

        Assert.Equal(2200m, result.ProjectedNetWorth);
        Assert.Equal(2200m, result.TotalContributed);
        Assert.Equal(0m, result.TotalReturn);
    }

    [Fact]
    public void Projection_applies_extraordinary_contribution_and_compound_return()
    {
        var result = InvestmentProjectionEngine.Project(ProjectionInput.Create(0m, 100m, 12,
            ProjectionScenario.Create("Base", 0.12m), extraordinaryContributions: new Dictionary<int, decimal> { [6] = 1000m }));

        Assert.Equal(2200m, result.TotalContributed);
        Assert.True(result.ProjectedNetWorth > result.TotalContributed);
        Assert.True(result.TotalReturn > 0m);
    }

    [Fact]
    public void Emergency_fund_reports_remaining_amount_and_months()
    {
        var fund = new EmergencyFundSnapshot(4250m, 32500m, 1000m);

        Assert.Equal(28250m, fund.RemainingAmount);
        Assert.Equal(29, fund.MonthsRemaining);
        Assert.Equal(4250m / 32500m, fund.ProgressRate);
    }

    private sealed class PlanFixture
    {
        private PlanFixture(PayYourselfFirstPlan plan, TestFinanceUnit unit,
            InvestmentMutationHandlers handler, ConfigurePayYourselfFirstPlanCommand command)
        {
            Plan = plan;
            Unit = unit;
            Handler = handler;
            Command = command;
        }

        public PayYourselfFirstPlan Plan { get; }
        public TestFinanceUnit Unit { get; }
        public InvestmentMutationHandlers Handler { get; }
        public ConfigurePayYourselfFirstPlanCommand Command { get; }

        public static PlanFixture Create()
        {
            var now = new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);
            var tenantId = Guid.NewGuid();
            var effectiveFrom = new DateOnly(2026, 11, 4);
            var plan = PayYourselfFirstPlan.Create(Guid.NewGuid(), tenantId, "user-1", .20m, .30m, effectiveFrom, now);
            var unit = new TestFinanceUnit(plan, PayYourselfFirstPlanRevision.Record(Guid.NewGuid(), plan, now));
            var user = new TestUser("user-1");
            var handler = new InvestmentMutationHandlers(new TestFinanceUnitFactory(unit), user, new TestTenant(tenantId));
            var command = new ConfigurePayYourselfFirstPlanCommand(Guid.NewGuid(), plan.Version, .20m, .30m, effectiveFrom);
            return new(plan, unit, handler, command);
        }
    }

    private sealed record TestTenant(Guid? TenantId) : ITenantContext
    {
        public bool IsResolved => TenantId is not null;
    }

    private sealed class TestUser(string subject) : IUserContext
    {
        public bool IsAuthenticated => true;
        public string Subject => subject;
        public IReadOnlySet<string> Permissions { get; } = new HashSet<string>
        {
            RassAppControleFin.Security.Permissions.TransactionsWrite
        };
    }

    private sealed class TestFinanceUnitFactory(IFinanceUnit unit) : IFinanceUnitFactory
    {
        public Task<Result<IFinanceUnit>> BeginAsync(CancellationToken ct) => Task.FromResult(Result<IFinanceUnit>.Success(unit));
    }

    private sealed class TestFinanceUnit(PayYourselfFirstPlan plan, PayYourselfFirstPlanRevision revision) : IFinanceUnit
    {
        public Goal? Goal { get; set; }
        public int CommitCount { get; private set; }
        public List<PayYourselfFirstPlanRevision> Revisions { get; } = [revision];

        public Task<Result<ReceiptLookup>> ClaimAsync(Guid operationId, string payloadHash, CancellationToken ct)
            => Task.FromResult(Result<ReceiptLookup>.Success(new ReceiptLookup(false, null)));
        public Task<PayYourselfFirstPlan?> PayYourselfFirstPlanAsync(CancellationToken ct) => Task.FromResult<PayYourselfFirstPlan?>(plan);
        public Task<PayYourselfFirstPlanRevision?> PayYourselfFirstPlanRevisionAsync(Guid planId, DateOnly effectiveFrom, CancellationToken ct)
            => Task.FromResult<PayYourselfFirstPlanRevision?>(Revisions.SingleOrDefault(x => x.PlanId == planId && x.EffectiveFrom == effectiveFrom));
        public void Add(FinancialAggregate resource)
        {
            if (resource is PayYourselfFirstPlanRevision added) Revisions.Add(added);
        }
        public Task<Result> CommitAsync(Guid operationId, string payloadHash, MutationReceipt receipt, string action, CancellationToken ct)
        {
            CommitCount++;
            return Task.FromResult(Result.Success());
        }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task<Account?> AccountAsync(Guid id, CancellationToken ct) => Task.FromResult<Account?>(null);
        public Task<Category?> CategoryAsync(Guid id, CancellationToken ct) => Task.FromResult<Category?>(null);
        public Task<CreditCard?> CardAsync(Guid id, CancellationToken ct) => Task.FromResult<CreditCard?>(null);
        public Task<CreditCardInvoice?> CreditCardInvoiceAsync(Guid id, CancellationToken ct) => Task.FromResult<CreditCardInvoice?>(null);
        public Task<CreditCardInvoice?> CreditCardInvoiceAsync(Guid cardId, DateOnly competence, CancellationToken ct) => Task.FromResult<CreditCardInvoice?>(null);
        public Task<FinancialTransaction?> TransactionAsync(Guid id, CancellationToken ct) => Task.FromResult<FinancialTransaction?>(null);
        public Task<Budget?> BudgetAsync(Guid id, CancellationToken ct) => Task.FromResult<Budget?>(null);
        public Task<Goal?> GoalAsync(Guid id, CancellationToken ct) => Task.FromResult(Goal?.Id == id ? Goal : null);
        public Task<ExpenseReductionGoal?> ExpenseReductionGoalAsync(Guid id, CancellationToken ct) => Task.FromResult<ExpenseReductionGoal?>(null);
        public Task<FinancialNotification?> NotificationAsync(Guid id, CancellationToken ct) => Task.FromResult<FinancialNotification?>(null);
        public Task<EmergencyFundPlan?> EmergencyFundPlanAsync(CancellationToken ct) => Task.FromResult<EmergencyFundPlan?>(null);
        public Task<InvestmentAsset?> InvestmentAssetAsync(Guid id, CancellationToken ct) => Task.FromResult<InvestmentAsset?>(null);
        public Task<IReadOnlyList<TargetPortfolioAllocation>> TargetPortfolioAllocationsAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<TargetPortfolioAllocation>>(Array.Empty<TargetPortfolioAllocation>());
        public Task<IReadOnlyList<BudgetAlert>> BudgetAlertsAsync(Guid categoryId, string currency, DateTimeOffset occurredAt, decimal pendingExpense, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<BudgetAlert>>(Array.Empty<BudgetAlert>());
        public Task<bool> NotificationExistsAsync(string type, CancellationToken ct) => Task.FromResult(false);
        public void Ledger(Guid accountId, decimal delta, string kind, Guid resourceId, Guid? transferId = null) { }
        public void CardMovement(Guid cardId, Guid? accountId, Guid? categoryId, decimal amount, string currency, string kind,
            DateTimeOffset occurredAt, string description, Guid id, Guid? invoiceId = null, Guid? purchaseId = null) { }
    }
}
