namespace RassAppControleFin.Contracts;

public sealed record ApiResponse<T>(bool Success, T? Data, ApiError? Error, ApiMeta Meta);
public sealed record ApiError(string Code, string Message, IReadOnlyList<ApiValidationError> Details);
public sealed record ApiValidationError(string Field, string Message);
public sealed record ApiMeta(string CorrelationId);
public sealed record SystemStatusDto(string Name, string Version, string Stage);
