using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace RassAppControleFin.Infrastructure.AI.Gemini;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";
    public const string ApiKeyEnvironmentVariable = "GEMINI_API_KEY";
    public bool Enabled { get; init; }
    public string Model { get; init; } = "";
    [Range(1, 120)] public int TimeoutSeconds { get; init; } = 30;
    [Range(1, 8192)] public int MaxOutputTokens { get; init; } = 2048;
    [Range(0.0, 2.0)] public double Temperature { get; init; } = 0.3;
    public static bool HasSafeModelIdentifier(GeminiOptions options) => !options.Enabled
        || Regex.IsMatch(options.Model, "\\Agemini-[A-Za-z0-9][A-Za-z0-9.-]{0,100}\\z", RegexOptions.CultureInvariant);
}
