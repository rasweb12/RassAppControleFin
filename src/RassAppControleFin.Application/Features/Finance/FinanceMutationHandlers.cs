using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using MediatR;
using RassAppControleFin.Application.Abstractions.Persistence;
using RassAppControleFin.Domain.Entities.Finance;
using RassAppControleFin.Domain.ValueObjects;
using RassAppControleFin.MultiTenancy;
using RassAppControleFin.Security;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Application.Features.Finance;

public sealed class FinanceMutationHandlers(IFinanceUnitFactory factory, IUserContext user, ITenantContext tenant) :
    IRequestHandler<OpenAccountCommand, Result<MutationReceipt>>,
    IRequestHandler<MaintainResourceCommand, Result<MutationReceipt>>,
    IRequestHandler<CreateCategoryCommand, Result<MutationReceipt>>,
    IRequestHandler<CreateCardCommand, Result<MutationReceipt>>,
    IRequestHandler<CreateTransactionCommand, Result<MutationReceipt>>,
    IRequestHandler<EditTransactionCommand, Result<MutationReceipt>>,
    IRequestHandler<CancelTransactionCommand, Result<MutationReceipt>>,
    IRequestHandler<TransferCommand, Result<MutationReceipt>>,
    IRequestHandler<CardMovementCommand, Result<MutationReceipt>>,
    IRequestHandler<CreateBudgetCommand, Result<MutationReceipt>>,
    IRequestHandler<ArchiveBudgetCommand, Result<MutationReceipt>>,
    IRequestHandler<CreateGoalCommand, Result<MutationReceipt>>,
    IRequestHandler<AddGoalProgressCommand, Result<MutationReceipt>>,
    IRequestHandler<ArchiveGoalCommand, Result<MutationReceipt>>
    , IRequestHandler<CreateExpenseReductionGoalCommand, Result<MutationReceipt>>
    , IRequestHandler<ArchiveExpenseReductionGoalCommand, Result<MutationReceipt>>
    , IRequestHandler<MarkNotificationReadCommand, Result<MutationReceipt>>
{
    private Guid TenantId => tenant.TenantId!.Value;
    private string Subject => user.Subject!;
    private static Result<MutationReceipt> Fail(string code, string message, ErrorType type = ErrorType.Domain)
        => Result<MutationReceipt>.Failure(new(code, message, type));
    private static Result<MutationReceipt> Missing() => Fail("RESOURCE_NOT_FOUND", "Recurso nao encontrado.", ErrorType.NotFound);
    private static Result<MutationReceipt> Conflict() => Fail("VERSION_CONFLICT", "O recurso foi alterado. Atualize os dados.", ErrorType.Conflict);
    private static bool ValidAccount(Account account, long version, string currency)
        => !account.IsArchived && account.Version == version && account.Currency == currency;
    private static bool ValidCategory(Category? category, FinancialFlow flow) => category is { IsArchived: false } && category.Flow == flow;

    private async Task<Result<MutationReceipt>> Execute(IFinanceMutation command,
        Func<IFinanceUnit, Task<Result<MutationReceipt>>> change, CancellationToken ct, string? additionalPermission = null)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.Subject))
            return Fail("IDENTITY_REQUIRED", "Autenticacao necessaria.", ErrorType.Unauthorized);
        if (!tenant.IsResolved || tenant.TenantId is null || !user.Permissions.Contains(command.Permission)
            || (additionalPermission is not null && !user.Permissions.Contains(additionalPermission)))
            return Fail("FINANCE_FORBIDDEN", "Operacao nao autorizada.", ErrorType.Forbidden);
        var started = await factory.BeginAsync(ct);
        if (!started.IsSuccess) return Result<MutationReceipt>.Failure(started.Error!);
        await using var unit = started.Value;
        var hash = FinancePayloadHash.Create(command);
        Activity.Current?.SetTag("app.operation_id", command.OperationId);
        var claim = await unit.ClaimAsync(command.OperationId, hash, ct);
        if (!claim.IsSuccess) return Result<MutationReceipt>.Failure(claim.Error!);
        if (claim.Value.Exists)
        {
            var previous = JsonSerializer.Deserialize<MutationReceipt>(claim.Value.Json!);
            return previous is null ? Fail("RECEIPT_INVALID", "Recibo indisponivel.", ErrorType.Infrastructure)
                : Result<MutationReceipt>.Success(previous);
        }
        var result = await change(unit);
        if (!result.IsSuccess) return result;
        var committed = await unit.CommitAsync(command.OperationId, hash, result.Value, command.GetType().Name, ct);
        return committed.IsSuccess ? result : Result<MutationReceipt>.Failure(committed.Error!);
    }

    public Task<Result<MutationReceipt>> Handle(OpenAccountCommand c, CancellationToken ct) => Execute(c, unit =>
    {
        var account = Account.Open(Guid.NewGuid(), TenantId, Subject, c.Name, Money.Create(c.OpeningBalance, c.Currency, true), DateTimeOffset.UtcNow, c.OverdraftLimit);
        unit.Add(account);
        if (c.OpeningBalance != 0) unit.Ledger(account.Id, c.OpeningBalance, "Opening", account.Id);
        return Task.FromResult(Result<MutationReceipt>.Success(new(account.Id, c.OperationId, account.Version)));
    }, ct);

    public Task<Result<MutationReceipt>> Handle(CreateCategoryCommand c, CancellationToken ct) => Execute(c, unit =>
    {
        var category = Category.Create(Guid.NewGuid(), TenantId, Subject, c.Name, Enum.Parse<FinancialFlow>(c.Flow), DateTimeOffset.UtcNow);
        unit.Add(category);
        return Task.FromResult(Result<MutationReceipt>.Success(new(category.Id, c.OperationId, category.Version)));
    }, ct);

    public Task<Result<MutationReceipt>> Handle(CreateCardCommand c, CancellationToken ct) => Execute(c, unit =>
    {
        var card = CreditCard.Create(Guid.NewGuid(), TenantId, Subject, c.Name, Money.Create(c.Limit, c.Currency), c.ClosingDay, c.DueDay, DateTimeOffset.UtcNow);
        unit.Add(card);
        return Task.FromResult(Result<MutationReceipt>.Success(new(card.Id, c.OperationId, card.Version)));
    }, ct);

    public Task<Result<MutationReceipt>> Handle(MaintainResourceCommand c, CancellationToken ct) => Execute(c, async unit =>
    {
        FinancialAggregate? resource = c.Resource switch
        {
            "account" => await unit.AccountAsync(c.Id, ct),
            "category" => await unit.CategoryAsync(c.Id, ct),
            "card" => await unit.CardAsync(c.Id, ct),
            _ => null
        };
        if (resource is null) return Missing();
        if (resource.Version != c.Version) return Conflict();
        switch (resource)
        {
            case Account account:
                if (account.IsArchived || (c.Archive && !account.Archive())) return Fail("ACCOUNT_STATE_INVALID", "Conta arquivada ou com saldo.");
                if (!c.Archive) account.Rename(c.Name);
                break;
            case Category category:
                if (category.IsArchived) return Fail("CATEGORY_ARCHIVED", "Categoria arquivada.");
                if (c.Archive) category.Archive(); else category.Rename(c.Name);
                break;
            case CreditCard card:
                if (card.IsArchived || (c.Archive && !card.Archive())) return Fail("CARD_STATE_INVALID", "Cartao arquivado ou com saldo devedor.");
                if (!c.Archive) card.Rename(c.Name);
                break;
        }
        return Result<MutationReceipt>.Success(new(resource.Id, c.OperationId, resource.Version));
    }, ct);

    public Task<Result<MutationReceipt>> Handle(CreateTransactionCommand c, CancellationToken ct) => Execute(c, async unit =>
    {
        var account = await unit.AccountAsync(c.AccountId, ct);
        var category = await unit.CategoryAsync(c.CategoryId, ct);
        if (account is null || category is null) return Missing();
        if (account.Version != c.AccountVersion) return Conflict();
        var flow = Enum.Parse<FinancialFlow>(c.Flow);
        if (!ValidAccount(account, c.AccountVersion, c.Currency) || !ValidCategory(category, flow))
            return Fail("TRANSACTION_SCOPE_INVALID", "Conta, moeda ou categoria invalida.");
        var transaction = FinancialTransaction.Create(Guid.NewGuid(), TenantId, Subject, account.Id, category.Id,
            Money.Create(c.Amount, c.Currency), flow, Enum.Parse<TransactionStatus>(c.Status), c.OccurredAt, c.Description, DateTimeOffset.UtcNow);
        if (transaction.PostedDelta != 0)
        {
            if (!account.TryApply(transaction.PostedDelta)) return Fail("BALANCE_INVALID", "Saldo insuficiente ou limite excedido.");
            unit.Ledger(account.Id, transaction.PostedDelta, "Transaction", transaction.Id);
        }
        unit.Add(transaction);
        if (flow == FinancialFlow.Expense && transaction.Status == TransactionStatus.Posted)
            await CreateBudgetNotificationsAsync(unit, transaction, ct);
        return Result<MutationReceipt>.Success(new(transaction.Id, c.OperationId, transaction.Version, account.Version, SourceBalance: account.Balance));
    }, ct, Permissions.AccountsWrite);

    private async Task CreateBudgetNotificationsAsync(IFinanceUnit unit, FinancialTransaction transaction,
        CancellationToken ct)
    {
        var alerts = await unit.BudgetAlertsAsync(transaction.CategoryId, transaction.Currency, transaction.OccurredAt,
            transaction.Amount, ct);
        foreach (var alert in alerts)
        {
            var type = $"budget.{alert.BudgetId:N}.{alert.Threshold}";
            if (await unit.NotificationExistsAsync(type, ct)) continue;
            unit.Add(FinancialNotification.Create(Guid.NewGuid(), TenantId, Subject, type, "Orcamento em alerta",
                $"Seus gastos atingiram {alert.Threshold}% do orcamento de {alert.Amount:0.00} {transaction.Currency}.",
                DateTimeOffset.UtcNow));
        }
    }

    public Task<Result<MutationReceipt>> Handle(EditTransactionCommand c, CancellationToken ct) => Execute(c, async unit =>
    {
        var transaction = await unit.TransactionAsync(c.Id, ct);
        if (transaction is null) return Missing();
        var account = await unit.AccountAsync(transaction.AccountId, ct);
        var category = await unit.CategoryAsync(c.CategoryId, ct);
        if (account is null || category is null) return Missing();
        if (transaction.Version != c.Version || account.Version != c.AccountVersion) return Conflict();
        if (transaction.Status == TransactionStatus.Cancelled || !ValidAccount(account, c.AccountVersion, c.Currency)
            || !ValidCategory(category, transaction.Flow)) return Fail("TRANSACTION_STATE_INVALID", "Transacao nao pode ser alterada.");
        var oldDelta = transaction.PostedDelta;
        transaction.Edit(category.Id, Money.Create(c.Amount, c.Currency), Enum.Parse<TransactionStatus>(c.Status), c.OccurredAt, c.Description);
        var delta = transaction.PostedDelta - oldDelta;
        if (delta != 0)
        {
            if (!account.TryApply(delta)) return Fail("BALANCE_INVALID", "Saldo insuficiente ou limite excedido.");
            unit.Ledger(account.Id, delta, "Adjustment", transaction.Id);
        }
        return Result<MutationReceipt>.Success(new(transaction.Id, c.OperationId, transaction.Version, account.Version, SourceBalance: account.Balance));
    }, ct, Permissions.AccountsWrite);

    public Task<Result<MutationReceipt>> Handle(CancelTransactionCommand c, CancellationToken ct) => Execute(c, async unit =>
    {
        var transaction = await unit.TransactionAsync(c.Id, ct);
        if (transaction is null) return Missing();
        var account = await unit.AccountAsync(transaction.AccountId, ct);
        if (account is null) return Missing();
        if (transaction.Version != c.Version || account.Version != c.AccountVersion) return Conflict();
        if (transaction.Status == TransactionStatus.Cancelled || account.IsArchived)
            return Fail("TRANSACTION_STATE_INVALID", "Transacao nao pode ser cancelada.");
        var reversal = -transaction.PostedDelta;
        if (reversal != 0)
        {
            if (!account.TryApply(reversal)) return Fail("BALANCE_INVALID", "Saldo insuficiente para o estorno.");
            unit.Ledger(account.Id, reversal, "Reversal", transaction.Id);
        }
        transaction.Cancel();
        return Result<MutationReceipt>.Success(new(transaction.Id, c.OperationId, transaction.Version, account.Version, SourceBalance: account.Balance));
    }, ct, Permissions.AccountsWrite);

    public Task<Result<MutationReceipt>> Handle(TransferCommand c, CancellationToken ct) => Execute(c, async unit =>
    {
        var source = await unit.AccountAsync(c.SourceAccountId, ct);
        var destination = await unit.AccountAsync(c.DestinationAccountId, ct);
        if (source is null || destination is null) return Missing();
        if (source.Version != c.SourceVersion || destination.Version != c.DestinationVersion) return Conflict();
        if (!ValidAccount(source, c.SourceVersion, c.Currency) || !ValidAccount(destination, c.DestinationVersion, c.Currency))
            return Fail("TRANSFER_SCOPE_INVALID", "Contas devem estar ativas e usar a mesma moeda.");
        if (!source.TryApply(-c.Amount) || !destination.TryApply(c.Amount))
            return Fail("BALANCE_INVALID", "Saldo insuficiente ou limite excedido.");
        var transfer = Transfer.Create(Guid.NewGuid(), TenantId, Subject, source.Id, destination.Id,
            Money.Create(c.Amount, c.Currency), c.OccurredAt, c.Description, c.OperationId, DateTimeOffset.UtcNow);
        unit.Add(transfer);
        unit.Ledger(source.Id, -c.Amount, "Transfer", transfer.Id, transfer.Id);
        unit.Ledger(destination.Id, c.Amount, "Transfer", transfer.Id, transfer.Id);
        return Result<MutationReceipt>.Success(new(transfer.Id, c.OperationId, transfer.Version,
            source.Version, destination.Version, source.Balance, destination.Balance));
    }, ct, Permissions.AccountsWrite);

    public Task<Result<MutationReceipt>> Handle(CardMovementCommand c, CancellationToken ct) => Execute(c, async unit =>
    {
        var card = await unit.CardAsync(c.CardId, ct);
        if (card is null) return Missing();
        if (card.Version != c.CardVersion) return Conflict();
        if (card.Currency != c.Currency || card.IsArchived) return Fail("CARD_STATE_INVALID", "Cartao ou moeda invalida.");
        var money = Money.Create(c.Amount, c.Currency);
        Account? account = null;
        if (c.Kind == "Purchase")
        {
            if (!ValidCategory(await unit.CategoryAsync(c.CategoryId!.Value, ct), FinancialFlow.Expense))
                return Fail("CATEGORY_INVALID", "Categoria de despesa ativa necessaria.");
            if (!card.TryCharge(money)) return Fail("CARD_LIMIT_EXCEEDED", "Limite do cartao excedido.");
        }
        else
        {
            account = await unit.AccountAsync(c.AccountId!.Value, ct);
            if (account is null) return Missing();
            if (account.Version != c.AccountVersion) return Conflict();
            if (!ValidAccount(account, c.AccountVersion!.Value, c.Currency) || !account.TryApply(-c.Amount) || !card.TryPay(money))
                return Fail("CARD_PAYMENT_INVALID", "Saldo ou pagamento invalido.");
        }
        var id = Guid.NewGuid();
        unit.CardMovement(card.Id, account?.Id, c.CategoryId, c.Amount, c.Currency, c.Kind, c.OccurredAt, c.Description.Trim(), id);
        if (account is not null) unit.Ledger(account.Id, -c.Amount, "CardPayment", id);
        return Result<MutationReceipt>.Success(new(id, c.OperationId, card.Version, account?.Version, SourceBalance: account?.Balance));
    }, ct, c.Kind == "Payment" ? Permissions.AccountsWrite : null);

    public Task<Result<MutationReceipt>> Handle(CreateBudgetCommand c, CancellationToken ct) => Execute(c, async unit =>
    {
        if (c.CategoryId is { } categoryId)
        {
            var category = await unit.CategoryAsync(categoryId, ct);
            if (category is null) return Missing();
            if (category.IsArchived || category.Flow != FinancialFlow.Expense)
                return Fail("BUDGET_CATEGORY_INVALID", "Orcamento requer categoria de despesa ativa.");
        }
        var budget = Budget.Create(Guid.NewGuid(), TenantId, Subject, c.CategoryId, Money.Create(c.Amount, c.Currency),
            c.PeriodStart, c.PeriodEnd, DateTimeOffset.UtcNow);
        unit.Add(budget);
        return Result<MutationReceipt>.Success(new(budget.Id, c.OperationId, budget.Version));
    }, ct);

    public Task<Result<MutationReceipt>> Handle(ArchiveBudgetCommand c, CancellationToken ct) => Execute(c, async unit =>
    {
        var budget = await unit.BudgetAsync(c.Id, ct);
        if (budget is null) return Missing();
        if (budget.Version != c.Version) return Conflict();
        budget.Archive();
        return Result<MutationReceipt>.Success(new(budget.Id, c.OperationId, budget.Version));
    }, ct);

    public Task<Result<MutationReceipt>> Handle(CreateGoalCommand c, CancellationToken ct) => Execute(c, unit =>
    {
        var goal = Goal.Create(Guid.NewGuid(), TenantId, Subject, c.Name, Money.Create(c.TargetAmount, c.Currency),
            c.TargetDate, DateTimeOffset.UtcNow);
        unit.Add(goal);
        return Task.FromResult(Result<MutationReceipt>.Success(new(goal.Id, c.OperationId, goal.Version)));
    }, ct);

    public Task<Result<MutationReceipt>> Handle(AddGoalProgressCommand c, CancellationToken ct) => Execute(c, async unit =>
    {
        var goal = await unit.GoalAsync(c.Id, ct);
        if (goal is null) return Missing();
        if (goal.Version != c.Version) return Conflict();
        if (!goal.AddProgress(Money.Create(c.Amount, c.Currency)))
            return Fail("GOAL_PROGRESS_INVALID", "Progresso invalido para a meta.");
        return Result<MutationReceipt>.Success(new(goal.Id, c.OperationId, goal.Version));
    }, ct);

    public Task<Result<MutationReceipt>> Handle(ArchiveGoalCommand c, CancellationToken ct) => Execute(c, async unit =>
    {
        var goal = await unit.GoalAsync(c.Id, ct);
        if (goal is null) return Missing();
        if (goal.Version != c.Version) return Conflict();
        goal.Archive();
        return Result<MutationReceipt>.Success(new(goal.Id, c.OperationId, goal.Version));
    }, ct);

    public Task<Result<MutationReceipt>> Handle(CreateExpenseReductionGoalCommand c, CancellationToken ct) => Execute(c, async unit =>
    {
        if (c.CategoryId is { } categoryId)
        {
            var category = await unit.CategoryAsync(categoryId, ct);
            if (category is null) return Missing();
            if (category.IsArchived || category.Flow != FinancialFlow.Expense)
                return Fail("EXPENSE_REDUCTION_CATEGORY_INVALID", "A meta requer categoria de despesa ativa.");
        }
        var goal = ExpenseReductionGoal.Create(Guid.NewGuid(), TenantId, Subject, c.CategoryId,
            Money.Create(c.ReferenceAmount, c.Currency), Money.Create(c.TargetAmount, c.Currency),
            c.PeriodStart, c.PeriodEnd, DateTimeOffset.UtcNow);
        unit.Add(goal);
        return Result<MutationReceipt>.Success(new(goal.Id, c.OperationId, goal.Version));
    }, ct);

    public Task<Result<MutationReceipt>> Handle(ArchiveExpenseReductionGoalCommand c, CancellationToken ct) => Execute(c, async unit =>
    {
        var goal = await unit.ExpenseReductionGoalAsync(c.Id, ct);
        if (goal is null) return Missing();
        if (goal.Version != c.Version) return Conflict();
        goal.Archive();
        return Result<MutationReceipt>.Success(new(goal.Id, c.OperationId, goal.Version));
    }, ct);

    public Task<Result<MutationReceipt>> Handle(MarkNotificationReadCommand c, CancellationToken ct) => Execute(c, async unit =>
    {
        var notification = await unit.NotificationAsync(c.Id, ct);
        if (notification is null) return Missing();
        if (notification.Version != c.Version) return Conflict();
        notification.MarkRead(DateTimeOffset.UtcNow);
        return Result<MutationReceipt>.Success(new(notification.Id, c.OperationId, notification.Version));
    }, ct);
}

public static class FinancePayloadHash
{
    public static string Create(IFinanceMutation command)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("command", command.GetType().Name);
            writer.WritePropertyName("payload");
            Write(writer, JsonSerializer.SerializeToElement(command, command.GetType()));
            writer.WriteEndObject();
        }
        return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
    }

    private static void Write(Utf8JsonWriter writer, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject();
            foreach (var property in value.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
            { writer.WritePropertyName(property.Name); Write(writer, property.Value); }
            writer.WriteEndObject();
        }
        else if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            writer.WriteRawValue(number.ToString("G29", CultureInfo.InvariantCulture));
        else value.WriteTo(writer);
    }
}
