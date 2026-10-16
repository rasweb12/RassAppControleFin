using FluentValidation;
using MediatR;
using RassAppControleFin.Application.Behaviors;
using RassAppControleFin.Application.Features.Identity;
using RassAppControleFin.SharedKernel.Results;
using Xunit;

namespace RassAppControleFin.UnitTests;

public sealed class ValidationTests
{
    private sealed record Input(string Name);

    private sealed class AsyncValidator : AbstractValidator<Input>
    {
        public AsyncValidator()
        {
            RuleFor(x => x.Name).MustAsync(async (value, token) =>
            {
                await Task.Yield();
                token.ThrowIfCancellationRequested();
                return value == "valid";
            }).WithMessage("Nome invalido.");
        }
    }

    [Fact]
    public async Task Async_validation_returns_structured_failure_without_invoking_handler()
    {
        var behavior = new ValidationBehavior<Input, Result<int>>([new AsyncValidator()]);
        var called = false;
        RequestHandlerDelegate<Result<int>> next = _ =>
        {
            called = true;
            return Task.FromResult(Result<int>.Success(1));
        };

        var result = await behavior.Handle(new("invalid"), next, CancellationToken.None);
        Assert.False(called);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Equal("Name", Assert.Single(result.Error.Details).Field);
    }

    [Fact]
    public async Task Valid_input_invokes_handler()
    {
        var behavior = new ValidationBehavior<Input, Result<int>>([new AsyncValidator()]);
        var result = await behavior.Handle(new("valid"),
            _ => Task.FromResult(Result<int>.Success(7)), CancellationToken.None);
        Assert.Equal(7, result.Value);
    }

    [Fact]
    public async Task Non_generic_result_supports_same_pipeline()
    {
        var behavior = new ValidationBehavior<Input, Result>([new AsyncValidator()]);
        var result = await behavior.Handle(new("invalid"),
            _ => Task.FromResult(Result.Success()), CancellationToken.None);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Cancellation_is_propagated()
    {
        var behavior = new ValidationBehavior<Input, Result<int>>([new AsyncValidator()]);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => behavior.Handle(
            new("valid"), _ => Task.FromResult(Result<int>.Success(7)), cancellation.Token));
    }

    [Fact]
    public void Password_reset_requires_a_token_and_an_eight_character_password()
    {
        var validator = new ResetPasswordValidator();

        var result = validator.Validate(new ResetPasswordCommand("", "short"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Token");
        Assert.Contains(result.Errors, error => error.PropertyName == "NewPassword");
    }
}
