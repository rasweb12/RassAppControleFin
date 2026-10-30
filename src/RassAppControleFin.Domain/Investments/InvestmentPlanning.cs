namespace RassAppControleFin.Domain.Investments;

public enum InvestmentAssetClass { FixedIncome, RealEstateFund, Equity, Other }
public enum InvestmentIndexer { Cdi, Selic, Ipca, Prefixed, Variable, Other }
public enum InvestmentLiquidityType { Immediate, Daily, D1, AtMaturity, SecondaryMarket, Other }
public enum InvestmentTransactionType { Contribution, Redemption, Dividend, Interest, Amortization, CapitalGain, CapitalLoss, Fee, Tax, Adjustment }
public enum EmergencyFundTargetType { GrossSalaryMultiple, NetSalaryMultiple, FixedAmount }

public sealed record PayYourselfFirstStrategy(decimal CurrentRate, decimal? GoalRate, DateOnly EffectiveFrom)
{
    public decimal PlannedAmount(decimal receivedIncome) => InvestmentMath.Money(receivedIncome * CurrentRate);
    public decimal AvailableForExpenses(decimal receivedIncome) => InvestmentMath.Money(receivedIncome - PlannedAmount(receivedIncome));
    public decimal? GoalGapPercentagePoints => GoalRate is null ? null : (GoalRate.Value - CurrentRate) * 100m;

    public static PayYourselfFirstStrategy Create(decimal currentRate, decimal? goalRate, DateOnly effectiveFrom)
    {
        InvestmentMath.Rate(currentRate, nameof(currentRate));
        if (goalRate is { } rate) InvestmentMath.Rate(rate, nameof(goalRate));
        return new PayYourselfFirstStrategy(currentRate, goalRate, effectiveFrom);
    }
}

public sealed record TargetAllocation(string Key, InvestmentAssetClass AssetClass, decimal Weight)
{
    public static void Validate(IReadOnlyCollection<TargetAllocation> allocations)
    {
        if (allocations.Count == 0) throw new ArgumentException("At least one allocation is required.", nameof(allocations));
        if (allocations.Any(x => string.IsNullOrWhiteSpace(x.Key) || x.Key.Trim().Length > 120 || x.Weight < 0))
            throw new ArgumentException("Invalid allocation.", nameof(allocations));
        if (InvestmentMath.Money(allocations.Sum(x => x.Weight)) != 100m)
            throw new ArgumentException("Target allocations must total 100%.", nameof(allocations));
    }
}

public sealed record EmergencyFundSnapshot(decimal CurrentAmount, decimal TargetAmount, decimal MonthlyContribution)
{
    public decimal ProgressRate => TargetAmount == 0 ? 0 : CurrentAmount / TargetAmount;
    public decimal RemainingAmount => Math.Max(0, InvestmentMath.Money(TargetAmount - CurrentAmount));
    public int? MonthsRemaining => MonthlyContribution <= 0 || RemainingAmount == 0
        ? null : (int)Math.Ceiling(RemainingAmount / MonthlyContribution);
}

public sealed record ProjectionScenario(string Name, decimal AnnualReturnRate, decimal? AnnualInflationRate = null)
{
    public static ProjectionScenario Create(string name, decimal annualReturnRate, decimal? annualInflationRate = null)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 80) throw new ArgumentException("Invalid scenario name.", nameof(name));
        if (annualReturnRate < -0.99m || annualReturnRate > 10m) throw new ArgumentOutOfRangeException(nameof(annualReturnRate));
        if (annualInflationRate is { } inflation && (inflation < 0 || inflation > 10m)) throw new ArgumentOutOfRangeException(nameof(annualInflationRate));
        return new ProjectionScenario(name.Trim(), annualReturnRate, annualInflationRate);
    }
}

