using MediatR;
using RassAppControleFin.Api.Http;
using RassAppControleFin.Application.Features.Finance;
using RassAppControleFin.Contracts;
using RassAppControleFin.Security;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Api.Finance;

public static class FinanceEndpoints
{
    public static void MapFinance(this WebApplication app)
    {
        var finance = app.MapGroup("/api/v1").RequireAuthorization().RequireRateLimiting("public-api");
        finance.MapPost("/accounts", (OpenAccountRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new OpenAccountCommand(r.OperationId, r.Name, r.Currency, r.OpeningBalance), h, ct))
            .RequireAuthorization(Permissions.AccountsWrite);
        finance.MapPatch("/accounts/{id:guid}", (Guid id, MaintainResourceRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new MaintainResourceCommand(r.OperationId, "account", id, r.Version, r.Name, r.Archive), h, ct))
            .RequireAuthorization(Permissions.AccountsWrite);
        finance.MapGet("/accounts", (int page, int pageSize, ISender s, HttpContext h, CancellationToken ct)
            => Query(s, new ListAccountsQuery(page, pageSize), h, ct)).RequireAuthorization(Permissions.AccountsRead);

        finance.MapPost("/categories", (CreateCategoryRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new CreateCategoryCommand(r.OperationId, r.Name, r.Flow), h, ct)).RequireAuthorization(Permissions.CategoriesWrite);
        finance.MapPatch("/categories/{id:guid}", (Guid id, MaintainResourceRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new MaintainResourceCommand(r.OperationId, "category", id, r.Version, r.Name, r.Archive), h, ct))
            .RequireAuthorization(Permissions.CategoriesWrite);
        finance.MapGet("/categories", (int page, int pageSize, ISender s, HttpContext h, CancellationToken ct)
            => Query(s, new ListCategoriesQuery(page, pageSize), h, ct)).RequireAuthorization(Permissions.CategoriesRead);

        finance.MapPost("/credit-cards", (CreateCardRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new CreateCardCommand(r.OperationId, r.Name, r.Currency, r.Limit, r.ClosingDay, r.DueDay), h, ct))
            .RequireAuthorization(Permissions.CreditCardsWrite);
        finance.MapPatch("/credit-cards/{id:guid}", (Guid id, MaintainResourceRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new MaintainResourceCommand(r.OperationId, "card", id, r.Version, r.Name, r.Archive), h, ct))
            .RequireAuthorization(Permissions.CreditCardsWrite);
        finance.MapGet("/credit-cards", (int page, int pageSize, ISender s, HttpContext h, CancellationToken ct)
            => Query(s, new ListCardsQuery(page, pageSize), h, ct)).RequireAuthorization(Permissions.CreditCardsRead);
        finance.MapPost("/credit-card-movements", (CreateCardMovementRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new CardMovementCommand(r.OperationId, r.CardId, r.CardVersion, r.Amount, r.Currency, r.Kind,
                r.AccountId, r.AccountVersion, r.CategoryId, r.OccurredAt, r.Description), h, ct))
            .RequireAuthorization(Permissions.CreditCardsWrite);
        finance.MapGet("/credit-cards/{id:guid}/movements", (Guid id, int page, int pageSize, ISender s, HttpContext h, CancellationToken ct)
            => Query(s, new ListCardMovementsQuery(id, page, pageSize), h, ct)).RequireAuthorization(Permissions.CreditCardsRead);

        finance.MapPost("/transactions", (CreateTransactionRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new CreateTransactionCommand(r.OperationId, r.AccountId, r.CategoryId, r.Amount, r.Currency,
                r.Flow, r.Status, r.OccurredAt, r.Description, r.AccountVersion), h, ct)).RequireAuthorization(Permissions.TransactionsWrite);
        finance.MapPut("/transactions/{id:guid}", (Guid id, EditTransactionRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new EditTransactionCommand(r.OperationId, id, r.Version, r.CategoryId, r.Amount, r.Currency,
                r.Status, r.OccurredAt, r.Description, r.AccountVersion), h, ct)).RequireAuthorization(Permissions.TransactionsWrite);
        finance.MapPost("/transactions/{id:guid}/cancel", (Guid id, CancelTransactionRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new CancelTransactionCommand(r.OperationId, id, r.Version, r.AccountVersion), h, ct))
            .RequireAuthorization(Permissions.TransactionsWrite);
        finance.MapGet("/transactions", (string? search, Guid? accountId, Guid? categoryId, string? flow, string? status,
            DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, ISender s, HttpContext h, CancellationToken ct)
            => Query(s, new ListTransactionsQuery(search, accountId, categoryId, flow, status, from, to, page, pageSize), h, ct))
            .RequireAuthorization(Permissions.TransactionsRead);

        finance.MapPost("/transfers", (CreateTransferRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new TransferCommand(r.OperationId, r.SourceAccountId, r.DestinationAccountId, r.Amount,
                r.Currency, r.OccurredAt, r.Description, r.SourceVersion, r.DestinationVersion), h, ct))
            .RequireAuthorization(Permissions.TransfersWrite);
        finance.MapGet("/transfers", (int page, int pageSize, ISender s, HttpContext h, CancellationToken ct)
            => Query(s, new ListTransfersQuery(page, pageSize), h, ct)).RequireAuthorization(Permissions.TransactionsRead);

        finance.MapGet("/dashboard", (DateTimeOffset from, DateTimeOffset to, ISender s, HttpContext h, CancellationToken ct)
            => Dashboard(s, new DashboardQuery(from, to), h, ct)).RequireAuthorization(Permissions.TransactionsRead);
        finance.MapPost("/budgets", (CreateBudgetRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new CreateBudgetCommand(r.OperationId, r.CategoryId, r.Amount, r.Currency, r.PeriodStart, r.PeriodEnd), h, ct))
            .RequireAuthorization(Permissions.TransactionsWrite);
        finance.MapPost("/budgets/{id:guid}/archive", (Guid id, VersionedOperationRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new ArchiveBudgetCommand(r.OperationId, id, r.Version), h, ct)).RequireAuthorization(Permissions.TransactionsWrite);
        finance.MapGet("/budgets", (int page, int pageSize, ISender s, HttpContext h, CancellationToken ct)
            => Budgets(s, new ListBudgetsQuery(page, pageSize), h, ct)).RequireAuthorization(Permissions.TransactionsRead);
        finance.MapPost("/goals", (CreateGoalRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new CreateGoalCommand(r.OperationId, r.Name, r.TargetAmount, r.Currency, r.TargetDate), h, ct))
            .RequireAuthorization(Permissions.TransactionsWrite);
        finance.MapPost("/goals/{id:guid}/progress", (Guid id, AddGoalProgressRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new AddGoalProgressCommand(r.OperationId, id, r.Version, r.Amount, r.Currency), h, ct))
            .RequireAuthorization(Permissions.TransactionsWrite);
        finance.MapPost("/goals/{id:guid}/archive", (Guid id, VersionedOperationRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new ArchiveGoalCommand(r.OperationId, id, r.Version), h, ct)).RequireAuthorization(Permissions.TransactionsWrite);
        finance.MapGet("/goals", (int page, int pageSize, ISender s, HttpContext h, CancellationToken ct)
            => Goals(s, new ListGoalsQuery(page, pageSize), h, ct)).RequireAuthorization(Permissions.TransactionsRead);
    }

