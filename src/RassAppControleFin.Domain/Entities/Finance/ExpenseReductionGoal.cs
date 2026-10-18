using RassAppControleFin.Domain.ValueObjects;

namespace RassAppControleFin.Domain.Entities.Finance;

public sealed class ExpenseReductionGoal : FinancialAggregate
{
    private ExpenseReductionGoal(Guid id, Guid tenantId, string userId, DateTimeOffset createdAt)
        : base(id, tenantId, userId, createdAt) { }

    public Guid? CategoryId { get; private set; }
    public decimal ReferenceAmount { get; private set; }
    public decimal TargetAmount { get; private set; }
    public string Currency { get; private set; } = "";
    public DateTimeOffset PeriodStart { get; private set; }
    public DateTimeOffset PeriodEnd { get; private set; }
    public bool IsArchived { get; private set; }

    public static ExpenseReductionGoal Create(Guid id, Guid tenantId, string userId, Guid? categoryId,
        Money referenceAmount, Money targetAmount, DateTimeOffset periodStart, DateTimeOffset periodEnd,
        DateTimeOffset createdAt)
    {
        if (referenceAmount.Currency != targetAmount.Currency || targetAmount.Amount >= referenceAmount.Amount)
            throw new ArgumentException("Reduction target must be lower than the reference amount.");
        if (periodStart.Offset != TimeSpan.Zero || periodEnd.Offset != TimeSpan.Zero || periodStart >= periodEnd)
            throw new ArgumentException("Invalid period.");
        return new(id, tenantId, userId, createdAt)
        {
            CategoryId = categoryId,
            ReferenceAmount = referenceAmount.Amount,
            TargetAmount = targetAmount.Amount,
            Currency = referenceAmount.Currency,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd
        };
    }

    public void Archive()
    {
        if (!IsArchived) { IsArchived = true; Version = checked(Version + 1); }
    }
}
