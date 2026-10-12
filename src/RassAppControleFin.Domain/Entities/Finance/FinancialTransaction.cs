using RassAppControleFin.Domain.ValueObjects;

namespace RassAppControleFin.Domain.Entities.Finance;

public enum TransactionStatus { Pending, Posted, Cancelled }
public sealed class FinancialTransaction : FinancialAggregate
{
    private FinancialTransaction(Guid id, Guid tenantId, string userId, DateTimeOffset createdAt) : base(id, tenantId, userId, createdAt) { }
    public Guid AccountId { get; private set; }
    public Guid CategoryId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "";
    public FinancialFlow Flow { get; private set; }
    public TransactionStatus Status { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string Description { get; private set; } = "";
    public decimal PostedDelta => Status == TransactionStatus.Posted ? (Flow == FinancialFlow.Income ? Amount : -Amount) : 0;
    public static FinancialTransaction Create(Guid id, Guid tenantId, string userId, Guid accountId, Guid categoryId,
        Money money, FinancialFlow flow, TransactionStatus status, DateTimeOffset occurredAt, string description, DateTimeOffset now)
    {
        if (accountId == Guid.Empty || categoryId == Guid.Empty || !Enum.IsDefined(flow)) throw new ArgumentException("Invalid transaction.");
        var transaction = new FinancialTransaction(id, tenantId, userId, now) { AccountId = accountId, Flow = flow, Currency = money.Currency };
        transaction.Set(categoryId, money, status, occurredAt, description);
        return transaction;
    }
    public void Edit(Guid categoryId, Money money, TransactionStatus status, DateTimeOffset occurredAt, string description)
    {
        if (Status == TransactionStatus.Cancelled || money.Currency != Currency) throw new InvalidOperationException("Transaction cannot be edited.");
        Set(categoryId, money, status, occurredAt, description); Version = checked(Version + 1);
    }
    public void Cancel() { Status = TransactionStatus.Cancelled; Version = checked(Version + 1); }
    private void Set(Guid categoryId, Money money, TransactionStatus status, DateTimeOffset occurredAt, string description)
    {
        if (categoryId == Guid.Empty || !Enum.IsDefined(status) || status == TransactionStatus.Cancelled
            || occurredAt.Offset != TimeSpan.Zero || description.Length > 500) throw new ArgumentException("Invalid transaction.");
        CategoryId = categoryId; Amount = money.Amount; Status = status; OccurredAt = occurredAt; Description = description.Trim();
    }
}
