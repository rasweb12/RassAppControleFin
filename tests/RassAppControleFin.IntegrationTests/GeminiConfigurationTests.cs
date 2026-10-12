using System.ComponentModel.DataAnnotations;
using RassAppControleFin.Infrastructure.AI.Gemini;
using Xunit;

namespace RassAppControleFin.IntegrationTests;

public sealed class GeminiConfigurationTests
{
    [Fact]
    public void Ai_is_disabled_and_has_no_hardcoded_model_or_api_key()
    {
        var options = new GeminiOptions();
        Assert.False(options.Enabled);
        Assert.Empty(options.Model);
        Assert.Null(typeof(GeminiOptions).GetProperty("ApiKey"));
        Assert.Equal("GEMINI_API_KEY", GeminiOptions.ApiKeyEnvironmentVariable);
        Assert.True(GeminiOptions.HasSafeModelIdentifier(options));
    }
    [Theory]
    [InlineData("gemini-configured-supported-model", true)]
    [InlineData("", false)]
    [InlineData("gpt-provider", false)]
    [InlineData("../other", false)]
    [InlineData("gemini-model?key=secret", false)]
    [InlineData("gemini-model/other", false)]
    public void Only_safe_gemini_model_identifiers_are_allowed(string model, bool expected)
        => Assert.Equal(expected, GeminiOptions.HasSafeModelIdentifier(new() { Enabled = true, Model = model }));
    [Theory]
    [InlineData(0, 2048, 0.3)]
    [InlineData(121, 2048, 0.3)]
    [InlineData(30, 0, 0.3)]
    [InlineData(30, 8193, 0.3)]
    [InlineData(30, 2048, 3.0)]
    public void Timeout_token_and_temperature_limits_are_bounded(int timeout, int tokens, double temperature)
    {
        var options = new GeminiOptions { TimeoutSeconds = timeout, MaxOutputTokens = tokens, Temperature = temperature };
        Assert.False(Validator.TryValidateObject(options, new(options), [], validateAllProperties: true));
    }
}
