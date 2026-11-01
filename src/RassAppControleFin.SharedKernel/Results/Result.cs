namespace RassAppControleFin.SharedKernel.Results;

public interface IResult<TSelf> where TSelf : IResult<TSelf>
{
    bool IsSuccess { get; }
    Error? Error { get; }
    static abstract TSelf Failure(Error error);
}

public sealed class Result : IResult<Result>
{
    private Result(Error? error) => Error = error;

    public bool IsSuccess => Error is null;
    public Error? Error { get; }

    public static Result Success() => new(null);

    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(error);
    }
}

public sealed class Result<T> : IResult<Result<T>>
{
    private readonly T? _value;

    private Result(T value) => _value = value;
    private Result(Error error) => Error = error;

    public bool IsSuccess => Error is null;
    public Error? Error { get; }
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result has no value.");

    public static Result<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(value);
    }

    /// <summary>
    /// Creates a successful result for a query whose domain contract allows an
    /// absent value.  Keep <see cref="Success"/> strict for mutation and
    /// required-query contracts.
    /// </summary>
    public static Result<T> SuccessOptional(T? value) => new(value!);

    public static Result<T> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(error);
    }
}
