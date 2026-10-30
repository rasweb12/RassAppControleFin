namespace RassAppControleFin.Contracts;

public sealed record OpenAccountRequest(Guid OperationId, string Name, string Currency, decimal OpeningBalance, decimal OverdraftLimit = 0m,
    string AccountType = "Bank", string? Subtype = null, string? Institution = null, bool IncludeInNetWorth = true);
public sealed record MaintainResourceRequest(Guid OperationId, long Version, string Name, bool Archive);
public sealed record UpdateAccountRequest(Guid OperationId, long Version, string Name, decimal OverdraftLimit,
    string AccountType, string? Subtype, string? Institution, bool IncludeInNetWorth);
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
public sealed record AccountDto(Guid Id, string Name, string Currency, decimal Balance, decimal OverdraftLimit, string AccountType,
    string? Subtype, string? Institution, bool IncludeInNetWorth, long Version, bool IsArchived);
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
public sealed record ConfigurePayYourselfFirstPlanRequest(Guid OperationId, long? Version, decimal CurrentRate,
    decimal? GoalRate, DateOnly EffectiveFrom);
public sealed record ConfigureEmergencyFundPlanRequest(Guid OperationId, long? Version, string TargetType,
    decimal? TargetMultiple, decimal TargetAmount, decimal CurrentAmount, decimal MonthlyContributionTarget,
    string Currency, bool LiquidityRequired, string? Notes);
public sealed record CreateInvestmentAssetRequest(Guid OperationId, string Name, string? Ticker, string AssetClass,
    string AssetSubClass, Guid? AccountId, string? Issuer, string Indexer, decimal? Rate, DateOnly? MaturityDate,
    string LiquidityType, bool FgcProtected, bool TaxExempt, string? Notes);
public sealed record UpdateInvestmentAssetRequest(Guid OperationId, long Version, string Name, string? Ticker,
    string AssetClass, string AssetSubClass, Guid? AccountId, string? Issuer, string Indexer, decimal? Rate,
    DateOnly? MaturityDate, string LiquidityType, bool FgcProtected, bool TaxExempt, string? Notes, bool Active);
public sealed record RecordInvestmentTransactionRequest(Guid OperationId, Guid InvestmentAssetId, DateOnly Date,
    string Type, decimal? Quantity, decimal? UnitPrice, decimal GrossAmount, decimal Fees, decimal Taxes,
    string Currency, string? Notes);
public sealed record TargetPortfolioAllocationRequest(Guid? Id, long? Version, Guid? InvestmentAssetId, string Key,
    string AssetClass, decimal Weight);
public sealed record SetTargetPortfolioRequest(Guid OperationId, IReadOnlyList<TargetPortfolioAllocationRequest> Allocations);
public sealed record PayYourselfFirstPlanDto(Guid Id, decimal CurrentRate, decimal? GoalRate, DateOnly EffectiveFrom, long Version);
public sealed record EmergencyFundPlanDto(Guid Id, string TargetType, decimal? TargetMultiple, decimal TargetAmount,
    decimal CurrentAmount, decimal MonthlyContributionTarget, string Currency, bool LiquidityRequired, string? Notes, long Version);
public sealed record InvestmentAssetDto(Guid Id, string Name, string? Ticker, string AssetClass, string AssetSubClass,
    Guid? AccountId, string? Issuer, string Indexer, decimal? Rate, DateOnly? MaturityDate, string LiquidityType,
    bool FgcProtected, bool TaxExempt, bool Active, string? Notes, DateTimeOffset UpdatedAt, long Version);
public sealed record InvestmentTransactionDto(Guid Id, Guid InvestmentAssetId, DateOnly Date, string Type, decimal? Quantity,
    decimal? UnitPrice, decimal GrossAmount, decimal Fees, decimal Taxes, decimal NetAmount, string Currency, string? Notes, long Version);
public sealed record TargetPortfolioAllocationDto(Guid Id, Guid? InvestmentAssetId, string Key, string AssetClass, decimal Weight, long Version);
public sealed record DashboardMetricDto(string Currency, decimal NetWorth, decimal Income, decimal Expense,
    decimal SavingsRate, decimal BudgetUsed, int ActiveGoals);
public sealed record DashboardDto(DateTimeOffset From, DateTimeOffset To, IReadOnlyList<DashboardMetricDto> Metrics);
public sealed record SyncChangeDto(Guid OperationId, Guid ResourceId, string EventType, DateTimeOffset OccurredAt);
public sealed record SyncPullDto(IReadOnlyList<SyncChangeDto> Changes, string? NextCursor,
    string? AppliedCursor);
public sealed record AskFinancialAssistantRequest(string Question, string Period);
public sealed record FinancialAssistantResponse(string Text, int TokensUsed, bool Generated);
