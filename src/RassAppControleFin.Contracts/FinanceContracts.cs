namespace RassAppControleFin.Contracts;

public sealed record OpenAccountRequest(Guid OperationId, string Name, string Currency, decimal OpeningBalance, decimal OverdraftLimit = 0m);
public sealed record MaintainResourceRequest(Guid OperationId, long Version, string Name, bool Archive);
public sealed record CreateCategoryRequest(Guid OperationId, string Name, string Flow);
public sealed record CreateCardRequest(Guid OperationId, string Name, string Currency, decimal Limit, int ClosingDay, int DueDay);
public sealed record CreateTransactionRequest(Guid OperationId, Guid AccountId, Guid CategoryId, decimal Amount,
    string Currency, string Flow, string Status, DateTimeOffset OccurredAt, string Description, long AccountVersion);
public sealed record EditTransactionRequest(Guid OperationId, long Version, Guid CategoryId, decimal Amount,
    string Currency, string Status, DateTimeOffset OccurredAt, string Description, long AccountVersion);
public sealed record CancelTransactionRequest(Guid OperationId, long Version, long AccountVersion);
public sealed record CreateTransferRequest(Guid OperationId, Guid SourceAccountId, Guid DestinationAccountId, decimal Amount,
    string Currency, DateTimeOffset OccurredAt, string Description, long SourceVersion, long DestinationVersion);
public sealed record CreateCardMovementRequest(Guid OperationId, Guid CardId, long CardVersion, decimal Amount, string Currency,
    string Kind, Guid? AccountId, long? AccountVersion, Guid? CategoryId, DateTimeOffset OccurredAt, string Description);

public sealed record MutationReceiptDto(Guid ResourceId, Guid OperationId, long Version, long? SourceVersion,
    long? DestinationVersion, decimal? SourceBalance, decimal? DestinationBalance);
public sealed record AccountDto(Guid Id, string Name, string Currency, decimal Balance, decimal OverdraftLimit, long Version, bool IsArchived);
public sealed record CategoryDto(Guid Id, string Name, string Flow, long Version, bool IsArchived);
public sealed record CardDto(Guid Id, string Name, string Currency, decimal Limit, decimal Outstanding,
    int ClosingDay, int DueDay, long Version, bool IsArchived);
public sealed record TransactionDto(Guid Id, Guid AccountId, Guid CategoryId, decimal Amount, string Currency,
    string Flow, string Status, DateTimeOffset OccurredAt, string Description, long Version);
public sealed record TransferDto(Guid Id, Guid SourceAccountId, Guid DestinationAccountId, decimal Amount,
    string Currency, DateTimeOffset OccurredAt, string Description, string Status);
public sealed record CardMovementDto(Guid Id, Guid CardId, Guid? AccountId, Guid? CategoryId, decimal Amount,
    string Currency, string Kind, DateTimeOffset OccurredAt, string Description);
public sealed record PageDto<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
public sealed record TransactionSummaryDto(string Currency, decimal Income, decimal Expense);
public sealed record TransactionPageDto(PageDto<TransactionDto> Page, IReadOnlyList<TransactionSummaryDto> Summary);
public sealed record CreateBudgetRequest(Guid OperationId, Guid? CategoryId, decimal Amount, string Currency,
    DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd);
public sealed record VersionedOperationRequest(Guid OperationId, long Version);
public sealed record CreateGoalRequest(Guid OperationId, string Name, decimal TargetAmount, string Currency, DateTimeOffset? TargetDate);
public sealed record AddGoalProgressRequest(Guid OperationId, long Version, decimal Amount, string Currency);
public sealed record BudgetDto(Guid Id, Guid? CategoryId, decimal Amount, decimal Spent, string Currency,
    DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd, long Version, bool IsArchived);
public sealed record GoalDto(Guid Id, string Name, decimal TargetAmount, decimal CurrentAmount, string Currency,
    DateTimeOffset? TargetDate, string Status, long Version);
public sealed record CreateExpenseReductionGoalRequest(Guid OperationId, Guid? CategoryId, decimal ReferenceAmount,
    decimal TargetAmount, string Currency, DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd);
public sealed record ExpenseReductionGoalDto(Guid Id, Guid? CategoryId, decimal ReferenceAmount, decimal TargetAmount,
    decimal ActualExpense, string Currency, DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd, long Version, bool IsArchived);
public sealed record FinancialNotificationDto(Guid Id, string Type, string Title, string Message, DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt, long Version);
public sealed record DashboardMetricDto(string Currency, decimal NetWorth, decimal Income, decimal Expense,
    decimal SavingsRate, decimal BudgetUsed, int ActiveGoals);
public sealed record DashboardDto(DateTimeOffset From, DateTimeOffset To, IReadOnlyList<DashboardMetricDto> Metrics);
public sealed record SyncChangeDto(Guid OperationId, Guid ResourceId, string EventType, DateTimeOffset OccurredAt);
public sealed record SyncPullDto(IReadOnlyList<SyncChangeDto> Changes, string? NextCursor,
    string? AppliedCursor);
public sealed record AskFinancialAssistantRequest(string Question, string Period);
public sealed record FinancialAssistantResponse(string Text, int TokensUsed, bool Generated);
