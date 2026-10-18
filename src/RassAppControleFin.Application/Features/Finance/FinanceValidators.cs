using FluentValidation;
using RassAppControleFin.Domain.ValueObjects;

namespace RassAppControleFin.Application.Features.Finance;

public abstract class MutationValidator<T> : AbstractValidator<T> where T : IFinanceMutation
{
    protected MutationValidator() => RuleFor(x => x.OperationId).NotEmpty();
}
public sealed class OpenAccountValidator : MutationValidator<OpenAccountCommand>
{
    public OpenAccountValidator() { RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x).Must(x => Money.IsValid(x.OpeningBalance, x.Currency, true)).WithMessage("Saldo/moeda invalidos."); }
}
public sealed class MaintainResourceValidator : MutationValidator<MaintainResourceCommand>
{
    public MaintainResourceValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Version).GreaterThan(0);
        RuleFor(x => x.Resource).Must(x => x is "account" or "category" or "card");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120).When(x => !x.Archive); }
}
public sealed class CategoryValidator : MutationValidator<CreateCategoryCommand>
{
    public CategoryValidator() { RuleFor(x => x.Name).NotEmpty().MaximumLength(120); RuleFor(x => x.Flow).Must(x => x is "Income" or "Expense"); }
}
public sealed class CardValidator : MutationValidator<CreateCardCommand>
{
    public CardValidator() { RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x).Must(x => Money.IsValid(x.Limit, x.Currency));
        RuleFor(x => x.ClosingDay).InclusiveBetween(1, 28); RuleFor(x => x.DueDay).InclusiveBetween(1, 28); }
}
public sealed class TransactionValidator : MutationValidator<CreateTransactionCommand>
{
    public TransactionValidator() { RuleFor(x => x.AccountId).NotEmpty(); RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x).Must(x => Money.IsValid(x.Amount, x.Currency));
        RuleFor(x => x.Flow).Must(x => x is "Income" or "Expense"); RuleFor(x => x.Status).Must(x => x is "Pending" or "Posted");
        RuleFor(x => x.AccountVersion).GreaterThan(0); RuleFor(x => x.OccurredAt).Must(x => x.Offset == TimeSpan.Zero);
        RuleFor(x => x.Description).NotNull().MaximumLength(500); }
}
public sealed class EditTransactionValidator : MutationValidator<EditTransactionCommand>
{
    public EditTransactionValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x).Must(x => Money.IsValid(x.Amount, x.Currency)); RuleFor(x => x.Version).GreaterThan(0);
        RuleFor(x => x.Status).Must(x => x is "Pending" or "Posted"); RuleFor(x => x.AccountVersion).GreaterThan(0);
        RuleFor(x => x.OccurredAt).Must(x => x.Offset == TimeSpan.Zero); RuleFor(x => x.Description).NotNull().MaximumLength(500); }
}
public sealed class CancelTransactionValidator : MutationValidator<CancelTransactionCommand>
{
    public CancelTransactionValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Version).GreaterThan(0); RuleFor(x => x.AccountVersion).GreaterThan(0); }
}
public sealed class TransferValidator : MutationValidator<TransferCommand>
{
    public TransferValidator() { RuleFor(x => x.SourceAccountId).NotEmpty(); RuleFor(x => x.DestinationAccountId).NotEmpty().NotEqual(x => x.SourceAccountId);
        RuleFor(x => x).Must(x => Money.IsValid(x.Amount, x.Currency)); RuleFor(x => x.SourceVersion).GreaterThan(0);
        RuleFor(x => x.DestinationVersion).GreaterThan(0); RuleFor(x => x.OccurredAt).Must(x => x.Offset == TimeSpan.Zero);
        RuleFor(x => x.Description).NotNull().MaximumLength(500); }
}
public sealed class CardMovementValidator : MutationValidator<CardMovementCommand>
{
    public CardMovementValidator() { RuleFor(x => x.CardId).NotEmpty(); RuleFor(x => x.CardVersion).GreaterThan(0);
        RuleFor(x => x).Must(x => Money.IsValid(x.Amount, x.Currency)); RuleFor(x => x.Kind).Must(x => x is "Purchase" or "Payment");
        RuleFor(x => x.OccurredAt).Must(x => x.Offset == TimeSpan.Zero); RuleFor(x => x.Description).NotNull().MaximumLength(500);
        RuleFor(x => x.CategoryId).NotNull().NotEqual(Guid.Empty).When(x => x.Kind == "Purchase");
        RuleFor(x => x.AccountId).Null().When(x => x.Kind == "Purchase");
        RuleFor(x => x.AccountId).NotNull().NotEqual(Guid.Empty).When(x => x.Kind == "Payment");
        RuleFor(x => x.AccountVersion).NotNull().GreaterThan(0).When(x => x.Kind == "Payment");
        RuleFor(x => x.CategoryId).Null().When(x => x.Kind == "Payment"); }
}
public sealed class CreateBudgetValidator : MutationValidator<CreateBudgetCommand>
{
    public CreateBudgetValidator()
    {
        RuleFor(x => x.CategoryId).NotEqual(Guid.Empty).When(x => x.CategoryId.HasValue);
        RuleFor(x => x).Must(x => Money.IsValid(x.Amount, x.Currency));
        RuleFor(x => x.PeriodStart).Must(x => x.Offset == TimeSpan.Zero);
        RuleFor(x => x.PeriodEnd).Must(x => x.Offset == TimeSpan.Zero)
            .GreaterThan(x => x.PeriodStart);
    }
}
public sealed class ArchiveBudgetValidator : MutationValidator<ArchiveBudgetCommand>
{
    public ArchiveBudgetValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Version).GreaterThan(0); }
}
public sealed class CreateGoalValidator : MutationValidator<CreateGoalCommand>
{
    public CreateGoalValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x).Must(x => Money.IsValid(x.TargetAmount, x.Currency));
        RuleFor(x => x.TargetDate).Must(x => !x.HasValue || x.Value.Offset == TimeSpan.Zero);
    }
}
public sealed class AddGoalProgressValidator : MutationValidator<AddGoalProgressCommand>
{
    public AddGoalProgressValidator()
    {
        RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Version).GreaterThan(0);
        RuleFor(x => x).Must(x => Money.IsValid(x.Amount, x.Currency));
    }
}
public sealed class ArchiveGoalValidator : MutationValidator<ArchiveGoalCommand>
{
    public ArchiveGoalValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Version).GreaterThan(0); }
}
public sealed class CreateExpenseReductionGoalValidator : MutationValidator<CreateExpenseReductionGoalCommand>
{
    public CreateExpenseReductionGoalValidator()
    {
        RuleFor(x => x.CategoryId).NotEqual(Guid.Empty).When(x => x.CategoryId.HasValue);
        RuleFor(x => x).Must(x => Money.IsValid(x.ReferenceAmount, x.Currency) && Money.IsValid(x.TargetAmount, x.Currency));
        RuleFor(x => x).Must(x => x.TargetAmount < x.ReferenceAmount).WithMessage("A meta deve ser menor que a referência.");
        RuleFor(x => x.PeriodStart).Must(x => x.Offset == TimeSpan.Zero);
        RuleFor(x => x.PeriodEnd).Must(x => x.Offset == TimeSpan.Zero).GreaterThan(x => x.PeriodStart);
    }
}
public sealed class ArchiveExpenseReductionGoalValidator : MutationValidator<ArchiveExpenseReductionGoalCommand>
{ public ArchiveExpenseReductionGoalValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Version).GreaterThan(0); } }
public sealed class MarkNotificationReadValidator : MutationValidator<MarkNotificationReadCommand>
{ public MarkNotificationReadValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Version).GreaterThan(0); } }
