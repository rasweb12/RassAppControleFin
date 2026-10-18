using MediatR;
using RassAppControleFin.Security;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Application.Features.Finance;

public sealed record MutationReceipt(Guid ResourceId, Guid OperationId, long Version, long? SourceVersion = null,
    long? DestinationVersion = null, decimal? SourceBalance = null, decimal? DestinationBalance = null);
public interface IFinanceMutation : IRequest<Result<MutationReceipt>> { Guid OperationId { get; } string Permission { get; } }
public sealed record OpenAccountCommand(Guid OperationId, string Name, string Currency, decimal OpeningBalance) : IFinanceMutation
{ public string Permission => Permissions.AccountsWrite; }
public sealed record MaintainResourceCommand(Guid OperationId, string Resource, Guid Id, long Version, string Name, bool Archive) : IFinanceMutation
{ public string Permission => Resource switch { "account" => Permissions.AccountsWrite, "category" => Permissions.CategoriesWrite, _ => Permissions.CreditCardsWrite }; }
public sealed record CreateCategoryCommand(Guid OperationId, string Name, string Flow) : IFinanceMutation
{ public string Permission => Permissions.CategoriesWrite; }
public sealed record CreateCardCommand(Guid OperationId, string Name, string Currency, decimal Limit, int ClosingDay, int DueDay) : IFinanceMutation
{ public string Permission => Permissions.CreditCardsWrite; }
public sealed record CreateTransactionCommand(Guid OperationId, Guid AccountId, Guid CategoryId, decimal Amount,
    string Currency, string Flow, string Status, DateTimeOffset OccurredAt, string Description, long AccountVersion) : IFinanceMutation
{ public string Permission => Permissions.TransactionsWrite; }
public sealed record EditTransactionCommand(Guid OperationId, Guid Id, long Version, Guid CategoryId, decimal Amount,
    string Currency, string Status, DateTimeOffset OccurredAt, string Description, long AccountVersion) : IFinanceMutation
{ public string Permission => Permissions.TransactionsWrite; }
public sealed record CancelTransactionCommand(Guid OperationId, Guid Id, long Version, long AccountVersion) : IFinanceMutation
{ public string Permission => Permissions.TransactionsWrite; }
public sealed record TransferCommand(Guid OperationId, Guid SourceAccountId, Guid DestinationAccountId, decimal Amount,
    string Currency, DateTimeOffset OccurredAt, string Description, long SourceVersion, long DestinationVersion) : IFinanceMutation
{ public string Permission => Permissions.TransfersWrite; }
public sealed record CardMovementCommand(Guid OperationId, Guid CardId, long CardVersion, decimal Amount, string Currency,
    string Kind, Guid? AccountId, long? AccountVersion, Guid? CategoryId, DateTimeOffset OccurredAt, string Description) : IFinanceMutation
{ public string Permission => Permissions.CreditCardsWrite; }

public sealed record AccountView(Guid Id, string Name, string Currency, decimal Balance, long Version, bool IsArchived);
public sealed record CategoryView(Guid Id, string Name, string Flow, long Version, bool IsArchived);
public sealed record CardView(Guid Id, string Name, string Currency, decimal Limit, decimal Outstanding,
    int ClosingDay, int DueDay, long Version, bool IsArchived);
public sealed record TransactionView(Guid Id, Guid AccountId, Guid CategoryId, decimal Amount, string Currency,
    string Flow, string Status, DateTimeOffset OccurredAt, string Description, long Version);
public sealed record TransferView(Guid Id, Guid SourceAccountId, Guid DestinationAccountId, decimal Amount,
    string Currency, DateTimeOffset OccurredAt, string Description, string Status);
public sealed record CardMovementView(Guid Id, Guid CardId, Guid? AccountId, Guid? CategoryId, decimal Amount,
    string Currency, string Kind, DateTimeOffset OccurredAt, string Description);
public sealed record BudgetView(Guid Id, Guid? CategoryId, decimal Amount, decimal Spent, string Currency,
    DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd, long Version, bool IsArchived);
public sealed record GoalView(Guid Id, string Name, decimal TargetAmount, decimal CurrentAmount, string Currency,
    DateTimeOffset? TargetDate, string Status, long Version);
public sealed record ExpenseReductionGoalView(Guid Id, Guid? CategoryId, decimal ReferenceAmount, decimal TargetAmount,
    decimal ActualExpense, string Currency, DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd, long Version, bool IsArchived);
