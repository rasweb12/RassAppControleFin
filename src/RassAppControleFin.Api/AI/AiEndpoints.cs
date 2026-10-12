using RassAppControleFin.AI;
using RassAppControleFin.Api.Http;
using RassAppControleFin.Contracts;
using RassAppControleFin.Security;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Api.AI;

public static class AiEndpoints
{
    public static void MapAi(this WebApplication app)
    {
        app.MapPost("/api/v1/ai/ask", async (AskFinancialAssistantRequest request, IAiOrchestrator ai,
            HttpContext context, CancellationToken cancellationToken) =>
        {
            var result = await ai.AskAsync(request.Question, request.Period, cancellationToken);
            return (result.IsSuccess
                ? Result<FinancialAssistantResponse>.Success(new(result.Value.Text, result.Value.TokensUsed, true))
                : Result<FinancialAssistantResponse>.Failure(result.Error!)).ToHttp(context);
        }).RequireAuthorization(Permissions.TransactionsRead).RequireRateLimiting("ai");
    }
}
