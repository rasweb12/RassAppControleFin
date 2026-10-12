using RassAppControleFin.Domain.ValueObjects;

namespace RassAppControleFin.Domain.Entities.Finance;

public sealed class Account : FinancialAggregate
{
    private Account(Guid id, Guid tenantId, string userId, DateTimeOffset createdAt) : base(id, tenantId, userId, createdAt) { }
    public string Name { get; private set; } = "";
    public string Currency { get; private set; } = "";
    public decimal Balance { get; private set; }
    public bool IsArchived { get; private set; }
    public static Account Open(Guid id, Guid tenantId, string userId, string name, Money opening, DateTimeOffset now)
        => new(id, tenantId, userId, now) { Name = NameValue(name), Currency = opening.Currency, Balance = opening.Amount };
    public bool TryApply(decimal delta)
    {
        if (IsArchived || decimal.Round(delta, 2) != delta || delta < -Balance || delta > Money.Maximum - Balance) return false;
        Balance += delta; Version = checked(Version + 1); return true;
    }
    public void Rename(string name) { Name = NameValue(name); Version = checked(Version + 1); }
    public bool Archive()
    {
        if (Balance != 0 || IsArchived) return false;
        IsArchived = true; Version = checked(Version + 1); return true;
    }
}
