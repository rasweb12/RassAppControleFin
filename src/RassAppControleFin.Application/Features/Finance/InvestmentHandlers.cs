using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using MediatR;
using RassAppControleFin.Application.Abstractions.Persistence;
using RassAppControleFin.Domain.Entities.Finance;
using RassAppControleFin.Domain.Investments;
using RassAppControleFin.Domain.ValueObjects;
using RassAppControleFin.MultiTenancy;
using RassAppControleFin.Security;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Application.Features.Finance;

public sealed class InvestmentMutationHandlers(IFinanceUnitFactory factory, IUserContext user, ITenantContext tenant) :
    IRequestHandler<ConfigurePayYourselfFirstPlanCommand, Result<MutationReceipt>>,
    IRequestHandler<ConfigureEmergencyFundPlanCommand, Result<MutationReceipt>>,
    IRequestHandler<CreateInvestmentAssetCommand, Result<MutationReceipt>>,
    IRequestHandler<UpdateInvestmentAssetCommand, Result<MutationReceipt>>,
    IRequestHandler<RecordInvestmentTransactionCommand, Result<MutationReceipt>>,
    IRequestHandler<SetTargetPortfolioCommand, Result<MutationReceipt>>
{
    private Guid TenantId => tenant.TenantId!.Value;
    private string Subject => user.Subject!;
    private static Result<MutationReceipt> Fail(string code, string message, ErrorType type = ErrorType.Domain)
        => Result<MutationReceipt>.Failure(new(code, message, type));
    private static Result<MutationReceipt> Missing() => Fail("RESOURCE_NOT_FOUND", "Recurso nao encontrado.", ErrorType.NotFound);
    private static Result<MutationReceipt> Conflict() => Fail("VERSION_CONFLICT", "O recurso foi alterado. Atualize os dados.", ErrorType.Conflict);

    private async Task<Result<MutationReceipt>> Execute(IFinanceMutation command,
        Func<IFinanceUnit, Task<Result<MutationReceipt>>> change, CancellationToken ct)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.Subject))
            return Fail("IDENTITY_REQUIRED", "Autenticacao necessaria.", ErrorType.Unauthorized);
        if (!tenant.IsResolved || tenant.TenantId is null || !user.Permissions.Contains(command.Permission))
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

    public Task<Result<MutationReceipt>> Handle(ConfigurePayYourselfFirstPlanCommand c, CancellationToken ct) => Execute(c, async unit =>
    {
        var now = DateTimeOffset.UtcNow;
        var plan = await unit.PayYourselfFirstPlanAsync(ct);
        if (plan is null)
        {
            if (c.Version is not null) return Conflict();
            try { plan = PayYourselfFirstPlan.Create(Guid.NewGuid(), TenantId, Subject, c.CurrentRate, c.GoalRate, c.EffectiveFrom, now); }
            catch (ArgumentException) { return Fail("PAY_YOURSELF_FIRST_INVALID", "Percentuais ou vigencia invalidos."); }
            unit.Add(plan);
        }
        else
        {
            if (c.Version != plan.Version) return Conflict();
            try { plan.Change(c.CurrentRate, c.GoalRate, c.EffectiveFrom); }
            catch (ArgumentException) { return Fail("PAY_YOURSELF_FIRST_INVALID", "Percentuais ou vigencia invalidos."); }
        }
        unit.Add(PayYourselfFirstPlanRevision.Record(Guid.NewGuid(), plan, now));
        return Result<MutationReceipt>.Success(new(plan.Id, c.OperationId, plan.Version));
    }, ct);

    public Task<Result<MutationReceipt>> Handle(ConfigureEmergencyFundPlanCommand c, CancellationToken ct) => Execute(c, async unit =>
    {
        var plan = await unit.EmergencyFundPlanAsync(ct);
        try
        {
            var targetType = Enum.Parse<EmergencyFundTargetType>(c.TargetType, false);
            var target = Money.Create(c.TargetAmount, c.Currency);
            if (plan is null)
            {
                if (c.Version is not null) return Conflict();
                plan = EmergencyFundPlan.Create(Guid.NewGuid(), TenantId, Subject, targetType, c.TargetMultiple, target,
                    c.CurrentAmount, c.MonthlyContributionTarget, c.LiquidityRequired, c.Notes, DateTimeOffset.UtcNow);
                unit.Add(plan);
            }
            else
            {
                if (c.Version != plan.Version) return Conflict();
                plan.Change(targetType, c.TargetMultiple, target, c.CurrentAmount, c.MonthlyContributionTarget,
                    c.LiquidityRequired, c.Notes);
            }
            return Result<MutationReceipt>.Success(new(plan.Id, c.OperationId, plan.Version));
        }
        catch (ArgumentException) { return Fail("EMERGENCY_FUND_INVALID", "Dados da reserva de emergencia invalidos."); }
    }, ct);

    public Task<Result<MutationReceipt>> Handle(CreateInvestmentAssetCommand c, CancellationToken ct) => Execute(c, async unit =>
    {
        if (c.AccountId is { } accountId)
        {
            var account = await unit.AccountAsync(accountId, ct);
            if (account is null || account.IsArchived) return Missing();
        }
        try
        {
            var asset = InvestmentAsset.Create(Guid.NewGuid(), TenantId, Subject, c.Name, c.Ticker,
                Enum.Parse<InvestmentAssetClass>(c.AssetClass, false), c.AssetSubClass, c.AccountId, c.Issuer,
                Enum.Parse<InvestmentIndexer>(c.Indexer, false), c.Rate, c.MaturityDate,
                Enum.Parse<InvestmentLiquidityType>(c.LiquidityType, false), c.FgcProtected, c.TaxExempt, c.Notes, DateTimeOffset.UtcNow);
            unit.Add(asset);
            return Result<MutationReceipt>.Success(new(asset.Id, c.OperationId, asset.Version));
        }
        catch (ArgumentException) { return Fail("INVESTMENT_ASSET_INVALID", "Dados do ativo invalidos."); }
    }, ct);

    public Task<Result<MutationReceipt>> Handle(UpdateInvestmentAssetCommand c, CancellationToken ct) => Execute(c, async unit =>
    {
        var asset = await unit.InvestmentAssetAsync(c.Id, ct);
        if (asset is null) return Missing();
        if (asset.Version != c.Version) return Conflict();
        if (c.AccountId is { } accountId)
        {
            var account = await unit.AccountAsync(accountId, ct);
            if (account is null || account.IsArchived) return Missing();
        }
        try
        {
            asset.Change(c.Name, c.Ticker, Enum.Parse<InvestmentAssetClass>(c.AssetClass, false), c.AssetSubClass,
                c.AccountId, c.Issuer, Enum.Parse<InvestmentIndexer>(c.Indexer, false), c.Rate, c.MaturityDate,
                Enum.Parse<InvestmentLiquidityType>(c.LiquidityType, false), c.FgcProtected, c.TaxExempt, c.Notes,
                c.Active, DateTimeOffset.UtcNow);
            return Result<MutationReceipt>.Success(new(asset.Id, c.OperationId, asset.Version));
        }
        catch (ArgumentException) { return Fail("INVESTMENT_ASSET_INVALID", "Dados do ativo invalidos."); }
    }, ct);

    public Task<Result<MutationReceipt>> Handle(RecordInvestmentTransactionCommand c, CancellationToken ct) => Execute(c, async unit =>
    {
        var asset = await unit.InvestmentAssetAsync(c.InvestmentAssetId, ct);
        if (asset is null) return Missing();
        if (!asset.Active) return Fail("INVESTMENT_ASSET_INACTIVE", "Ativo de investimento inativo.");
        try
        {
            var movement = InvestmentTransaction.Create(Guid.NewGuid(), TenantId, Subject, asset.Id, c.Date,
                Enum.Parse<InvestmentTransactionType>(c.Type, false), c.Quantity, c.UnitPrice,
                Money.Create(c.GrossAmount, c.Currency, true), c.Fees, c.Taxes, c.Notes, DateTimeOffset.UtcNow);
            unit.Add(movement);
            return Result<MutationReceipt>.Success(new(movement.Id, c.OperationId, movement.Version));
        }
        catch (ArgumentException) { return Fail("INVESTMENT_TRANSACTION_INVALID", "Movimentacao de investimento invalida."); }
    }, ct);

    public Task<Result<MutationReceipt>> Handle(SetTargetPortfolioCommand c, CancellationToken ct) => Execute(c, async unit =>
    {
        if (c.Allocations.Count == 0 || c.Allocations.Select(x => x.Key.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != c.Allocations.Count)
            return Fail("TARGET_PORTFOLIO_INVALID", "Informe pesos unicos que totalizem 100%.");
        var existing = await unit.TargetPortfolioAllocationsAsync(ct);
        if (c.Allocations.Any(x => x.Id is not null && existing.All(y => y.Id != x.Id))
            || existing.Any(x => c.Allocations.All(y => y.Id != x.Id)))
            return Conflict();
        try
        {
            var requested = c.Allocations.Select(x => new TargetAllocation(x.Key,
                Enum.Parse<InvestmentAssetClass>(x.AssetClass, false), x.Weight)).ToArray();
            TargetAllocation.Validate(requested);
            TargetPortfolioAllocation? receiptResource = existing.FirstOrDefault();
            foreach (var input in c.Allocations)
            {
                if (input.InvestmentAssetId is { } assetId && await unit.InvestmentAssetAsync(assetId, ct) is null) return Missing();
                var assetClass = Enum.Parse<InvestmentAssetClass>(input.AssetClass, false);
                var found = input.Id is null ? null : existing.Single(x => x.Id == input.Id.Value);
                if (found is null)
                {
                    var created = TargetPortfolioAllocation.Create(Guid.NewGuid(), TenantId, Subject, input.InvestmentAssetId,
                        input.Key, assetClass, input.Weight, DateTimeOffset.UtcNow);
                    unit.Add(created);
                    receiptResource ??= created;
                }
                else
                {
                    if (input.Version != found.Version) return Conflict();
                    found.Change(input.InvestmentAssetId, input.Key, assetClass, input.Weight);
                }
            }
            if (receiptResource is null) return Fail("TARGET_PORTFOLIO_INVALID", "Carteira-alvo invalida.");
            return Result<MutationReceipt>.Success(new(receiptResource.Id, c.OperationId, receiptResource.Version));
        }
        catch (ArgumentException) { return Fail("TARGET_PORTFOLIO_INVALID", "Carteira-alvo invalida."); }
    }, ct);
}

