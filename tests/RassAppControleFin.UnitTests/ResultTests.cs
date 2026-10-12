using RassAppControleFin.SharedKernel.Results;
using Xunit;

namespace RassAppControleFin.UnitTests;

public sealed class ResultTests
{
    [Fact]
    public void Success_has_value_and_no_error()
    {
        var result = Result<int>.Success(42);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(42, result.Value);
        Assert.True(Result.Success().IsSuccess);
    }

    [Fact]
    public void Failure_has_error_and_rejects_value_access()
    {
        var error = new Error("CONFLICT", "Conflito.", ErrorType.Conflict);
        var result = Result<int>.Failure(error);
        Assert.False(result.IsSuccess);
        Assert.Same(error, result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.False(Result.Failure(error).IsSuccess);
    }

    [Fact]
    public void Null_success_or_error_cannot_create_an_invalid_state()
    {
        Assert.Throws<ArgumentNullException>(() => Result<string>.Success(null!));
        Assert.Throws<ArgumentNullException>(() => Result<int>.Failure(null!));
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
    }

    [Fact]
    public void Validation_details_are_snapshotted()
    {
        var details = new List<ValidationError> { new("Amount", "Valor invalido.") };
        var error = new Error("VALIDATION", "Dados invalidos.", ErrorType.Validation, details);
        details.Clear();
        Assert.Single(error.Details);
    }

    [Fact]
    public void Undefined_error_type_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Error("INVALID", "Invalid.", (ErrorType)999));
    }
}
