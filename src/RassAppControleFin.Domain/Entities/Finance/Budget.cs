using RassAppControleFin.Domain.ValueObjects;

namespace RassAppControleFin.Domain.Entities.Finance;

public sealed class Budget : FinancialAggregate
{
    private Budget(Guid id, Guid tenantId, string userId, DateTimeOffset createdAt) : base(id, tenantId, userId, createdAt) { }
    public Guid? CategoryId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "";
    public DateTimeOffset PeriodStart { get; private set; }
    public DateTimeOffset PeriodEnd { get; private set; }
    public bool IsArchived { get; private set; }
    public static Budget Create(Guid id, Guid tenantId, string userId, Guid? categoryId, Money amount,
        DateTimeOffset start, DateTimeOffset end, DateTimeOffset now)
    {
        if (start.Offset != TimeSpan.Zero || end.Offset != TimeSpan.Zero || start >= end) throw new ArgumentException("Invalid period.");
        return new(id, tenantId, userId, now) { CategoryId = categoryId, Amount = amount.Amount,
            Currency = amount.Currency, PeriodStart = start, PeriodEnd = end };
    }
    public void Archive() { if (!IsArchived) { IsArchived = true; Version = checked(Version + 1); } }
}
