using RassAppControleFin.Domain.ValueObjects;

namespace RassAppControleFin.Domain.Entities.Finance;

public sealed class CreditCard : FinancialAggregate
{
    private CreditCard(Guid id, Guid tenantId, string userId, DateTimeOffset createdAt) : base(id, tenantId, userId, createdAt) { }
    public string Name { get; private set; } = "";
    public string Currency { get; private set; } = "";
    public decimal Limit { get; private set; }
    public decimal Outstanding { get; private set; }
    public string Institution { get; private set; } = "";
    public string? Brand { get; private set; }
    public Guid? PreferredPaymentAccountId { get; private set; }
    public string? AccentColor { get; private set; }
    public int ClosingDay { get; private set; }
    public int DueDay { get; private set; }
    public bool IsArchived { get; private set; }
    public static CreditCard Create(Guid id, Guid tenantId, string userId, string name, Money limit,
        int closingDay, int dueDay, DateTimeOffset now, string? institution = null,
        string? brand = null, Guid? preferredPaymentAccountId = null, string? accentColor = null)
    {
        if (closingDay is < 1 or > 31 || dueDay is < 1 or > 31) throw new ArgumentException("Invalid billing day.");
        return new(id, tenantId, userId, now) { Name = NameValue(name), Limit = limit.Amount, Currency = limit.Currency,
            ClosingDay = closingDay, DueDay = dueDay,
            Institution = string.IsNullOrWhiteSpace(institution) ? "Não informado" : NameValue(institution),
            Brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim(),
            PreferredPaymentAccountId = preferredPaymentAccountId,
            AccentColor = string.IsNullOrWhiteSpace(accentColor) ? null : accentColor.Trim() };
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
    public decimal AvailableLimit => Limit - Outstanding;
    public void UpdateDetails(string name, string institution, string? brand, Guid? preferredPaymentAccountId, string? accentColor)
    {
        Name = NameValue(name);
        Institution = NameValue(institution);
        if (brand?.Trim().Length > 40 || accentColor?.Trim().Length > 16) throw new ArgumentException("Invalid card details.");
        Brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim();
        PreferredPaymentAccountId = preferredPaymentAccountId;
        AccentColor = string.IsNullOrWhiteSpace(accentColor) ? null : accentColor.Trim();
        Version = checked(Version + 1);
    }
    public DateOnly BillingDate(int year, int month, bool dueDate)
    {
        var day = dueDate ? DueDay : ClosingDay;
        return new DateOnly(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));
    }
    public void Rename(string name) { Name = NameValue(name); Version = checked(Version + 1); }
    public bool Archive()
    { if (Outstanding != 0 || IsArchived) return false; IsArchived = true; Version = checked(Version + 1); return true; }
    public bool Reactivate()
    {
        if (!IsArchived) return false;
        IsArchived = false;
        Version = checked(Version + 1);
        return true;
    }
}
