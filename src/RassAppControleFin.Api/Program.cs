using System.Threading.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RassAppControleFin.Api.Http;
using RassAppControleFin.Application;
using RassAppControleFin.Application.Behaviors;
using RassAppControleFin.Application.Features.System;
using RassAppControleFin.Contracts;
using RassAppControleFin.Infrastructure;
using RassAppControleFin.Infrastructure.Persistence;
using RassAppControleFin.SharedKernel.Results;
using RassAppControleFin.Api.Identity;
using RassAppControleFin.Infrastructure.Identity;
using RassAppControleFin.Application.Abstractions.Identity;
using RassAppControleFin.Security;
using RassAppControleFin.MultiTenancy;
using RassAppControleFin.Api.Finance;
using RassAppControleFin.Api.AI;
using RassAppControleFin.Api.Synchronization;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 65536);
if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddApplication().AddInfrastructure();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<RequestIdentity>();
builder.Services.AddScoped<ICurrentUser>(p => p.GetRequiredService<RequestIdentity>());
builder.Services.AddScoped<IUserContext>(p => p.GetRequiredService<RequestIdentity>());
builder.Services.AddScoped<ITenantContext>(p => p.GetRequiredService<RequestIdentity>());
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<NeonTokenValidator>((options, trust) =>
    {
        options.TokenValidationParameters = trust.Parameters;
        options.ConfigurationManager = trust.Configuration;
        options.IncludeErrorDetails = false;
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var subject = context.Principal?.FindFirst("sub")?.Value;
                var header = context.Request.Headers["X-App-Session"];
                var bearer = context.Request.Headers.Authorization.ToString();
                if (string.IsNullOrWhiteSpace(subject) || header.Count != 1 || !Guid.TryParse(header[0], out var id)
                    || !bearer.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    || !await context.HttpContext.RequestServices.GetRequiredService<IIdentityStore>()
                        .IsSessionActiveAsync(id, subject, bearer[7..], context.HttpContext.RequestAborted))
                    context.Fail("Invalid application session.");
            }
        };
    });
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
foreach (var permission in typeof(Permissions).GetFields().Select(f => (string)f.GetRawConstantValue()!))
    builder.Services.AddAuthorizationBuilder().AddPolicy(permission, policy => policy.RequireAuthenticatedUser()
        .RequireAssertion(ctx => ctx.Resource is HttpContext http
            && http.RequestServices.GetRequiredService<IUserContext>().Permissions.Contains(permission)));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 15, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("public-api", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60, Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0, AutoReplenishment = true
        }));
    options.AddPolicy("ai", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddHealthChecks()
    .AddCheck("process", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<DatabaseHealthCheck>("postgresql", tags: ["ready"], timeout: TimeSpan.FromSeconds(15));

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("RassAppControleFin.Api"))
    .WithTracing(tracing =>
    {
        tracing.AddSource(ApplicationTelemetry.SourceName, TenantTransactionFactory.TelemetrySourceName, "RassAppControleFin.Gemini").AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            tracing.AddOtlpExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("RassAppControleFin.Gemini").AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            metrics.AddOtlpExporter();
    });

var app = builder.Build();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Correlation-ID"] = context.TraceIdentifier;
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Cache-Control"] = "no-store";
    await next(context);
});
app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment())
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "RassAppControleFin v1"));
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();
app.MapIdentity();
app.MapFinance();
app.MapSync();
app.MapAi();

app.MapHealthChecks("/health", new HealthCheckOptions()).AllowAnonymous();
app.MapHealthChecks("/health/live", new HealthCheckOptions
    { Predicate = check => check.Tags.Contains("live") }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
    { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();

app.MapGet("/api/v1/system", async (ISender sender, HttpContext context,
    CancellationToken cancellationToken) =>
{
    var result = await sender.Send(new GetSystemStatusQuery(), cancellationToken);
    var response = result.IsSuccess
        ? Result<SystemStatusDto>.Success(new(result.Value.Name, result.Value.Version, result.Value.Stage))
        : Result<SystemStatusDto>.Failure(result.Error!);
    return response.ToHttp(context);
}).AllowAnonymous().RequireRateLimiting("public-api").WithName("GetSystemStatus")
  .Produces<ApiResponse<SystemStatusDto>>();

// Protected probe validates the deny-by-default boundary without exposing business data.
app.MapGet("/api/v1/session", () => Results.NoContent()).RequireAuthorization()
    .RequireRateLimiting("public-api");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.Run();

public partial class Program;
