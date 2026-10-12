using RassAppControleFin.Contracts;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Api.Http;

public static class ResultResponse
{
    public static IResult ToHttp(this Result result, HttpContext context)
        => (result.IsSuccess ? Result<bool>.Success(true) : Result<bool>.Failure(result.Error!)).ToHttp(context);
    public static IResult ToHttp<T>(this Result<T> result, HttpContext context)
    {
        if (result.IsSuccess)
            return Results.Ok(new ApiResponse<T>(true, result.Value, null,
                new(context.TraceIdentifier)));

        var error = result.Error!;
        return Results.Json(new ApiResponse<object>(false, null,
            new(error.Code, error.Message, error.Details
                .Select(d => new ApiValidationError(d.Field, d.Message)).ToArray()),
            new(context.TraceIdentifier)), statusCode: StatusCode(error.Type));
    }

    public static int StatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => 400,
        ErrorType.Unauthorized => 401,
        ErrorType.Forbidden => 403,
        ErrorType.NotFound => 404,
        ErrorType.Conflict => 409,
        ErrorType.Domain => 422,
        ErrorType.Infrastructure => 503,
        _ => 500
    };
}
