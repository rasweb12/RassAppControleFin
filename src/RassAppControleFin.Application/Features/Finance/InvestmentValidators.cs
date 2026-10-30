using FluentValidation;
using RassAppControleFin.Domain.Investments;
using RassAppControleFin.Domain.ValueObjects;

namespace RassAppControleFin.Application.Features.Finance;

public sealed class ConfigurePayYourselfFirstPlanValidator : MutationValidator<ConfigurePayYourselfFirstPlanCommand>
{
    public ConfigurePayYourselfFirstPlanValidator()
    {
        RuleFor(x => x.CurrentRate).InclusiveBetween(0m, 1m);
        RuleFor(x => x.GoalRate).InclusiveBetween(0m, 1m).When(x => x.GoalRate.HasValue);
        RuleFor(x => x.Version).GreaterThan(0).When(x => x.Version.HasValue);
    }
}
public sealed class ConfigureEmergencyFundPlanValidator : MutationValidator<ConfigureEmergencyFundPlanCommand>
{
    public ConfigureEmergencyFundPlanValidator()
    {
        RuleFor(x => x.TargetType).Must(x => Enum.TryParse<EmergencyFundTargetType>(x, false, out _));
        RuleFor(x => x.TargetMultiple).GreaterThan(0).When(x => x.TargetType != nameof(EmergencyFundTargetType.FixedAmount));
        RuleFor(x => x.TargetMultiple).Null().When(x => x.TargetType == nameof(EmergencyFundTargetType.FixedAmount));
        RuleFor(x => x).Must(x => Money.IsValid(x.TargetAmount, x.Currency));
        RuleFor(x => x).Must(x => Money.IsValid(x.CurrentAmount, x.Currency, true));
        RuleFor(x => x).Must(x => Money.IsValid(x.MonthlyContributionTarget, x.Currency, true));
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Version).GreaterThan(0).When(x => x.Version.HasValue);
    }
}
public sealed class CreateInvestmentAssetValidator : MutationValidator<CreateInvestmentAssetCommand>
{
    public CreateInvestmentAssetValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120); RuleFor(x => x.Ticker).MaximumLength(20);
        RuleFor(x => x.AssetSubClass).NotEmpty().MaximumLength(80); RuleFor(x => x.Issuer).MaximumLength(120);
        RuleFor(x => x.Notes).MaximumLength(2000); RuleFor(x => x.AccountId).NotEqual(Guid.Empty).When(x => x.AccountId.HasValue);
        RuleFor(x => x.AssetClass).Must(x => Enum.TryParse<InvestmentAssetClass>(x, false, out _));
        RuleFor(x => x.Indexer).Must(x => Enum.TryParse<InvestmentIndexer>(x, false, out _));
        RuleFor(x => x.LiquidityType).Must(x => Enum.TryParse<InvestmentLiquidityType>(x, false, out _));
        RuleFor(x => x.Rate).InclusiveBetween(-1m, 100m).When(x => x.Rate.HasValue);
    }
}
public sealed class UpdateInvestmentAssetValidator : AbstractValidator<UpdateInvestmentAssetCommand>
{
    public UpdateInvestmentAssetValidator()
    {
        Include(new CreateInvestmentAssetValidatorAdapter());
        RuleFor(x => x.OperationId).NotEmpty(); RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Version).GreaterThan(0);
    }
    private sealed class CreateInvestmentAssetValidatorAdapter : AbstractValidator<UpdateInvestmentAssetCommand>
    {
        public CreateInvestmentAssetValidatorAdapter()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(120); RuleFor(x => x.Ticker).MaximumLength(20);
            RuleFor(x => x.AssetSubClass).NotEmpty().MaximumLength(80); RuleFor(x => x.Issuer).MaximumLength(120);
            RuleFor(x => x.Notes).MaximumLength(2000); RuleFor(x => x.AccountId).NotEqual(Guid.Empty).When(x => x.AccountId.HasValue);
            RuleFor(x => x.AssetClass).Must(x => Enum.TryParse<InvestmentAssetClass>(x, false, out _));
            RuleFor(x => x.Indexer).Must(x => Enum.TryParse<InvestmentIndexer>(x, false, out _));
            RuleFor(x => x.LiquidityType).Must(x => Enum.TryParse<InvestmentLiquidityType>(x, false, out _));
            RuleFor(x => x.Rate).InclusiveBetween(-1m, 100m).When(x => x.Rate.HasValue);
        }
    }
}
public sealed class RecordInvestmentTransactionValidator : MutationValidator<RecordInvestmentTransactionCommand>
{
    public RecordInvestmentTransactionValidator()
    {
        RuleFor(x => x.InvestmentAssetId).NotEmpty(); RuleFor(x => x.Type).Must(x => Enum.TryParse<InvestmentTransactionType>(x, false, out _));
        RuleFor(x => x).Must(x => Money.IsValid(x.GrossAmount, x.Currency, true));
        RuleFor(x => x.Fees).GreaterThanOrEqualTo(0); RuleFor(x => x.Taxes).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(x => x.Fees + x.Taxes <= x.GrossAmount);
        RuleFor(x => x.Quantity).GreaterThan(0).When(x => x.Quantity.HasValue); RuleFor(x => x.UnitPrice).GreaterThan(0).When(x => x.UnitPrice.HasValue);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
public sealed class SetTargetPortfolioValidator : MutationValidator<SetTargetPortfolioCommand>
{
    public SetTargetPortfolioValidator()
    {
        RuleFor(x => x.Allocations).NotEmpty();
        RuleForEach(x => x.Allocations).ChildRules(item =>
        {
            item.RuleFor(x => x.Key).NotEmpty().MaximumLength(120);
            item.RuleFor(x => x.AssetClass).Must(x => Enum.TryParse<InvestmentAssetClass>(x, false, out _));
            item.RuleFor(x => x.Weight).InclusiveBetween(0m, 100m);
            item.RuleFor(x => x.Version).GreaterThan(0).When(x => x.Version.HasValue);
            item.RuleFor(x => x.InvestmentAssetId).NotEqual(Guid.Empty).When(x => x.InvestmentAssetId.HasValue);
        });
        RuleFor(x => x).Must(x => x.Allocations.Select(a => a.Key.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == x.Allocations.Count);
        RuleFor(x => x).Must(x => x.Allocations.Sum(a => a.Weight) == 100m).WithMessage("A carteira-alvo deve totalizar 100%.");
    }
}
public sealed class ListInvestmentAssetsValidator : PageValidator<ListInvestmentAssetsQuery>;
public sealed class ListInvestmentTransactionsValidator : PageValidator<ListInvestmentTransactionsQuery>
{ public ListInvestmentTransactionsValidator() => RuleFor(x => x.InvestmentAssetId).NotEqual(Guid.Empty).When(x => x.InvestmentAssetId.HasValue); }
