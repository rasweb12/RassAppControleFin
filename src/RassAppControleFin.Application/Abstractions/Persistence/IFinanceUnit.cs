using RassAppControleFin.Application.Features.Finance;
using RassAppControleFin.Domain.Entities.Finance;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Application.Abstractions.Persistence;

public sealed record ReceiptLookup(bool Exists, string? Json);
public sealed record BudgetAlert(Guid BudgetId, decimal Amount, decimal Spent, int Threshold);
public interface IFinanceUnitFactory { Task<Result<IFinanceUnit>> BeginAsync(CancellationToken ct); }
public interface IFinanceUnit : IAsyncDisposable
{
    Task<Result<ReceiptLookup>> ClaimAsync(Guid operationId, string payloadHash, CancellationToken ct);
    Task<Account?> AccountAsync(Guid id, CancellationToken ct);
    Task<Category?> CategoryAsync(Guid id, CancellationToken ct);
    Task<CreditCard?> CardAsync(Guid id, CancellationToken ct);
    Task<CreditCardInvoice?> CreditCardInvoiceAsync(Guid id, CancellationToken ct);
    Task<CreditCardInvoice?> CreditCardInvoiceAsync(Guid cardId, DateOnly competence, CancellationToken ct);
    Task<FinancialTransaction?> TransactionAsync(Guid id, CancellationToken ct);
    Task<Budget?> BudgetAsync(Guid id, CancellationToken ct);
    Task<Goal?> GoalAsync(Guid id, CancellationToken ct);
    Task<ExpenseReductionGoal?> ExpenseReductionGoalAsync(Guid id, CancellationToken ct);
    Task<FinancialNotification?> NotificationAsync(Guid id, CancellationToken ct);
    Task<PayYourselfFirstPlan?> PayYourselfFirstPlanAsync(CancellationToken ct);
    Task<PayYourselfFirstPlanRevision?> PayYourselfFirstPlanRevisionAsync(Guid planId, DateOnly effectiveFrom,
        CancellationToken ct);
    Task<EmergencyFundPlan?> EmergencyFundPlanAsync(CancellationToken ct);
    Task<InvestmentAsset?> InvestmentAssetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<TargetPortfolioAllocation>> TargetPortfolioAllocationsAsync(CancellationToken ct);
    Task<IReadOnlyList<BudgetAlert>> BudgetAlertsAsync(Guid categoryId, string currency, DateTimeOffset occurredAt,
        decimal pendingExpense, CancellationToken ct);
    Task<bool> NotificationExistsAsync(string type, CancellationToken ct);
    void Add(FinancialAggregate resource);
    void Ledger(Guid accountId, decimal delta, string kind, Guid resourceId, Guid? transferId = null);
    void CardMovement(Guid cardId, Guid? accountId, Guid? categoryId, decimal amount, string currency,
        string kind, DateTimeOffset occurredAt, string description, Guid id, Guid? invoiceId = null, Guid? purchaseId = null);
    Task<Result> CommitAsync(Guid operationId, string payloadHash, MutationReceipt receipt, string action, CancellationToken ct);
}
public interface IFinanceReadStore
{
    Task<Result<Page<AccountView>>> AccountsAsync(ListAccountsQuery query, CancellationToken ct);
    Task<Result<Page<CategoryView>>> CategoriesAsync(ListCategoriesQuery query, CancellationToken ct);
    Task<Result<Page<CardView>>> CardsAsync(ListCardsQuery query, CancellationToken ct);
    Task<Result<Page<TransferView>>> TransfersAsync(ListTransfersQuery query, CancellationToken ct);
    Task<Result<TransactionPage>> TransactionsAsync(ListTransactionsQuery query, CancellationToken ct);
    Task<Result<Page<CardMovementView>>> CardMovementsAsync(ListCardMovementsQuery query, CancellationToken ct);
    Task<Result<Page<CreditCardInvoiceView>>> CreditCardInvoicesAsync(ListCreditCardInvoicesQuery query, CancellationToken ct);
    Task<Result<Page<BudgetView>>> BudgetsAsync(ListBudgetsQuery query, CancellationToken ct);
    Task<Result<Page<GoalView>>> GoalsAsync(ListGoalsQuery query, CancellationToken ct);
    Task<Result<Page<ExpenseReductionGoalView>>> ExpenseReductionGoalsAsync(ListExpenseReductionGoalsQuery query, CancellationToken ct);
    Task<Result<IReadOnlyList<ExpenseReductionOpportunityView>>> ExpenseReductionOpportunitiesAsync(ListExpenseReductionOpportunitiesQuery query, CancellationToken ct);
    Task<Result<Page<FinancialNotificationView>>> NotificationsAsync(ListNotificationsQuery query, CancellationToken ct);
    Task<Result<PayYourselfFirstPlanView?>> PayYourselfFirstPlanAsync(CancellationToken ct);
    Task<Result<EmergencyFundPlanView?>> EmergencyFundPlanAsync(CancellationToken ct);
    Task<Result<Page<InvestmentAssetView>>> InvestmentAssetsAsync(ListInvestmentAssetsQuery query, CancellationToken ct);
    Task<Result<Page<InvestmentTransactionView>>> InvestmentTransactionsAsync(ListInvestmentTransactionsQuery query, CancellationToken ct);
    Task<Result<IReadOnlyList<TargetPortfolioAllocationView>>> TargetPortfolioAsync(CancellationToken ct);
    Task<Result<DashboardView>> DashboardAsync(DashboardQuery query, CancellationToken ct);
}

/// <summary>
/// Read-only analytical projection used by the financial assistant and dashboard insights.
/// Its implementation must use the authenticated tenant/user scope already enforced by the
/// finance read transaction; it is deliberately separate from mutations and from the public API.
/// </summary>
public interface IFinanceAiReadStore
{
    Task<Result<FinanceAiSnapshot>> AiSnapshotAsync(FinanceAiContextQuery query, CancellationToken ct);
}

public sealed record SyncChange(Guid OperationId, Guid ResourceId, string EventType,
    DateTimeOffset OccurredAt, Guid CursorId);
public sealed record SyncChangePage(IReadOnlyList<SyncChange> Changes, bool HasMore);
public interface IFinanceSyncReadStore
{
    Task<Result<SyncChangePage>> PullAsync(DateTimeOffset? after, Guid? afterId,
        int limit, CancellationToken ct);
}
