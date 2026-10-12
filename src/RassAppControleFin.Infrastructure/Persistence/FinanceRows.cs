using RassAppControleFin.Domain.Entities.Finance;

namespace RassAppControleFin.Infrastructure.Persistence;

internal interface IAppendOnlyFinancialRecord : IFinancialResource;
internal sealed class LedgerEntry : IAppendOnlyFinancialRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = "";
    public Guid AccountId { get; set; }
    public decimal Delta { get; set; }
    public string Kind { get; set; } = "";
    public Guid ResourceId { get; set; }
    public Guid? TransferId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
internal sealed class CardMovementRow : IAppendOnlyFinancialRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = "";
    public Guid CardId { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? CategoryId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "";
    public string Kind { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
    public string Description { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
internal sealed class SyncOperationReceipt : IAppendOnlyFinancialRecord
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = "";
    public Guid OperationId { get; set; }
    public string PayloadHash { get; set; } = "";
    public string ResponseJson { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
internal sealed class FinancialOutboxRow : IAppendOnlyFinancialRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = "";
    public Guid OperationId { get; set; }
    public Guid ResourceId { get; set; }
    public string EventType { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
