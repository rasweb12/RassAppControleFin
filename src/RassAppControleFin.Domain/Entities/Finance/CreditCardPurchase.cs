using RassAppControleFin.Domain.ValueObjects;

namespace RassAppControleFin.Domain.Entities.Finance;

public sealed class CreditCardPurchase : FinancialAggregate
{
    private CreditCardPurchase(Guid id, Guid tenantId, string userId, DateTimeOffset createdAt)
        : base(id, tenantId, userId, createdAt) { }

    public Guid CardId { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Description { get; private set; } = "";
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "";
    public DateOnly PurchasedOn { get; private set; }
    public int InstallmentCount { get; private set; }
    public bool IsCancelled { get; private set; }

    public static CreditCardPurchase Create(Guid id, Guid tenantId, string userId, Guid cardId, Guid categoryId,
        string description, Money amount, DateOnly purchasedOn, int installmentCount, DateTimeOffset now)
    {
        if (cardId == Guid.Empty || categoryId == Guid.Empty || installmentCount is < 1 or > 360)
            throw new ArgumentException("Invalid credit card purchase.");
        return new(id, tenantId, userId, now)
        {
            CardId = cardId,
            CategoryId = categoryId,
            Description = DescriptionValue(description),
            Amount = amount.Amount,
            Currency = amount.Currency,
            PurchasedOn = purchasedOn,
            InstallmentCount = installmentCount
        };
    }

    public void Cancel()
    {
        if (IsCancelled) return;
        IsCancelled = true;
        Version = checked(Version + 1);
    }

    public IReadOnlyList<decimal> SplitInstallments()
    {
        var baseAmount = decimal.Floor(Amount / InstallmentCount * 100m) / 100m;
        var remainder = (int)decimal.Round(
            (Amount - (baseAmount * InstallmentCount)) * 100m,
            0,
            MidpointRounding.AwayFromZero);

        return Enumerable.Range(0, InstallmentCount)
            .Select(index => baseAmount + (index < remainder ? 0.01m : 0m))
            .ToArray();
    }

    private static string DescriptionValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Trim().Length > 500) throw new ArgumentException("Description exceeds limit.");
        return value.Trim();
    }
}