    private static async Task<IResult> Send(ISender sender, IFinanceMutation command, HttpContext context, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return (result.IsSuccess ? Result<MutationReceiptDto>.Success(ToDto(result.Value))
            : Result<MutationReceiptDto>.Failure(result.Error!)).ToHttp(context);
    }
    private static async Task<IResult> Query<T>(ISender sender, IRequest<Result<T>> query, HttpContext context, CancellationToken ct)
        => (await sender.Send(query, ct)).ToHttp(context);
    private static MutationReceiptDto ToDto(MutationReceipt x)
        => new(x.ResourceId, x.OperationId, x.Version, x.SourceVersion, x.DestinationVersion, x.SourceBalance, x.DestinationBalance);
    private static async Task<IResult> Dashboard(ISender sender, DashboardQuery query, HttpContext context, CancellationToken ct)
    {
        var result = await sender.Send(query, ct);
        return (result.IsSuccess ? Result<DashboardDto>.Success(new(result.Value.From, result.Value.To,
            result.Value.Metrics.Select(x => new DashboardMetricDto(x.Currency, x.NetWorth, x.Income, x.Expense,
                x.SavingsRate, x.BudgetUsed, x.ActiveGoals)).ToArray()))
            : Result<DashboardDto>.Failure(result.Error!)).ToHttp(context);
    }
    private static async Task<IResult> Budgets(ISender sender, ListBudgetsQuery query, HttpContext context, CancellationToken ct)
    {
        var result = await sender.Send(query, ct);
        return (result.IsSuccess ? Result<PageDto<BudgetDto>>.Success(new(result.Value.Items.Select(x =>
            new BudgetDto(x.Id, x.CategoryId, x.Amount, x.Spent, x.Currency, x.PeriodStart, x.PeriodEnd, x.Version, x.IsArchived)).ToArray(),
            result.Value.Total, result.Value.PageNumber, result.Value.PageSize)) : Result<PageDto<BudgetDto>>.Failure(result.Error!)).ToHttp(context);
    }
    private static async Task<IResult> Goals(ISender sender, ListGoalsQuery query, HttpContext context, CancellationToken ct)
    {
        var result = await sender.Send(query, ct);
        return (result.IsSuccess ? Result<PageDto<GoalDto>>.Success(new(result.Value.Items.Select(x =>
            new GoalDto(x.Id, x.Name, x.TargetAmount, x.CurrentAmount, x.Currency, x.TargetDate, x.Status, x.Version)).ToArray(),
            result.Value.Total, result.Value.PageNumber, result.Value.PageSize)) : Result<PageDto<GoalDto>>.Failure(result.Error!)).ToHttp(context);
    }
}