public sealed record FinancialNotificationView(Guid Id, string Type, string Title, string Message, DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt, long Version);
public sealed record DashboardMetric(string Currency, decimal NetWorth, decimal Income, decimal Expense,
    decimal SavingsRate, decimal BudgetUsed, int ActiveGoals);
public sealed record DashboardView(DateTimeOffset From, DateTimeOffset To, IReadOnlyList<DashboardMetric> Metrics);
public sealed record Page<T>(IReadOnlyList<T> Items, int Total, int PageNumber, int PageSize);
public sealed record TransactionSummary(string Currency, decimal Income, decimal Expense);
public sealed record TransactionPage(Page<TransactionView> Page, IReadOnlyList<TransactionSummary> Summary);
public interface IPageQuery { int Page { get; } int PageSize { get; } }
public sealed record ListAccountsQuery(int Page = 1, int PageSize = 50) : IRequest<Result<Page<AccountView>>>, IPageQuery;
public sealed record ListCategoriesQuery(int Page = 1, int PageSize = 50) : IRequest<Result<Page<CategoryView>>>, IPageQuery;
public sealed record ListCardsQuery(int Page = 1, int PageSize = 50) : IRequest<Result<Page<CardView>>>, IPageQuery;
public sealed record ListTransfersQuery(int Page = 1, int PageSize = 50) : IRequest<Result<Page<TransferView>>>, IPageQuery;
public sealed record ListCardMovementsQuery(Guid CardId, int Page = 1, int PageSize = 50) : IRequest<Result<Page<CardMovementView>>>, IPageQuery;
public sealed record ListTransactionsQuery(string? Search = null, Guid? AccountId = null, Guid? CategoryId = null,
    string? Flow = null, string? Status = null, DateTimeOffset? From = null, DateTimeOffset? To = null,
    int Page = 1, int PageSize = 50) : IRequest<Result<TransactionPage>>, IPageQuery;
public sealed record CreateBudgetCommand(Guid OperationId, Guid? CategoryId, decimal Amount, string Currency,
    DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd) : IFinanceMutation
{ public string Permission => Permissions.TransactionsWrite; }
public sealed record ArchiveBudgetCommand(Guid OperationId, Guid Id, long Version) : IFinanceMutation
{ public string Permission => Permissions.TransactionsWrite; }
public sealed record CreateGoalCommand(Guid OperationId, string Name, decimal TargetAmount, string Currency,
    DateTimeOffset? TargetDate) : IFinanceMutation
{ public string Permission => Permissions.TransactionsWrite; }
public sealed record AddGoalProgressCommand(Guid OperationId, Guid Id, long Version, decimal Amount, string Currency) : IFinanceMutation
{ public string Permission => Permissions.TransactionsWrite; }
public sealed record ArchiveGoalCommand(Guid OperationId, Guid Id, long Version) : IFinanceMutation
{ public string Permission => Permissions.TransactionsWrite; }
public sealed record CreateExpenseReductionGoalCommand(Guid OperationId, Guid? CategoryId, decimal ReferenceAmount,
    decimal TargetAmount, string Currency, DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd) : IFinanceMutation
{ public string Permission => Permissions.TransactionsWrite; }
public sealed record ArchiveExpenseReductionGoalCommand(Guid OperationId, Guid Id, long Version) : IFinanceMutation
{ public string Permission => Permissions.TransactionsWrite; }
public sealed record MarkNotificationReadCommand(Guid OperationId, Guid Id, long Version) : IFinanceMutation
{ public string Permission => Permissions.TransactionsRead; }
public sealed record ListBudgetsQuery(int Page = 1, int PageSize = 50) : IRequest<Result<Page<BudgetView>>>, IPageQuery;
public sealed record ListGoalsQuery(int Page = 1, int PageSize = 50) : IRequest<Result<Page<GoalView>>>, IPageQuery;
public sealed record ListExpenseReductionGoalsQuery(int Page = 1, int PageSize = 50) : IRequest<Result<Page<ExpenseReductionGoalView>>>, IPageQuery;
public sealed record ListNotificationsQuery(int Page = 1, int PageSize = 50) : IRequest<Result<Page<FinancialNotificationView>>>, IPageQuery;
public sealed record DashboardQuery(DateTimeOffset From, DateTimeOffset To) : IRequest<Result<DashboardView>>;
