using RassAppControleFin.Domain.ValueObjects;

namespace RassAppControleFin.Domain.Entities.Finance;

public sealed class Transfer : FinancialAggregate
{
    private Transfer(Guid id, Guid tenantId, string userId, DateTimeOffset createdAt) : base(id, tenantId, userId, createdAt) { }
    public Guid SourceAccountId { get; private set; }
    public Guid DestinationAccountId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "";
    public DateTimeOffset OccurredAt { get; private set; }
    public string Description { get; private set; } = "";
    public Guid OperationId { get; private set; }
    public string Status { get; private set; } = "Posted";
    public static Transfer Create(Guid id, Guid tenantId, string userId, Guid source, Guid destination, Money money,
        DateTimeOffset occurredAt, string description, Guid operationId, DateTimeOffset now)
    {
        if (source == Guid.Empty || destination == Guid.Empty || source == destination || operationId == Guid.Empty
            || occurredAt.Offset != TimeSpan.Zero || description.Length > 500) throw new ArgumentException("Invalid transfer.");
        return new(id, tenantId, userId, now) { SourceAccountId = source, DestinationAccountId = destination,
            Amount = money.Amount, Currency = money.Currency, OccurredAt = occurredAt, Description = description.Trim(), OperationId = operationId };
    }
}
