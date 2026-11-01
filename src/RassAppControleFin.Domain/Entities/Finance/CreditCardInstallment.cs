using RassAppControleFin.Domain.ValueObjects;

namespace RassAppControleFin.Domain.Entities.Finance;

public sealed class CreditCardInstallment : FinancialAggregate
{
    private CreditCardInstallment(Guid id, Guid tenantId, string userId, DateTimeOffset createdAt)
        : base(id, tenantId, userId, createdAt) { }

    public Guid PurchaseId { get; private set; }
    public Guid CardId { get; private set; }
    public Guid InvoiceId { get; private set; }
    public int Number { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "";
    public DateOnly Competence { get; private set; }
    public string Status { get; private set; } = "Scheduled";

    public static CreditCardInstallment Create(Guid id, Guid tenantId, string userId, Guid purchaseId, Guid cardId,
        Guid invoiceId, int number, decimal amount, string currency, DateOnly competence, DateTimeOffset now)
    {
        if (purchaseId == Guid.Empty || cardId == Guid.Empty || invoiceId == Guid.Empty || number < 1
            || !Money.IsValid(amount, currency)) throw new ArgumentException("Invalid credit card installment.");
        return new(id, tenantId, userId, now)
        {
            PurchaseId = purchaseId, CardId = cardId, InvoiceId = invoiceId, Number = number,
            Amount = amount, Currency = currency, Competence = competence
        };
    }

    public void Cancel()
    {
        if (Status == "Cancelled") return;
        Status = "Cancelled";
        Version = checked(Version + 1);
    }
}
