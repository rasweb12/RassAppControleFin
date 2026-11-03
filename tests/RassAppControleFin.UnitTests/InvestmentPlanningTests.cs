using RassAppControleFin.Domain.Investments;
using Xunit;

namespace RassAppControleFin.UnitTests;

public sealed class InvestmentPlanningTests
{
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
}
