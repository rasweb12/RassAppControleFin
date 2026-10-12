using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace RassAppControleFin.Api.Http;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context,
        Exception exception, CancellationToken cancellationToken)
    {
        var status = exception is ValidationException ? 400 : 500;

        // Do not include exception messages or payloads in responses or logs.
        logger.LogError("Request failed. ExceptionType={ExceptionType} CorrelationId={CorrelationId}",
            exception.GetType().Name, context.TraceIdentifier);

        context.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = status == 400 ? "Dados invalidos." : "Falha inesperada.",
                Extensions = { ["correlationId"] = context.TraceIdentifier }
            }
        });
    }
}
