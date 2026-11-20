using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RassAppControleFin.Infrastructure.Configuration;
using RassAppControleFin.Infrastructure.Identity;
using Xunit;

namespace RassAppControleFin.IntegrationTests;

public sealed class NeonAuthDiagnosticsTests
{
    private const string Secret = "private-canary@example.invalid/password-cookie-token";
    private const string Cookie = "synthetic-cookie-123456";
    private const string Correlation = "e17-local-correlation";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Provider_failure_preserves_unavailable_and_logs_only_metadata(bool signUp)
    {
        var (service, log) = Create(_ => Reply(HttpStatusCode.ServiceUnavailable, Secret));
        var result = signUp
            ? await service.SignUpAsync(Secret, Secret, Secret, default)
            : await service.SignInAsync(Secret, Secret, default);

        Assert.Equal("AUTH_UNAVAILABLE", result.Error!.Code);
        AssertMetadata(log, "authenticate-send", 503, "provider-response");
        Assert.Equal(signUp ? "sign-up/email" : "sign-in/email", log.Fields["Operation"]);
    }

    [Theory]
    [InlineData("connection", "connection", "HttpRequestException")]
    [InlineData("timeout", "timeout", "TimeoutRejectedException")]
    [InlineData("circuit", "circuit-open", "BrokenCircuitException")]
    public async Task Transport_failure_does_not_log_exception_message(string kind, string failure, string type)
    {
        Exception error = kind switch
        {
            "connection" => new HttpRequestException(HttpRequestError.NameResolutionError, Secret),
            "timeout" => new Polly.Timeout.TimeoutRejectedException(Secret),
            _ => new Polly.CircuitBreaker.BrokenCircuitException(Secret)
        };
        var (service, log) = Create(_ => throw error);

        var result = await service.SignInAsync(Secret, Secret, default);

        Assert.Equal("AUTH_UNAVAILABLE", result.Error!.Code);
        AssertMetadata(log, "authenticate-send", null, failure);
        Assert.Equal(type, log.Fields["ExceptionType"]);
        if (kind == "connection")
            Assert.Equal("NameResolutionError", log.Fields["TransportError"]);
    }

    [Fact]
    public async Task Cancellation_is_logged_and_still_propagates()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var (service, log) = Create(_ => throw new OperationCanceledException(Secret, cancellation.Token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.SignInAsync(Secret, Secret, cancellation.Token));

        AssertMetadata(log, "authenticate-send", null, "caller-cancelled");
    }

    [Fact]
    public async Task Invalid_configuration_is_distinguished_from_transport_failure()
    {
        var (service, log) = Create(_ => throw new Exception("Network must not run"), "");

        var result = await service.SignInAsync(Secret, Secret, default);

        Assert.Equal("AUTH_UNAVAILABLE", result.Error!.Code);
        AssertMetadata(log, "request-configuration", null, "processing");
        Assert.Equal("InvalidOperationException", log.Fields["ExceptionType"]);
    }

    [Theory]
    [InlineData("session-send", 503, "AUTH_REJECTED")]
    [InlineData("session-parse", 200, "AUTH_UNAVAILABLE")]
    [InlineData("token-send", 502, "AUTH_REJECTED")]
    [InlineData("token-parse", 200, "AUTH_UNAVAILABLE")]
    public async Task Login_continuation_preserves_result_and_identifies_failing_stage(
        string stage, int status, string errorCode)
    {
        var (service, log) = Create(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("sign-in/email"))
            {
                var response = Reply(HttpStatusCode.OK, "{}");
                response.Headers.Add("Set-Cookie", "__Secure-neon-auth.session_token=" + Cookie + "; Secure; Path=/");
                return response;
            }
            var session = request.RequestUri.AbsolutePath.EndsWith("get-session");
            if (session && stage.StartsWith("session-", StringComparison.Ordinal))
                return Reply((HttpStatusCode)status, Secret);
            return session
                ? Reply(HttpStatusCode.OK, """{"session":{"userId":"synthetic-user","expiresAt":"2099-01-01T00:00:00Z"}}""")
                : Reply((HttpStatusCode)status, Secret);
        });

        var result = await service.SignInAsync(Secret, Secret, default);

        Assert.Equal(errorCode, result.Error!.Code);
        AssertMetadata(log, stage, status, stage.EndsWith("parse") ? "processing" : "provider-response");
    }

    [Fact]
    public async Task Existing_account_response_is_preserved_without_payload_logging()
    {
        var (service, log) = Create(_ => Reply(HttpStatusCode.UnprocessableEntity,
            """{"code":"USER_ALREADY_EXISTS","message":"private-canary@example.invalid/password-cookie-token"}"""));

        var result = await service.SignUpAsync(Secret, Secret, Secret, default);

        Assert.Equal("ACCOUNT_ALREADY_EXISTS", result.Error!.Code);
        Assert.Empty(log.Fields);
    }

    [Theory]
    [InlineData(401, "authenticate-send")]
    [InlineData(200, "authenticate-cookie")]
    public async Task Credential_rejection_and_missing_cookie_keep_existing_denial(int status, string stage)
    {
        var (service, log) = Create(_ => Reply((HttpStatusCode)status, Secret));

        var result = await service.SignInAsync(Secret, Secret, default);

        Assert.Equal("AUTH_REJECTED", result.Error!.Code);
        AssertMetadata(log, stage, status, "provider-response");
    }

    [Fact]
    public async Task Provider_status_from_transport_exception_is_preserved_without_details()
    {
        var (service, log) = Create(_ =>
            throw new HttpRequestException(Secret, null, HttpStatusCode.BadGateway));

        var result = await service.SignInAsync(Secret, Secret, default);

        Assert.Equal("AUTH_UNAVAILABLE", result.Error!.Code);
        AssertMetadata(log, "authenticate-send", 502, "connection");
    }
    private static HttpResponseMessage Reply(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static (NeonAuthService Service, CapturingLogger Log) Create(
        Func<HttpRequestMessage, HttpResponseMessage> handler, string baseUrl = "https://auth.example.invalid")
    {
        var factory = new ClientFactory(new Handler(handler));
        var options = Options.Create(new NeonOptions { AuthBaseUrl = baseUrl, Origin = "https://app.example.invalid" });
        var log = new CapturingLogger();
        var context = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        context.HttpContext.TraceIdentifier = Correlation;
        return (new NeonAuthService(factory, options, new NeonTokenValidator(options, factory), log, context), log);
    }

    private static void AssertMetadata(CapturingLogger log, string stage, int? status, string failure)
    {
        Assert.Equal(1701, log.Event.Id);
        Assert.Equal(stage, log.Fields["Stage"]);
        Assert.Equal(status, log.Fields["ProviderStatus"]);
        Assert.Equal(failure, log.Fields["Failure"]);
        Assert.Equal(Correlation, log.Fields["CorrelationId"]);
        Assert.Null(log.Exception);
        Assert.DoesNotContain(Secret, log.Text);
        Assert.DoesNotContain(Cookie, log.Text);
        Assert.DoesNotContain("https://", log.Text);
    }

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(handler(request));
    }

    private sealed class ClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class CapturingLogger : ILogger<NeonAuthService>
    {
        public Dictionary<string, object?> Fields { get; } = [];
        public string Text { get; private set; } = "";
        public Exception? Exception { get; private set; }
        public EventId Event { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Event = eventId;
            Exception = exception;
            Text = formatter(state, exception);
            foreach (var field in (IEnumerable<KeyValuePair<string, object?>>)(object)state!)
                Fields[field.Key] = field.Value;
        }
    }
}