using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

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
        if (exception is PostgresException postgres)
        {
            logger.LogError("Request failed. Endpoint={Endpoint} Operation={Operation} ExceptionType={ExceptionType} " +
                "SqlState={SqlState} Table={Table} Column={Column} Constraint={Constraint} CorrelationId={CorrelationId}",
                context.Request.Path.Value, Operation(context), exception.GetType().Name, postgres.SqlState,
                postgres.TableName, postgres.ColumnName, postgres.ConstraintName, context.TraceIdentifier);
        }
        else
        {
            logger.LogError("Request failed. Endpoint={Endpoint} Operation={Operation} ExceptionType={ExceptionType} CorrelationId={CorrelationId}",
                context.Request.Path.Value, Operation(context), exception.GetType().Name, context.TraceIdentifier);
        }

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

    private static string Operation(HttpContext context)
        => context.Request.Path.StartsWithSegments("/api/v1/sync") ? "finance.sync" : "http.request";
}
