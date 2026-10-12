using RassAppControleFin.Domain.ValueObjects;

namespace RassAppControleFin.Domain.Entities.Finance;

public sealed class Goal : FinancialAggregate
{
    private Goal(Guid id, Guid tenantId, string userId, DateTimeOffset createdAt) : base(id, tenantId, userId, createdAt) { }
    public string Name { get; private set; } = "";
    public decimal TargetAmount { get; private set; }
    public decimal CurrentAmount { get; private set; }
    public string Currency { get; private set; } = "";
    public DateTimeOffset? TargetDate { get; private set; }
    public string Status { get; private set; } = "Active";
    public static Goal Create(Guid id, Guid tenantId, string userId, string name, Money target,
        DateTimeOffset? targetDate, DateTimeOffset now)
    {
        if (targetDate is { } date && date.Offset != TimeSpan.Zero) throw new ArgumentException("Invalid target date.");
        return new(id, tenantId, userId, now) { Name = NameValue(name), TargetAmount = target.Amount,
            Currency = target.Currency, TargetDate = targetDate };
    }
    public bool AddProgress(Money amount)
    {
        if (Status != "Active" || amount.Currency != Currency || amount.Amount > TargetAmount - CurrentAmount) return false;
        CurrentAmount += amount.Amount;
        if (CurrentAmount == TargetAmount) Status = "Completed";
        Version = checked(Version + 1); return true;
    }
    public void Archive() { if (Status != "Archived") { Status = "Archived"; Version = checked(Version + 1); } }
}
