using System.Diagnostics;
using MediatR;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Application.Behaviors;

public static class ApplicationTelemetry
{
    public const string SourceName = "RassAppControleFin.Application";
    public static readonly ActivitySource Source = new(SourceName);
}

public sealed class TelemetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult<TResponse>
{
    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        using var activity = ApplicationTelemetry.Source.StartActivity(typeof(TRequest).Name);
        try
        {
            var response = await next(cancellationToken);
            activity?.SetTag("app.result.success", response.IsSuccess);
            if (!response.IsSuccess)
            {
                activity?.SetTag("app.error.code", response.Error!.Code);
                activity?.SetStatus(ActivityStatusCode.Error);
            }
            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            activity?.SetTag("app.cancelled", true);
            throw;
        }
        catch
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
    }
}
