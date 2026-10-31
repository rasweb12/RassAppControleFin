using RassAppControleFin.Domain.ValueObjects;

namespace RassAppControleFin.Domain.Entities.Finance;

public sealed class Account : FinancialAggregate
{
    private Account(Guid id, Guid tenantId, string userId, DateTimeOffset createdAt) : base(id, tenantId, userId, createdAt) { }
    public string Name { get; private set; } = "";
    public string Currency { get; private set; } = "";
    public decimal Balance { get; private set; }
    public decimal OverdraftLimit { get; private set; }
    public AccountType Type { get; private set; } = AccountType.Bank;
    public string? Subtype { get; private set; }
    public string? Institution { get; private set; }
    public bool IncludeInNetWorth { get; private set; } = true;
    public bool IsArchived { get; private set; }
    public static Account Open(Guid id, Guid tenantId, string userId, string name, Money opening, DateTimeOffset now,
        decimal overdraftLimit = 0m, AccountType type = AccountType.Bank, string? subtype = null,
        string? institution = null, bool includeInNetWorth = true)
    {
        if (!Money.IsValid(overdraftLimit, opening.Currency, true)) throw new ArgumentException("Invalid overdraft limit.");
        if (!Enum.IsDefined(type)) throw new ArgumentException("Invalid account type.");
        return new(id, tenantId, userId, now)
        {
            Name = NameValue(name), Currency = opening.Currency, Balance = opening.Amount, OverdraftLimit = overdraftLimit,
            Type = type, Subtype = OptionalValue(subtype, 50), Institution = OptionalValue(institution, 120),
            IncludeInNetWorth = includeInNetWorth
        };
    }
    private static string? OptionalValue(string? value, int maximum)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maximum) throw new ArgumentException("Value is too long.");
        return normalized;
    }
    public bool TryApply(decimal delta)
    {
        if (IsArchived || decimal.Round(delta, 2) != delta || Balance + delta < -OverdraftLimit || delta > Money.Maximum - Balance) return false;
        Balance += delta; Version = checked(Version + 1); return true;
    }
    public void Rename(string name) { Name = NameValue(name); Version = checked(Version + 1); }
    public void UpdateDetails(string name, AccountType type, string? subtype, string? institution,
        bool includeInNetWorth, decimal overdraftLimit)
    {
        if (!Enum.IsDefined(type) || !Money.IsValid(overdraftLimit, Currency, true))
            throw new ArgumentException("Invalid account details.");
        Name = NameValue(name);
        Type = type;
        Subtype = OptionalValue(subtype, 50);
        Institution = OptionalValue(institution, 120);
        IncludeInNetWorth = includeInNetWorth;
        OverdraftLimit = overdraftLimit;
        if (Balance < -OverdraftLimit) throw new ArgumentException("Overdraft limit is below current balance.");
        Version = checked(Version + 1);
    }
    public bool Archive()
    {
        if (IsArchived) return false;
        IsArchived = true; Version = checked(Version + 1); return true;
    }
    public bool Reactivate()
    {
        if (!IsArchived) return false;
        IsArchived = false; Version = checked(Version + 1); return true;
    }
}
