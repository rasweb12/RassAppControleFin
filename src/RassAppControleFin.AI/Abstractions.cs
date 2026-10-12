using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.AI;

public sealed record FinanceContext(Guid TenantId, string UserId, string Period,
    IReadOnlyDictionary<string, decimal> Aggregates);
public sealed record AiPrompt(string SystemInstructions, string UserQuestion, FinanceContext Context);
public sealed record AiResponse(string Text, int TokensUsed);

public interface IAiProvider
{
    Task<Result<AiResponse>> GenerateAsync(AiPrompt prompt, CancellationToken cancellationToken);
}

public interface IFinanceContextService
{
    Task<Result<FinanceContext>> GetAsync(string period, CancellationToken cancellationToken);
}

public interface IPromptBuilder
{
    Result<AiPrompt> Build(string question, FinanceContext context);
}

public interface IAiOrchestrator
{
    Task<Result<AiResponse>> AskAsync(string question, string period, CancellationToken cancellationToken);
}

public interface IAiAuditService
{
    Task RecordAsync(Guid interactionId, string outcome, int tokensUsed,
        CancellationToken cancellationToken);
}
