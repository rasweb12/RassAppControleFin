using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using RassAppControleFin.Application.Features.System;
using RassAppControleFin.Contracts;
using RassAppControleFin.SharedKernel.Results;
using Xunit;

namespace RassAppControleFin.IntegrationTests;

public sealed class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public ApiTests(WebApplicationFactory<Program> factory) => _factory = factory.WithWebHostBuilder(builder =>
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?> { ["Database:ConnectionString"] = "" })));

    [Theory]
    [InlineData("/health/live")]
    public async Task Operational_endpoints_are_available_without_auth(string path)
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    public async Task Readiness_is_unhealthy_without_a_configured_database(string path)
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task System_endpoint_has_versioned_contract_and_correlation()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/system");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SystemStatusDto>>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body!.Success);
        Assert.Null(body.Error);
        Assert.Equal("1.0.0-alpha.1", body.Data!.Version);
        Assert.Equal(body.Meta.CorrelationId, response.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
    }

    [Fact]
    public async Task Protected_endpoint_denies_unauthenticated_requests()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/session");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unsigned_token_cannot_establish_identity()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer",
            "eyJhbGciOiJub25lIn0.eyJzdWIiOiJhdHRhY2tlciIsInRlbmFudF9pZCI6ImZha2UifQ.");
        using var response = await client.GetAsync("/api/v1/session");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unexpected_exception_is_redacted()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddTransient<IRequestHandler<GetSystemStatusQuery, Result<FoundationInfo>>, ThrowingHandler>()));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/system");
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("sensitive-test-value", body);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        using var json = JsonDocument.Parse(body);
        Assert.Equal("Falha inesperada.", json.RootElement.GetProperty("title").GetString());
        Assert.True(json.RootElement.TryGetProperty("correlationId", out _));
    }

    [Fact]
    public async Task Pipeline_validation_maps_to_http_400_envelope()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IValidator<GetSystemStatusQuery>, RejectingValidator>()));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/system");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SystemStatusDto>>();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(body!.Success);
        Assert.Equal("VALIDATION_FAILED", body.Error!.Code);
        Assert.Single(body.Error.Details);
    }

    [Fact]
    public async Task Openapi_document_exists_only_in_development()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("paths").TryGetProperty("/api/v1/system", out _));
    }

    [Fact]
    public async Task Swagger_ui_is_available_in_development()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("swagger-ui", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Production_does_not_expose_openapi_or_swagger()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = factory.CreateClient();
        using var document = await client.GetAsync("/openapi/v1.json");
        using var ui = await client.GetAsync("/swagger/index.html");
        Assert.NotEqual(HttpStatusCode.OK, document.StatusCode);
        Assert.NotEqual(HttpStatusCode.OK, ui.StatusCode);
    }

    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.Domain, 422)]
    [InlineData(ErrorType.Infrastructure, 503)]
    public async Task Application_failure_maps_to_http_and_null_data(ErrorType type, int status)
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IRequestHandler<GetSystemStatusQuery, Result<FoundationInfo>>>(
                new FailureHandler(type))));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/system");
        Assert.Equal(status, (int)response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("data").ValueKind);
    }

    public sealed class FailureHandler(ErrorType type) : IRequestHandler<GetSystemStatusQuery, Result<FoundationInfo>>
    {
        public Task<Result<FoundationInfo>> Handle(GetSystemStatusQuery request, CancellationToken cancellationToken)
            => Task.FromResult(Result<FoundationInfo>.Failure(new("TEST_ERROR", "Erro de teste.", type)));
    }

    public sealed class ThrowingHandler : IRequestHandler<GetSystemStatusQuery, Result<FoundationInfo>>
    {
        public Task<Result<FoundationInfo>> Handle(GetSystemStatusQuery request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("sensitive-test-value");
    }

    public sealed class RejectingValidator : AbstractValidator<GetSystemStatusQuery>
    {
        public RejectingValidator()
        {
            RuleFor(query => query).Must(_ => false).WithMessage("Dados invalidos.");
        }
    }
}
