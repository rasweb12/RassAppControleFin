using RassAppControleFin.Domain.ValueObjects;

namespace RassAppControleFin.Domain.Entities.Finance;

public sealed class CreditCard : FinancialAggregate
{
    private CreditCard(Guid id, Guid tenantId, string userId, DateTimeOffset createdAt) : base(id, tenantId, userId, createdAt) { }
    public string Name { get; private set; } = "";
    public string Currency { get; private set; } = "";
    public decimal Limit { get; private set; }
    public decimal Outstanding { get; private set; }
    public int ClosingDay { get; private set; }
    public int DueDay { get; private set; }
    public bool IsArchived { get; private set; }
    public static CreditCard Create(Guid id, Guid tenantId, string userId, string name, Money limit,
        int closingDay, int dueDay, DateTimeOffset now)
    {
        if (closingDay is < 1 or > 28 || dueDay is < 1 or > 28) throw new ArgumentException("Invalid billing day.");
        return new(id, tenantId, userId, now) { Name = NameValue(name), Limit = limit.Amount, Currency = limit.Currency,
            ClosingDay = closingDay, DueDay = dueDay };
    }
    public bool TryCharge(Money money)
    {
        if (IsArchived || money.Currency != Currency || money.Amount > Limit - Outstanding) return false;
        Outstanding += money.Amount; Version = checked(Version + 1); return true;
    }
    public bool TryPay(Money money)
    {
        if (IsArchived || money.Currency != Currency || money.Amount > Outstanding) return false;
        Outstanding -= money.Amount; Version = checked(Version + 1); return true;
    }
    public void Rename(string name) { Name = NameValue(name); Version = checked(Version + 1); }
    public bool Archive()
    { if (Outstanding != 0 || IsArchived) return false; IsArchived = true; Version = checked(Version + 1); return true; }
}
