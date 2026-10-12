using FluentValidation;
using MediatR;
using RassAppControleFin.Application.Abstractions.Persistence;
using RassAppControleFin.MultiTenancy;
using RassAppControleFin.Security;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Application.Features.Finance;

public sealed class FinanceQueryHandlers(IFinanceReadStore store, IUserContext user, ITenantContext tenant) :
    IRequestHandler<ListAccountsQuery, Result<Page<AccountView>>>,
    IRequestHandler<ListCategoriesQuery, Result<Page<CategoryView>>>,
    IRequestHandler<ListCardsQuery, Result<Page<CardView>>>,
    IRequestHandler<ListTransfersQuery, Result<Page<TransferView>>>,
    IRequestHandler<ListTransactionsQuery, Result<TransactionPage>>,
    IRequestHandler<ListCardMovementsQuery, Result<Page<CardMovementView>>>,
    IRequestHandler<ListBudgetsQuery, Result<Page<BudgetView>>>,
    IRequestHandler<ListGoalsQuery, Result<Page<GoalView>>>,
    IRequestHandler<DashboardQuery, Result<DashboardView>>
{
    private Task<Result<T>> Read<T>(string permission, Func<Task<Result<T>>> read)
        => user.IsAuthenticated && !string.IsNullOrWhiteSpace(user.Subject) && tenant.IsResolved
            && user.Permissions.Contains(permission) ? read()
            : Task.FromResult(Result<T>.Failure(new("FINANCE_FORBIDDEN", "Consulta nao autorizada.", ErrorType.Forbidden)));
    public Task<Result<Page<AccountView>>> Handle(ListAccountsQuery q, CancellationToken ct)
        => Read(Permissions.AccountsRead, () => store.AccountsAsync(q, ct));
    public Task<Result<Page<CategoryView>>> Handle(ListCategoriesQuery q, CancellationToken ct)
        => Read(Permissions.CategoriesRead, () => store.CategoriesAsync(q, ct));
    public Task<Result<Page<CardView>>> Handle(ListCardsQuery q, CancellationToken ct)
        => Read(Permissions.CreditCardsRead, () => store.CardsAsync(q, ct));
    public Task<Result<Page<TransferView>>> Handle(ListTransfersQuery q, CancellationToken ct)
        => Read(Permissions.TransactionsRead, () => store.TransfersAsync(q, ct));
    public Task<Result<TransactionPage>> Handle(ListTransactionsQuery q, CancellationToken ct)
        => Read(Permissions.TransactionsRead, () => store.TransactionsAsync(q, ct));
    public Task<Result<Page<CardMovementView>>> Handle(ListCardMovementsQuery q, CancellationToken ct)
        => Read(Permissions.CreditCardsRead, () => store.CardMovementsAsync(q, ct));
    public Task<Result<Page<BudgetView>>> Handle(ListBudgetsQuery q, CancellationToken ct)
        => Read(Permissions.TransactionsRead, () => store.BudgetsAsync(q, ct));
    public Task<Result<Page<GoalView>>> Handle(ListGoalsQuery q, CancellationToken ct)
        => Read(Permissions.TransactionsRead, () => store.GoalsAsync(q, ct));
    public Task<Result<DashboardView>> Handle(DashboardQuery q, CancellationToken ct)
        => Read(Permissions.TransactionsRead, () => store.DashboardAsync(q, ct));
}

public abstract class PageValidator<T> : AbstractValidator<T> where T : IPageQuery
{
    protected PageValidator()
    { RuleFor(x => x.Page).InclusiveBetween(1, 10000); RuleFor(x => x.PageSize).InclusiveBetween(1, 100); }
}
public sealed class ListAccountsValidator : PageValidator<ListAccountsQuery>;
public sealed class ListCategoriesValidator : PageValidator<ListCategoriesQuery>;
public sealed class ListCardsValidator : PageValidator<ListCardsQuery>;
public sealed class ListTransfersValidator : PageValidator<ListTransfersQuery>;
public sealed class ListCardMovementsValidator : PageValidator<ListCardMovementsQuery>
{ public ListCardMovementsValidator() => RuleFor(x => x.CardId).NotEmpty(); }
public sealed class ListTransactionsValidator : PageValidator<ListTransactionsQuery>
{
    public ListTransactionsValidator()
    {
        RuleFor(x => x.Search).MaximumLength(120);
        RuleFor(x => x.AccountId).NotEqual(Guid.Empty);
        RuleFor(x => x.CategoryId).NotEqual(Guid.Empty);
        RuleFor(x => x.Flow).Must(x => x is null or "Income" or "Expense");
        RuleFor(x => x.Status).Must(x => x is null or "Pending" or "Posted" or "Cancelled");
        RuleFor(x => x.From).Must(x => x is null || x.Value.Offset == TimeSpan.Zero);
        RuleFor(x => x.To).Must(x => x is null || x.Value.Offset == TimeSpan.Zero);
        RuleFor(x => x).Must(x => x.From is null || x.To is null || x.From < x.To)
            .WithMessage("O inicio deve ser anterior ao fim exclusivo do periodo.");
    }
}
public sealed class ListBudgetsValidator : PageValidator<ListBudgetsQuery>;
public sealed class ListGoalsValidator : PageValidator<ListGoalsQuery>;
public sealed class DashboardValidator : AbstractValidator<DashboardQuery>
{
    public DashboardValidator()
    {
        RuleFor(x => x.From).Must(x => x.Offset == TimeSpan.Zero);
        RuleFor(x => x.To).Must(x => x.Offset == TimeSpan.Zero);
        RuleFor(x => x).Must(x => x.From < x.To && x.To - x.From <= TimeSpan.FromDays(366));
    }
}
