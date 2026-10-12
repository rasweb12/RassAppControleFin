using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RassAppControleFin.AI;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Infrastructure.AI.Gemini;

public sealed class GeminiAiProvider(IHttpClientFactory clients, IOptions<GeminiOptions> options,
    ILogger<GeminiAiProvider> logger) : IAiProvider
{
    private static readonly ActivitySource Source = new("RassAppControleFin.Gemini");
    private static readonly Meter Meter = new("RassAppControleFin.Gemini");
    private static readonly Counter<long> Requests = Meter.CreateCounter<long>("rass.ai.requests");
    private static readonly Counter<long> Tokens = Meter.CreateCounter<long>("rass.ai.tokens");

    public async Task<Result<AiResponse>> GenerateAsync(AiPrompt prompt, CancellationToken cancellationToken)
    {
        var config = options.Value;
        var apiKey = Environment.GetEnvironmentVariable(GeminiOptions.ApiKeyEnvironmentVariable);
        if (!config.Enabled || string.IsNullOrWhiteSpace(apiKey))
            return Result<AiResponse>.Failure(new Error("AI_UNAVAILABLE", "Assistente financeiro indisponivel.", ErrorType.Infrastructure));
        using var activity = Source.StartActivity("gemini.generate");
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"v1beta/models/{Uri.EscapeDataString(config.Model)}:generateContent");
            request.Headers.Add("x-goog-api-key", apiKey);
            request.Content = JsonContent.Create(new
            {
                systemInstruction = new { parts = new[] { new { text = prompt.SystemInstructions } } },
                contents = new[] { new { role = "user", parts = new[] { new { text =
                    $"{prompt.UserQuestion}\n\nDados autorizados {prompt.Context.Period}: "
                    + string.Join("; ", prompt.Context.Aggregates.OrderBy(x => x.Key).Select(x => $"{x.Key}={x.Value:0.00}")) } } } },
                generationConfig = new { temperature = config.Temperature, maxOutputTokens = config.MaxOutputTokens }
            });
            using var response = await clients.CreateClient("gemini").SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Gemini request failed. StatusCode={StatusCode}", (int)response.StatusCode);
                return Result<AiResponse>.Failure(new Error("AI_UNAVAILABLE", "Assistente financeiro indisponivel.", ErrorType.Infrastructure));
            }
            using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
            var root = json.RootElement;
            var text = root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0
                && candidates[0].TryGetProperty("content", out var content)
                && content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0
                && parts[0].TryGetProperty("text", out var value) ? value.GetString() : null;
            if (string.IsNullOrWhiteSpace(text) || text.Length > 12000)
                return Result<AiResponse>.Failure(new Error("AI_RESPONSE_INVALID", "Resposta do assistente invalida.", ErrorType.Infrastructure));
            var used = root.TryGetProperty("usageMetadata", out var usage)
                && usage.TryGetProperty("totalTokenCount", out var total) && total.TryGetInt32(out var count) ? count : 0;
            Requests.Add(1); Tokens.Add(used); activity?.SetTag("ai.tokens", used);
            return Result<AiResponse>.Success(new AiResponse(text.Trim(), used));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException
            or Polly.Timeout.TimeoutRejectedException or Polly.CircuitBreaker.BrokenCircuitException)
        {
            logger.LogWarning("Gemini request unavailable. ExceptionType={ExceptionType}", ex.GetType().Name);
            return Result<AiResponse>.Failure(new Error("AI_UNAVAILABLE", "Assistente financeiro indisponivel.", ErrorType.Infrastructure));
        }
    }
}

public sealed class AiAuditService(ILogger<AiAuditService> logger) : IAiAuditService
{
    public Task RecordAsync(Guid interactionId, string outcome, int tokensUsed, CancellationToken cancellationToken)
    {
        logger.LogInformation("AI interaction completed. InteractionId={InteractionId} Outcome={Outcome} TokensUsed={TokensUsed}",
            interactionId, outcome, tokensUsed);
        return Task.CompletedTask;
    }
}