public sealed class InvestmentQueryHandlers(IFinanceReadStore store, IUserContext user, ITenantContext tenant) :
    IRequestHandler<GetPayYourselfFirstPlanQuery, Result<PayYourselfFirstPlanView?>>,
    IRequestHandler<GetEmergencyFundPlanQuery, Result<EmergencyFundPlanView?>>,
    IRequestHandler<ListInvestmentAssetsQuery, Result<Page<InvestmentAssetView>>>,
    IRequestHandler<ListInvestmentTransactionsQuery, Result<Page<InvestmentTransactionView>>>,
    IRequestHandler<ListTargetPortfolioQuery, Result<IReadOnlyList<TargetPortfolioAllocationView>>>
{
    private Task<Result<T>> Read<T>(Func<Task<Result<T>>> action) => user.IsAuthenticated && tenant.IsResolved
        && user.Permissions.Contains(Permissions.TransactionsRead) ? action()
        : Task.FromResult(Result<T>.Failure(new("FINANCE_FORBIDDEN", "Consulta nao autorizada.", ErrorType.Forbidden)));
    public Task<Result<PayYourselfFirstPlanView?>> Handle(GetPayYourselfFirstPlanQuery q, CancellationToken ct) => Read(() => store.PayYourselfFirstPlanAsync(ct));
    public Task<Result<EmergencyFundPlanView?>> Handle(GetEmergencyFundPlanQuery q, CancellationToken ct) => Read(() => store.EmergencyFundPlanAsync(ct));
    public Task<Result<Page<InvestmentAssetView>>> Handle(ListInvestmentAssetsQuery q, CancellationToken ct) => Read(() => store.InvestmentAssetsAsync(q, ct));
    public Task<Result<Page<InvestmentTransactionView>>> Handle(ListInvestmentTransactionsQuery q, CancellationToken ct) => Read(() => store.InvestmentTransactionsAsync(q, ct));
    public Task<Result<IReadOnlyList<TargetPortfolioAllocationView>>> Handle(ListTargetPortfolioQuery q, CancellationToken ct) => Read(() => store.TargetPortfolioAsync(ct));
}
