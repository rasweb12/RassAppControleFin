using Microsoft.EntityFrameworkCore;
using RassAppControleFin.Application.Abstractions.Persistence;
using RassAppControleFin.Application.Features.Finance;
using RassAppControleFin.Domain.Entities.Finance;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Infrastructure.Persistence;

public sealed class FinanceReadStore(AppDbContext db, FinanceUnitFactory factory) : IFinanceReadStore
{
    private async Task<Result<T>> Read<T>(Func<Task<T>> query, CancellationToken ct)
    {
        var opened = await factory.BeginAsync(ct, true);
        if (!opened.IsSuccess) return Result<T>.Failure(opened.Error!);
        await using var scope = opened.Value;
        return Result<T>.Success(await query());
    }
    private static async Task<Page<T>> PageAsync<T>(IQueryable<T> query, int page, int size, CancellationToken ct)
        => new(await query.Skip((page - 1) * size).Take(size).ToListAsync(ct), await query.CountAsync(ct), page, size);
    public Task<Result<Page<AccountView>>> AccountsAsync(ListAccountsQuery q, CancellationToken ct) => Read(() => PageAsync(
        db.Accounts.AsNoTracking().OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Select(x => new AccountView(x.Id, x.Name, x.Currency, x.Balance, x.Version, x.IsArchived)), q.Page, q.PageSize, ct), ct);
    public Task<Result<Page<CategoryView>>> CategoriesAsync(ListCategoriesQuery q, CancellationToken ct) => Read(() => PageAsync(
        db.Categories.AsNoTracking().OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Select(x => new CategoryView(x.Id, x.Name, x.Flow.ToString(), x.Version, x.IsArchived)), q.Page, q.PageSize, ct), ct);
    public Task<Result<Page<CardView>>> CardsAsync(ListCardsQuery q, CancellationToken ct) => Read(() => PageAsync(
        db.CreditCards.AsNoTracking().OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Select(x => new CardView(x.Id, x.Name, x.Currency, x.Limit, x.Outstanding, x.ClosingDay, x.DueDay, x.Version, x.IsArchived)), q.Page, q.PageSize, ct), ct);
    public Task<Result<Page<TransferView>>> TransfersAsync(ListTransfersQuery q, CancellationToken ct) => Read(() => PageAsync(
        db.Transfers.AsNoTracking().OrderByDescending(x => x.OccurredAt).ThenBy(x => x.Id)
            .Select(x => new TransferView(x.Id, x.SourceAccountId, x.DestinationAccountId, x.Amount, x.Currency, x.OccurredAt, x.Description, x.Status)),
        q.Page, q.PageSize, ct), ct);
    public Task<Result<Page<CardMovementView>>> CardMovementsAsync(ListCardMovementsQuery q, CancellationToken ct) => Read(() => PageAsync(
        db.Set<CardMovementRow>().AsNoTracking().Where(x => x.CardId == q.CardId).OrderByDescending(x => x.OccurredAt).ThenBy(x => x.Id)
            .Select(x => new CardMovementView(x.Id, x.CardId, x.AccountId, x.CategoryId, x.Amount, x.Currency, x.Kind, x.OccurredAt, x.Description)),
        q.Page, q.PageSize, ct), ct);
    public Task<Result<TransactionPage>> TransactionsAsync(ListTransactionsQuery q, CancellationToken ct) => Read(async () =>
    {
        var rows = db.Transactions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q.Search)) rows = rows.Where(x => x.Description.Contains(q.Search.Trim()));
        if (q.AccountId is { } account) rows = rows.Where(x => x.AccountId == account);
        if (q.CategoryId is { } category) rows = rows.Where(x => x.CategoryId == category);
        if (q.Flow is { } flow) { var value = Enum.Parse<FinancialFlow>(flow); rows = rows.Where(x => x.Flow == value); }
        if (q.Status is { } status) { var value = Enum.Parse<TransactionStatus>(status); rows = rows.Where(x => x.Status == value); }
        if (q.From is { } from) rows = rows.Where(x => x.OccurredAt >= from);
        if (q.To is { } to) rows = rows.Where(x => x.OccurredAt < to);
        var page = await PageAsync(rows.OrderByDescending(x => x.OccurredAt).ThenBy(x => x.Id)
            .Select(x => new TransactionView(x.Id, x.AccountId, x.CategoryId, x.Amount, x.Currency, x.Flow.ToString(),
                x.Status.ToString(), x.OccurredAt, x.Description, x.Version)), q.Page, q.PageSize, ct);
        var summary = await rows.Where(x => x.Status == TransactionStatus.Posted).GroupBy(x => x.Currency)
            .Select(g => new TransactionSummary(g.Key, g.Sum(x => x.Flow == FinancialFlow.Income ? x.Amount : 0),
                g.Sum(x => x.Flow == FinancialFlow.Expense ? x.Amount : 0))).OrderBy(x => x.Currency).ToListAsync(ct);
        return new TransactionPage(page, summary);
    }, ct);
    public Task<Result<Page<BudgetView>>> BudgetsAsync(ListBudgetsQuery q, CancellationToken ct) => Read(async () =>
    {
        var now = DateTimeOffset.UtcNow;
        var budgets = db.Budgets.AsNoTracking().OrderByDescending(x => x.PeriodStart).ThenBy(x => x.Id);
        var rows = await budgets.Select(x => new BudgetView(x.Id, x.CategoryId, x.Amount,
            db.Transactions.Where(t => t.Status == TransactionStatus.Posted && t.Flow == FinancialFlow.Expense
                && t.OccurredAt >= x.PeriodStart && t.OccurredAt < x.PeriodEnd
                && (!x.CategoryId.HasValue || t.CategoryId == x.CategoryId)).Sum(t => (decimal?)t.Amount) ?? 0,
            x.Currency, x.PeriodStart, x.PeriodEnd, x.Version, x.IsArchived)).ToListAsync(ct);
        return new Page<BudgetView>(rows.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToList(), rows.Count, q.Page, q.PageSize);
    }, ct);
    public Task<Result<Page<GoalView>>> GoalsAsync(ListGoalsQuery q, CancellationToken ct) => Read(() => PageAsync(
        db.Goals.AsNoTracking().OrderBy(x => x.Status).ThenBy(x => x.TargetDate).ThenBy(x => x.Name)
            .Select(x => new GoalView(x.Id, x.Name, x.TargetAmount, x.CurrentAmount, x.Currency, x.TargetDate, x.Status, x.Version)),
        q.Page, q.PageSize, ct), ct);
    public Task<Result<Page<ExpenseReductionGoalView>>> ExpenseReductionGoalsAsync(ListExpenseReductionGoalsQuery q, CancellationToken ct) => Read(async () =>
    {
        var goals = db.ExpenseReductionGoals.AsNoTracking().OrderByDescending(x => x.PeriodStart).ThenBy(x => x.Id);
        var rows = await goals.Select(x => new ExpenseReductionGoalView(x.Id, x.CategoryId, x.ReferenceAmount, x.TargetAmount,
            db.Transactions.Where(t => t.Status == TransactionStatus.Posted && t.Flow == FinancialFlow.Expense
                && t.Currency == x.Currency && t.OccurredAt >= x.PeriodStart && t.OccurredAt < x.PeriodEnd
                && (!x.CategoryId.HasValue || t.CategoryId == x.CategoryId)).Sum(t => (decimal?)t.Amount) ?? 0,
            x.Currency, x.PeriodStart, x.PeriodEnd, x.Version, x.IsArchived)).ToListAsync(ct);
        return new Page<ExpenseReductionGoalView>(rows.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToList(), rows.Count, q.Page, q.PageSize);
    }, ct);
    public Task<Result<Page<FinancialNotificationView>>> NotificationsAsync(ListNotificationsQuery q, CancellationToken ct) => Read(() => PageAsync(
        db.FinancialNotifications.AsNoTracking().OrderBy(x => x.ReadAt != null).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
            .Select(x => new FinancialNotificationView(x.Id, x.Type, x.Title, x.Message, x.CreatedAt, x.ReadAt, x.Version)),
        q.Page, q.PageSize, ct), ct);
    public Task<Result<DashboardView>> DashboardAsync(DashboardQuery q, CancellationToken ct) => Read(async () =>
    {
        var balances = await db.Accounts.AsNoTracking().GroupBy(x => x.Currency)
            .Select(g => new { Currency = g.Key, NetWorth = g.Sum(x => x.Balance) }).ToListAsync(ct);
        var movements = await db.Transactions.AsNoTracking().Where(x => x.Status == TransactionStatus.Posted
            && x.OccurredAt >= q.From && x.OccurredAt < q.To).GroupBy(x => x.Currency)
            .Select(g => new { Currency = g.Key, Income = g.Sum(x => x.Flow == FinancialFlow.Income ? x.Amount : 0),
                Expense = g.Sum(x => x.Flow == FinancialFlow.Expense ? x.Amount : 0) }).ToListAsync(ct);
        var budgets = await db.Budgets.AsNoTracking().Where(x => !x.IsArchived && x.PeriodStart < q.To && x.PeriodEnd > q.From)
            .GroupBy(x => x.Currency).Select(g => new { Currency = g.Key, Amount = g.Sum(x => x.Amount) }).ToListAsync(ct);
        var activeGoals = await db.Goals.AsNoTracking().Where(x => x.Status == "Active").GroupBy(x => x.Currency)
            .Select(g => new { Currency = g.Key, Count = g.Count() }).ToListAsync(ct);
        var currencies = balances.Select(x => x.Currency).Union(movements.Select(x => x.Currency))
            .Union(budgets.Select(x => x.Currency)).Union(activeGoals.Select(x => x.Currency)).OrderBy(x => x);
        var metrics = currencies.Select(currency =>
        {
            var b = balances.SingleOrDefault(x => x.Currency == currency)?.NetWorth ?? 0;
            var m = movements.SingleOrDefault(x => x.Currency == currency);
            var income = m?.Income ?? 0; var expense = m?.Expense ?? 0;
            var budget = budgets.SingleOrDefault(x => x.Currency == currency)?.Amount ?? 0;
            var goals = activeGoals.SingleOrDefault(x => x.Currency == currency)?.Count ?? 0;
            return new DashboardMetric(currency, b, income, expense, income == 0 ? 0 : (income - expense) / income,
                budget == 0 ? 0 : expense / budget, goals);
        }).ToArray();
        return new DashboardView(q.From, q.To, metrics);
    }, ct);
}
