using FluentValidation;
using MediatR;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult<TResponse>
{
    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var details = new List<ValidationError>();
        var context = new ValidationContext<TRequest>(request);

        foreach (var validator in validators)
        {
            var validation = await validator.ValidateAsync(context, cancellationToken);
            details.AddRange(validation.Errors.Select(f => new ValidationError(f.PropertyName, f.ErrorMessage)));
        }

        return details.Count == 0
            ? await next(cancellationToken)
            : TResponse.Failure(new Error("VALIDATION_FAILED", "Dados invalidos.",
                ErrorType.Validation, details));
    }
}
