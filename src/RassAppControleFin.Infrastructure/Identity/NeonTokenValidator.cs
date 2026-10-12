using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using RassAppControleFin.Infrastructure.Configuration;
using ScottBrady.IdentityModel.Tokens;

namespace RassAppControleFin.Infrastructure.Identity;

public sealed class NeonTokenValidator
{
    private readonly NeonOptions _options;
    public ConfigurationManager<OpenIdConnectConfiguration>? Configuration { get; }

    public NeonTokenValidator(IOptions<NeonOptions> options, IHttpClientFactory clients)
    {
        _options = options.Value;
        if (string.IsNullOrEmpty(_options.JwksUrl)) return; // Unconfigured hosts fail closed.
        if (!Uri.TryCreate(_options.JwksUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https"
            || string.IsNullOrWhiteSpace(_options.ExpectedIssuer) || string.IsNullOrWhiteSpace(_options.ExpectedAudience))
            throw new InvalidOperationException("Invalid Neon JWT trust configuration.");
        Configuration = new(_options.JwksUrl, new EdDsaJwksRetriever(_options.ExpectedIssuer),
            new HttpDocumentRetriever(clients.CreateClient("neon-auth")) { RequireHttps = true });
    }

    public TokenValidationParameters Parameters => new()
    {
        ValidIssuer = _options.ExpectedIssuer, ValidAudience = _options.ExpectedAudience,
        ValidateIssuer = true, ValidateAudience = true, ValidateIssuerSigningKey = true,
        ValidateLifetime = true, RequireExpirationTime = true, RequireSignedTokens = true,
        ValidAlgorithms = ["EdDSA"], ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = "sub", RoleClaimType = "ignored-provider-role"
    };

    public async Task<(ClaimsIdentity Identity, DateTimeOffset Expires)?> ValidateAsync(string token, CancellationToken ct)
    {
        if (Configuration is null || token.Length > 16384) return null;
        var parameters = Parameters;
        parameters.IssuerSigningKeys = (await Configuration.GetConfigurationAsync(ct)).SigningKeys;
        var handler = new JsonWebTokenHandler { MapInboundClaims = false, MaximumTokenSizeInBytes = 16384 };
        var result = await handler.ValidateTokenAsync(token, parameters);
        if (!result.IsValid && result.Exception is SecurityTokenSignatureKeyNotFoundException)
        {
            Configuration.RequestRefresh();
            parameters.IssuerSigningKeys = (await Configuration.GetConfigurationAsync(ct)).SigningKeys;
            result = await handler.ValidateTokenAsync(token, parameters);
        }
        if (!result.IsValid || string.IsNullOrWhiteSpace(result.ClaimsIdentity.FindFirst("sub")?.Value)) return null;
        return (result.ClaimsIdentity, new DateTimeOffset(result.SecurityToken.ValidTo, TimeSpan.Zero));
    }
}

public sealed class EdDsaJwksRetriever(string issuer) : IConfigurationRetriever<OpenIdConnectConfiguration>
{
    public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(string address, IDocumentRetriever retriever, CancellationToken ct)
    {
        var json = await retriever.GetDocumentAsync(address, ct);
        if (json.Length > 65536) throw new InvalidOperationException("JWKS exceeds limit.");
        using var document = JsonDocument.Parse(json);
        var configuration = new OpenIdConnectConfiguration { Issuer = issuer };
        foreach (var key in document.RootElement.GetProperty("keys").EnumerateArray())
        {
            if (key.GetProperty("kty").GetString() != "OKP" || key.GetProperty("crv").GetString() != "Ed25519") continue;
            if (key.TryGetProperty("alg", out var alg) && alg.GetString() != "EdDSA") continue;
            if (key.TryGetProperty("use", out var use) && use.GetString() != "sig") continue;
            var bytes = Base64UrlEncoder.DecodeBytes(key.GetProperty("x").GetString()!);
            if (bytes.Length != 32) throw new InvalidOperationException("Invalid Ed25519 public key.");
            configuration.SigningKeys.Add(new EdDsaSecurityKey(EdDsa.Create(new EdDsaParameters("Ed25519") { X = bytes }))
                { KeyId = key.GetProperty("kid").GetString() });
        }
        if (configuration.SigningKeys.Count == 0) throw new InvalidOperationException("No trusted Ed25519 signing keys.");
        return configuration;
    }
}