public sealed record ProjectionInput(decimal InitialNetWorth, decimal MonthlyContribution, int Months,
    ProjectionScenario Scenario, decimal AnnualContributionGrowthRate = 0m,
    IReadOnlyDictionary<int, decimal>? ExtraordinaryContributions = null)
{
    public static ProjectionInput Create(decimal initialNetWorth, decimal monthlyContribution, int months,
        ProjectionScenario scenario, decimal annualContributionGrowthRate = 0m,
        IReadOnlyDictionary<int, decimal>? extraordinaryContributions = null)
    {
        if (initialNetWorth < 0 || monthlyContribution < 0 || months is < 1 or > 1200)
            throw new ArgumentOutOfRangeException(nameof(months));
        if (annualContributionGrowthRate is < -0.99m or > 10m) throw new ArgumentOutOfRangeException(nameof(annualContributionGrowthRate));
        if (extraordinaryContributions?.Any(x => x.Key is < 1 or > 1200 || x.Value < 0) == true)
            throw new ArgumentException("Invalid extraordinary contribution.", nameof(extraordinaryContributions));
        return new ProjectionInput(initialNetWorth, monthlyContribution, months, scenario,
            annualContributionGrowthRate, extraordinaryContributions);
    }
}

public sealed record ProjectionResult(decimal ProjectedNetWorth, decimal TotalContributed, decimal TotalReturn,
    decimal? InflationAdjustedNetWorth, IReadOnlyList<decimal> MonthEndBalances);

public static class InvestmentProjectionEngine
{
    public static ProjectionResult Project(ProjectionInput input)
    {
        var monthlyRate = MonthlyRate(input.Scenario.AnnualReturnRate);
        var inflationMonthlyRate = input.Scenario.AnnualInflationRate is { } inflation ? MonthlyRate(inflation) : (decimal?)null;
        var balance = input.InitialNetWorth;
        var totalContributed = input.InitialNetWorth;
        var balances = new List<decimal>(input.Months);
        for (var month = 1; month <= input.Months; month++)
        {
            var contribution = input.MonthlyContribution * GrowthFactor(input.AnnualContributionGrowthRate, month);
            contribution += input.ExtraordinaryContributions?.GetValueOrDefault(month) ?? 0m;
            contribution = InvestmentMath.Money(contribution);
            balance = InvestmentMath.Money((balance * (1 + monthlyRate)) + contribution);
            totalContributed = InvestmentMath.Money(totalContributed + contribution);
            balances.Add(balance);
        }
        decimal? adjusted = inflationMonthlyRate is null ? null
            : InvestmentMath.Money(balance / Pow(1 + inflationMonthlyRate.Value, input.Months));
        return new ProjectionResult(balance, totalContributed, InvestmentMath.Money(balance - totalContributed), adjusted, balances);
    }

    public static IReadOnlyDictionary<string, decimal> Allocate(decimal contribution, IReadOnlyCollection<TargetAllocation> allocations)
    {
        if (contribution < 0) throw new ArgumentOutOfRangeException(nameof(contribution));
        TargetAllocation.Validate(allocations);
        var ordered = allocations.OrderBy(x => x.Key, StringComparer.Ordinal).ToArray();
        var result = ordered.ToDictionary(x => x.Key, x => InvestmentMath.Money(contribution * x.Weight / 100m));
        var residue = InvestmentMath.Money(contribution - result.Values.Sum());
        if (residue != 0) result[ordered[0].Key] += residue;
        return result;
    }

    // Rates are calculated with decimal arithmetic so money calculations never pass through double.
    private static decimal MonthlyRate(decimal annualRate) => NthRoot(1m + annualRate, 12) - 1m;
    private static decimal GrowthFactor(decimal annualGrowth, int month) => Pow(1 + annualGrowth, (month - 1) / 12);
    private static decimal Pow(decimal value, int exponent)
    {
        var result = 1m;
        for (var i = 0; i < exponent; i++) result *= value;
        return result;
    }

    private static decimal NthRoot(decimal value, int degree)
    {
        var low = 0m;
        var high = Math.Max(1m, value);
        for (var i = 0; i < 64; i++)
        {
            var candidate = (low + high) / 2m;
            if (Pow(candidate, degree) > value) high = candidate;
            else low = candidate;
        }
        return (low + high) / 2m;
    }
}

public static class InvestmentMath
{
    public static decimal Money(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    public static void Rate(decimal value, string parameter)
    {
        if (value is < 0 or > 1) throw new ArgumentOutOfRangeException(parameter, "Rate must be between zero and one.");
    }
}
