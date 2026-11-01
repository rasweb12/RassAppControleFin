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
            => Send(s, new OpenAccountCommand(r.OperationId, r.Name, r.Currency, r.OpeningBalance, r.OverdraftLimit,
                r.AccountType, r.Subtype, r.Institution, r.IncludeInNetWorth), h, ct))
            .RequireAuthorization(Permissions.AccountsWrite);
        finance.MapPatch("/accounts/{id:guid}", (Guid id, MaintainResourceRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new MaintainResourceCommand(r.OperationId, "account", id, r.Version, r.Name, r.Archive), h, ct))
            .RequireAuthorization(Permissions.AccountsWrite);
        finance.MapPut("/accounts/{id:guid}", (Guid id, UpdateAccountRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new UpdateAccountCommand(r.OperationId, id, r.Version, r.Name, r.OverdraftLimit,
                r.AccountType, r.Subtype, r.Institution, r.IncludeInNetWorth), h, ct))
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
            => Send(s, new CreateCardCommand(r.OperationId, r.Name, r.Currency, r.Limit, r.ClosingDay, r.DueDay,
                r.Institution, r.Brand, r.PreferredPaymentAccountId, r.AccentColor), h, ct))
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
        finance.MapGet("/credit-cards/{id:guid}/invoices", (Guid id, int page, int pageSize, ISender s, HttpContext h, CancellationToken ct)
            => Query(s, new ListCreditCardInvoicesQuery(id, page, pageSize), h, ct)).RequireAuthorization(Permissions.CreditCardsRead);
        finance.MapPost("/credit-card-purchases", (CreateCreditCardPurchaseRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new CreateCreditCardPurchaseCommand(r.OperationId, r.CardId, r.CardVersion, r.CategoryId,
                r.Amount, r.Currency, r.OccurredAt, r.Description, r.InstallmentCount), h, ct))
            .RequireAuthorization(Permissions.CreditCardsWrite);
        finance.MapPost("/credit-card-invoices/{id:guid}/payments", (Guid id, PayCreditCardInvoiceRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new PayCreditCardInvoiceCommand(r.OperationId, id, r.InvoiceVersion, r.AccountId,
                r.AccountVersion, r.Amount, r.Currency, r.OccurredAt, r.Description), h, ct))
            .RequireAuthorization(Permissions.CreditCardsWrite);

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

        finance.MapPost("/investments/pay-yourself-first", (ConfigurePayYourselfFirstPlanRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new ConfigurePayYourselfFirstPlanCommand(r.OperationId, r.Version, r.CurrentRate, r.GoalRate, r.EffectiveFrom), h, ct))
            .RequireAuthorization(Permissions.TransactionsWrite);
        finance.MapGet("/investments/pay-yourself-first", (ISender s, HttpContext h, CancellationToken ct)
            => PayYourselfFirst(s, h, ct)).RequireAuthorization(Permissions.TransactionsRead);
        finance.MapPost("/investments/emergency-fund", (ConfigureEmergencyFundPlanRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new ConfigureEmergencyFundPlanCommand(r.OperationId, r.Version, r.TargetType, r.TargetMultiple,
                r.TargetAmount, r.CurrentAmount, r.MonthlyContributionTarget, r.Currency, r.LiquidityRequired, r.Notes), h, ct))
            .RequireAuthorization(Permissions.TransactionsWrite);
        finance.MapGet("/investments/emergency-fund", (ISender s, HttpContext h, CancellationToken ct)
            => EmergencyFund(s, h, ct)).RequireAuthorization(Permissions.TransactionsRead);
        finance.MapPost("/investment-assets", (CreateInvestmentAssetRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new CreateInvestmentAssetCommand(r.OperationId, r.Name, r.Ticker, r.AssetClass, r.AssetSubClass,
                r.AccountId, r.Issuer, r.Indexer, r.Rate, r.MaturityDate, r.LiquidityType, r.FgcProtected, r.TaxExempt, r.Notes), h, ct))
            .RequireAuthorization(Permissions.TransactionsWrite);
        finance.MapPut("/investment-assets/{id:guid}", (Guid id, UpdateInvestmentAssetRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new UpdateInvestmentAssetCommand(r.OperationId, id, r.Version, r.Name, r.Ticker, r.AssetClass,
                r.AssetSubClass, r.AccountId, r.Issuer, r.Indexer, r.Rate, r.MaturityDate, r.LiquidityType, r.FgcProtected,
                r.TaxExempt, r.Notes, r.Active), h, ct)).RequireAuthorization(Permissions.TransactionsWrite);
        // includeInactive is an optional filter. Keep it nullable at the HTTP
        // boundary so a normal paged sync request need not send a synthetic false.
        finance.MapGet("/investment-assets", (bool? includeInactive, int page, int pageSize, ISender s, HttpContext h, CancellationToken ct)
            => InvestmentAssets(s, new ListInvestmentAssetsQuery(includeInactive ?? false, page, pageSize), h, ct))
            .RequireAuthorization(Permissions.TransactionsRead);
        finance.MapPost("/investment-transactions", (RecordInvestmentTransactionRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new RecordInvestmentTransactionCommand(r.OperationId, r.InvestmentAssetId, r.Date, r.Type, r.Quantity,
                r.UnitPrice, r.GrossAmount, r.Fees, r.Taxes, r.Currency, r.Notes), h, ct)).RequireAuthorization(Permissions.TransactionsWrite);
        finance.MapGet("/investment-transactions", (Guid? investmentAssetId, int page, int pageSize, ISender s, HttpContext h, CancellationToken ct)
            => InvestmentTransactions(s, new ListInvestmentTransactionsQuery(investmentAssetId, page, pageSize), h, ct))
            .RequireAuthorization(Permissions.TransactionsRead);
        finance.MapPut("/investments/target-portfolio", (SetTargetPortfolioRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new SetTargetPortfolioCommand(r.OperationId, r.Allocations.Select(x => new TargetPortfolioAllocationInput(
                x.Id, x.Version, x.InvestmentAssetId, x.Key, x.AssetClass, x.Weight)).ToArray()), h, ct))
            .RequireAuthorization(Permissions.TransactionsWrite);
        finance.MapGet("/investments/target-portfolio", (ISender s, HttpContext h, CancellationToken ct)
            => TargetPortfolio(s, h, ct)).RequireAuthorization(Permissions.TransactionsRead);

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
        finance.MapPost("/goals/{id:guid}/archive", (Guid id, ArchiveResourceRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new ArchiveGoalCommand(r.OperationId, id, r.Version, r.Archive), h, ct)).RequireAuthorization(Permissions.TransactionsWrite);
        finance.MapGet("/goals", (int page, int pageSize, ISender s, HttpContext h, CancellationToken ct)
            => Goals(s, new ListGoalsQuery(page, pageSize), h, ct)).RequireAuthorization(Permissions.TransactionsRead);
        finance.MapPost("/expense-reduction-goals", (CreateExpenseReductionGoalRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new CreateExpenseReductionGoalCommand(r.OperationId, r.CategoryId, r.ReferenceAmount, r.TargetAmount,
                r.Currency, r.PeriodStart, r.PeriodEnd), h, ct)).RequireAuthorization(Permissions.TransactionsWrite);
        finance.MapPost("/expense-reduction-goals/{id:guid}/archive", (Guid id, ArchiveResourceRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new ArchiveExpenseReductionGoalCommand(r.OperationId, id, r.Version, r.Archive), h, ct)).RequireAuthorization(Permissions.TransactionsWrite);
        finance.MapGet("/expense-reduction-goals", (int page, int pageSize, ISender s, HttpContext h, CancellationToken ct)
            => ExpenseReductionGoals(s, new ListExpenseReductionGoalsQuery(page, pageSize), h, ct)).RequireAuthorization(Permissions.TransactionsRead);
        finance.MapGet("/notifications", (int page, int pageSize, ISender s, HttpContext h, CancellationToken ct)
            => Notifications(s, new ListNotificationsQuery(page, pageSize), h, ct)).RequireAuthorization(Permissions.TransactionsRead);
        finance.MapPost("/notifications/{id:guid}/read", (Guid id, VersionedOperationRequest r, ISender s, HttpContext h, CancellationToken ct)
            => Send(s, new MarkNotificationReadCommand(r.OperationId, id, r.Version), h, ct)).RequireAuthorization(Permissions.TransactionsRead);
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
    private static async Task<IResult> PayYourselfFirst(ISender sender, HttpContext context, CancellationToken ct)
    {
        var result = await sender.Send(new GetPayYourselfFirstPlanQuery(), ct);
        return (result.IsSuccess ? Result<PayYourselfFirstPlanDto?>.SuccessOptional(result.Value is null ? null : new(
            result.Value.Id, result.Value.CurrentRate, result.Value.GoalRate, result.Value.EffectiveFrom, result.Value.Version))
            : Result<PayYourselfFirstPlanDto?>.Failure(result.Error!)).ToHttp(context);
    }
    private static async Task<IResult> EmergencyFund(ISender sender, HttpContext context, CancellationToken ct)
    {
        var result = await sender.Send(new GetEmergencyFundPlanQuery(), ct);
        return (result.IsSuccess ? Result<EmergencyFundPlanDto?>.SuccessOptional(result.Value is null ? null : new(result.Value.Id,
            result.Value.TargetType, result.Value.TargetMultiple, result.Value.TargetAmount, result.Value.CurrentAmount,
            result.Value.MonthlyContributionTarget, result.Value.Currency, result.Value.LiquidityRequired, result.Value.Notes,
            result.Value.Version)) : Result<EmergencyFundPlanDto?>.Failure(result.Error!)).ToHttp(context);
    }
    private static async Task<IResult> InvestmentAssets(ISender sender, ListInvestmentAssetsQuery query, HttpContext context, CancellationToken ct)
    {
        var result = await sender.Send(query, ct);
        return (result.IsSuccess ? Result<PageDto<InvestmentAssetDto>>.Success(new(result.Value.Items.Select(x => new InvestmentAssetDto(
            x.Id, x.Name, x.Ticker, x.AssetClass, x.AssetSubClass, x.AccountId, x.Issuer, x.Indexer, x.Rate, x.MaturityDate,
            x.LiquidityType, x.FgcProtected, x.TaxExempt, x.Active, x.Notes, x.UpdatedAt, x.Version)).ToArray(), result.Value.Total,
            result.Value.PageNumber, result.Value.PageSize)) : Result<PageDto<InvestmentAssetDto>>.Failure(result.Error!)).ToHttp(context);
    }
    private static async Task<IResult> InvestmentTransactions(ISender sender, ListInvestmentTransactionsQuery query, HttpContext context, CancellationToken ct)
    {
        var result = await sender.Send(query, ct);
        return (result.IsSuccess ? Result<PageDto<InvestmentTransactionDto>>.Success(new(result.Value.Items.Select(x => new InvestmentTransactionDto(
            x.Id, x.InvestmentAssetId, x.Date, x.Type, x.Quantity, x.UnitPrice, x.GrossAmount, x.Fees, x.Taxes, x.NetAmount,
            x.Currency, x.Notes, x.Version)).ToArray(), result.Value.Total, result.Value.PageNumber, result.Value.PageSize))
            : Result<PageDto<InvestmentTransactionDto>>.Failure(result.Error!)).ToHttp(context);
    }
    private static async Task<IResult> TargetPortfolio(ISender sender, HttpContext context, CancellationToken ct)
    {
        var result = await sender.Send(new ListTargetPortfolioQuery(), ct);
        return (result.IsSuccess ? Result<IReadOnlyList<TargetPortfolioAllocationDto>>.Success(result.Value.Select(x => new TargetPortfolioAllocationDto(
            x.Id, x.InvestmentAssetId, x.Key, x.AssetClass, x.Weight, x.Version)).ToArray())
            : Result<IReadOnlyList<TargetPortfolioAllocationDto>>.Failure(result.Error!)).ToHttp(context);
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
    private static async Task<IResult> ExpenseReductionGoals(ISender sender, ListExpenseReductionGoalsQuery query, HttpContext context, CancellationToken ct)
    {
        var result = await sender.Send(query, ct);
        return (result.IsSuccess ? Result<PageDto<ExpenseReductionGoalDto>>.Success(new(result.Value.Items.Select(x =>
            new ExpenseReductionGoalDto(x.Id, x.CategoryId, x.ReferenceAmount, x.TargetAmount, x.ActualExpense, x.Currency,
                x.PeriodStart, x.PeriodEnd, x.Version, x.IsArchived)).ToArray(), result.Value.Total, result.Value.PageNumber,
            result.Value.PageSize)) : Result<PageDto<ExpenseReductionGoalDto>>.Failure(result.Error!)).ToHttp(context);
    }
    private static async Task<IResult> Notifications(ISender sender, ListNotificationsQuery query, HttpContext context, CancellationToken ct)
    {
        var result = await sender.Send(query, ct);
        return (result.IsSuccess ? Result<PageDto<FinancialNotificationDto>>.Success(new(result.Value.Items.Select(x =>
            new FinancialNotificationDto(x.Id, x.Type, x.Title, x.Message, x.CreatedAt, x.ReadAt, x.Version)).ToArray(),
            result.Value.Total, result.Value.PageNumber, result.Value.PageSize))
            : Result<PageDto<FinancialNotificationDto>>.Failure(result.Error!)).ToHttp(context);
    }
}
