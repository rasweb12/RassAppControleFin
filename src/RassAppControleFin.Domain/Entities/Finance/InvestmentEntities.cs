using RassAppControleFin.Domain.Investments;
using RassAppControleFin.Domain.ValueObjects;

namespace RassAppControleFin.Domain.Entities.Finance;

public sealed class PayYourselfFirstPlan : FinancialAggregate
{
    private PayYourselfFirstPlan(Guid id, Guid tenantId, string userId, DateTimeOffset createdAt) : base(id, tenantId, userId, createdAt) { }
    public decimal CurrentRate { get; private set; }
    public decimal? GoalRate { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public static PayYourselfFirstPlan Create(Guid id, Guid tenantId, string userId, decimal currentRate,
        decimal? goalRate, DateOnly effectiveFrom, DateTimeOffset now)
    {
        var strategy = PayYourselfFirstStrategy.Create(currentRate, goalRate, effectiveFrom);
        return new(id, tenantId, userId, now) { CurrentRate = strategy.CurrentRate, GoalRate = strategy.GoalRate, EffectiveFrom = strategy.EffectiveFrom };
    }
    public void Change(decimal currentRate, decimal? goalRate, DateOnly effectiveFrom)
    {
        var strategy = PayYourselfFirstStrategy.Create(currentRate, goalRate, effectiveFrom);
        CurrentRate = strategy.CurrentRate; GoalRate = strategy.GoalRate; EffectiveFrom = strategy.EffectiveFrom;
        Version = checked(Version + 1);
    }
}

public sealed class PayYourselfFirstPlanRevision : FinancialAggregate
{
    private PayYourselfFirstPlanRevision(Guid id, Guid tenantId, string userId, DateTimeOffset createdAt) : base(id, tenantId, userId, createdAt) { }
    public Guid PlanId { get; private set; }
    public decimal CurrentRate { get; private set; }
    public decimal? GoalRate { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public static PayYourselfFirstPlanRevision Record(Guid id, PayYourselfFirstPlan plan, DateTimeOffset now)
        => new(id, plan.TenantId, plan.UserId, now) { PlanId = plan.Id, CurrentRate = plan.CurrentRate, GoalRate = plan.GoalRate, EffectiveFrom = plan.EffectiveFrom };
}

public sealed class EmergencyFundPlan : FinancialAggregate
{
    private EmergencyFundPlan(Guid id, Guid tenantId, string userId, DateTimeOffset createdAt) : base(id, tenantId, userId, createdAt) { }
    public EmergencyFundTargetType TargetType { get; private set; }
    public decimal? TargetMultiple { get; private set; }
    public decimal TargetAmount { get; private set; }
    public decimal CurrentAmount { get; private set; }
    public decimal MonthlyContributionTarget { get; private set; }
    public bool LiquidityRequired { get; private set; }
    public string? Notes { get; private set; }
    public string Currency { get; private set; } = "";
    public static EmergencyFundPlan Create(Guid id, Guid tenantId, string userId, EmergencyFundTargetType targetType,
        decimal? targetMultiple, Money targetAmount, decimal currentAmount, decimal monthlyContributionTarget,
        bool liquidityRequired, string? notes, DateTimeOffset now)
    {
        if (!Enum.IsDefined(targetType) || currentAmount < 0 || monthlyContributionTarget < 0
            || (targetType == EmergencyFundTargetType.FixedAmount && targetMultiple is not null)
            || (targetType != EmergencyFundTargetType.FixedAmount && targetMultiple is not > 0)
            || notes?.Trim().Length > 1000) throw new ArgumentException("Invalid emergency fund plan.");
        return new(id, tenantId, userId, now) { TargetType = targetType, TargetMultiple = targetMultiple,
            TargetAmount = targetAmount.Amount, CurrentAmount = currentAmount, MonthlyContributionTarget = monthlyContributionTarget,
            LiquidityRequired = liquidityRequired, Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(), Currency = targetAmount.Currency };
    }
    public void Change(EmergencyFundTargetType targetType, decimal? targetMultiple, Money targetAmount,
        decimal currentAmount, decimal monthlyContributionTarget, bool liquidityRequired, string? notes)
    {
        var replacement = Create(Id, TenantId, UserId, targetType, targetMultiple, targetAmount, currentAmount,
            monthlyContributionTarget, liquidityRequired, notes, CreatedAt);
        TargetType = replacement.TargetType; TargetMultiple = replacement.TargetMultiple; TargetAmount = replacement.TargetAmount;
        CurrentAmount = replacement.CurrentAmount; MonthlyContributionTarget = replacement.MonthlyContributionTarget;
        LiquidityRequired = replacement.LiquidityRequired; Notes = replacement.Notes; Currency = replacement.Currency;
        Version = checked(Version + 1);
    }
}

public sealed class InvestmentAsset : FinancialAggregate
{
    private InvestmentAsset(Guid id, Guid tenantId, string userId, DateTimeOffset createdAt) : base(id, tenantId, userId, createdAt) { }
    public string Name { get; private set; } = "";
    public string? Ticker { get; private set; }
    public InvestmentAssetClass AssetClass { get; private set; }
    public string AssetSubClass { get; private set; } = "Other";
    public Guid? AccountId { get; private set; }
    public string? Issuer { get; private set; }
    public InvestmentIndexer Indexer { get; private set; }
    public decimal? Rate { get; private set; }
    public DateOnly? MaturityDate { get; private set; }
    public InvestmentLiquidityType LiquidityType { get; private set; }
    public bool FgcProtected { get; private set; }
    public bool TaxExempt { get; private set; }
    public bool Active { get; private set; } = true;
    public string? Notes { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public static InvestmentAsset Create(Guid id, Guid tenantId, string userId, string name, string? ticker,
        InvestmentAssetClass assetClass, string assetSubClass, Guid? accountId, string? issuer, InvestmentIndexer indexer,
        decimal? rate, DateOnly? maturityDate, InvestmentLiquidityType liquidityType, bool fgcProtected, bool taxExempt,
        string? notes, DateTimeOffset now)
    {
        if (!Enum.IsDefined(assetClass) || !Enum.IsDefined(indexer) || !Enum.IsDefined(liquidityType)
            || string.IsNullOrWhiteSpace(assetSubClass) || assetSubClass.Trim().Length > 80 || rate is < -1m or > 100m
            || ticker?.Trim().Length > 20 || issuer?.Trim().Length > 120 || notes?.Trim().Length > 2000)
            throw new ArgumentException("Invalid investment asset.");
        return new(id, tenantId, userId, now) { Name = NameValue(name), Ticker = Optional(ticker, 20), AssetClass = assetClass,
            AssetSubClass = assetSubClass.Trim(), AccountId = accountId, Issuer = Optional(issuer, 120), Indexer = indexer,
            Rate = rate, MaturityDate = maturityDate, LiquidityType = liquidityType, FgcProtected = fgcProtected,
            TaxExempt = taxExempt, Notes = Optional(notes, 2000), UpdatedAt = now };
    }
    public void Change(string name, string? ticker, InvestmentAssetClass assetClass, string assetSubClass, Guid? accountId,
        string? issuer, InvestmentIndexer indexer, decimal? rate, DateOnly? maturityDate,
        InvestmentLiquidityType liquidityType, bool fgcProtected, bool taxExempt, string? notes, bool active, DateTimeOffset now)
    {
        var replacement = Create(Id, TenantId, UserId, name, ticker, assetClass, assetSubClass, accountId, issuer, indexer,
            rate, maturityDate, liquidityType, fgcProtected, taxExempt, notes, now);
        Name = replacement.Name; Ticker = replacement.Ticker; AssetClass = replacement.AssetClass; AssetSubClass = replacement.AssetSubClass;
        AccountId = replacement.AccountId; Issuer = replacement.Issuer; Indexer = replacement.Indexer; Rate = replacement.Rate;
        MaturityDate = replacement.MaturityDate; LiquidityType = replacement.LiquidityType; FgcProtected = replacement.FgcProtected;
        TaxExempt = replacement.TaxExempt; Notes = replacement.Notes; Active = active; UpdatedAt = now;
        Version = checked(Version + 1);
    }
    private static string? Optional(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null
        : value.Trim().Length <= max ? value.Trim() : throw new ArgumentException("Value exceeds limit.");
}

public sealed class InvestmentTransaction : FinancialAggregate
{
    private InvestmentTransaction(Guid id, Guid tenantId, string userId, DateTimeOffset createdAt) : base(id, tenantId, userId, createdAt) { }
    public Guid InvestmentAssetId { get; private set; }
    public DateOnly Date { get; private set; }
    public InvestmentTransactionType Type { get; private set; }
    public decimal? Quantity { get; private set; }
    public decimal? UnitPrice { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal Fees { get; private set; }
    public decimal Taxes { get; private set; }
    public decimal NetAmount { get; private set; }
    public string Currency { get; private set; } = "";
    public string? Notes { get; private set; }
    public static InvestmentTransaction Create(Guid id, Guid tenantId, string userId, Guid assetId, DateOnly date,
        InvestmentTransactionType type, decimal? quantity, decimal? unitPrice, Money grossAmount, decimal fees,
        decimal taxes, string? notes, DateTimeOffset now)
    {
        if (assetId == Guid.Empty || !Enum.IsDefined(type) || quantity is <= 0 || unitPrice is <= 0
            || fees < 0 || taxes < 0 || fees + taxes > grossAmount.Amount || notes?.Trim().Length > 1000)
            throw new ArgumentException("Invalid investment transaction.");
        return new(id, tenantId, userId, now) { InvestmentAssetId = assetId, Date = date, Type = type, Quantity = quantity,
            UnitPrice = unitPrice, GrossAmount = grossAmount.Amount, Fees = fees, Taxes = taxes,
            NetAmount = grossAmount.Amount - fees - taxes, Currency = grossAmount.Currency,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim() };
    }
}

public sealed class TargetPortfolioAllocation : FinancialAggregate
{
    private TargetPortfolioAllocation(Guid id, Guid tenantId, string userId, DateTimeOffset createdAt) : base(id, tenantId, userId, createdAt) { }
    public Guid? InvestmentAssetId { get; private set; }
    public string Key { get; private set; } = "";
    public InvestmentAssetClass AssetClass { get; private set; }
    public decimal Weight { get; private set; }
    public static TargetPortfolioAllocation Create(Guid id, Guid tenantId, string userId, Guid? assetId, string key,
        InvestmentAssetClass assetClass, decimal weight, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Trim().Length > 120 || !Enum.IsDefined(assetClass)
            || weight is < 0m or > 100m)
            throw new ArgumentException("Invalid target portfolio allocation.");
        return new(id, tenantId, userId, now) { InvestmentAssetId = assetId, Key = key.Trim(), AssetClass = assetClass, Weight = weight };
    }
    public void Change(Guid? assetId, string key, InvestmentAssetClass assetClass, decimal weight)
    {
        var replacement = Create(Id, TenantId, UserId, assetId, key, assetClass, weight, CreatedAt);
        InvestmentAssetId = replacement.InvestmentAssetId; Key = replacement.Key; AssetClass = replacement.AssetClass;
        Weight = replacement.Weight; Version = checked(Version + 1);
    }
}
