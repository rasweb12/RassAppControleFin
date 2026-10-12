namespace RassAppControleFin.SharedKernel.Results;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Domain,
    Infrastructure
}

public sealed record ValidationError(string Field, string Message);

public sealed record Error
{
    public Error(string code, string message, ErrorType type,
        IEnumerable<ValidationError>? details = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (!Enum.IsDefined(type))
            throw new ArgumentOutOfRangeException(nameof(type));

        Code = code;
        Message = message;
        Type = type;
        Details = Array.AsReadOnly(details?.ToArray() ?? []);
    }

    public string Code { get; }
    public string Message { get; }
    public ErrorType Type { get; }
    public IReadOnlyList<ValidationError> Details { get; }
}
