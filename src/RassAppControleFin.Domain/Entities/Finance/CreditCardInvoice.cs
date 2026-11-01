using RassAppControleFin.Domain.ValueObjects;

namespace RassAppControleFin.Domain.Entities.Finance;

public sealed class CreditCardInvoice : FinancialAggregate
{
    private CreditCardInvoice(Guid id, Guid tenantId, string userId, DateTimeOffset createdAt)
        : base(id, tenantId, userId, createdAt) { }

    public Guid CardId { get; private set; }
    public DateOnly Competence { get; private set; }
    public DateOnly ClosingDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public string Currency { get; private set; } = "";
    public string Status { get; private set; } = "Open";
    public decimal OpenAmount => TotalAmount - PaidAmount;

    public static CreditCardInvoice Open(Guid id, Guid tenantId, string userId, Guid cardId, DateOnly competence,
        DateOnly closingDate, DateOnly dueDate, string currency, DateTimeOffset now)
    {
        if (cardId == Guid.Empty || closingDate < competence || dueDate < closingDate || !Money.IsValid(0m, currency, true))
            throw new ArgumentException("Invalid credit card invoice.");
        return new(id, tenantId, userId, now)
        {
            CardId = cardId,
            Competence = competence,
            ClosingDate = closingDate,
            DueDate = dueDate,
            Currency = currency
        };
    }

    public void AddCharge(Money amount)
    {
        if (Status is "Paid" or "Cancelled" || amount.Currency != Currency) throw new InvalidOperationException("Invoice cannot accept charges.");
        TotalAmount += amount.Amount;
        Version = checked(Version + 1);
    }

    public void RegisterPayment(Money amount)
    {
        if (Status == "Cancelled" || amount.Currency != Currency || amount.Amount > OpenAmount)
            throw new InvalidOperationException("Invalid invoice payment.");
        PaidAmount += amount.Amount;
        Status = PaidAmount == TotalAmount ? "Paid" : "PartiallyPaid";
        Version = checked(Version + 1);
    }

    public void Close(DateOnly today)
    {
        if (Status != "Open" || today < ClosingDate) throw new InvalidOperationException("Invoice cannot be closed.");
        Status = "Closed";
        Version = checked(Version + 1);
    }
}
