using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http.Resilience;
using RassAppControleFin.Admin.Web.Components;
using RassAppControleFin.Admin.Web.Services;
using RassAppControleFin.Presentation;

var builder = WebApplication.CreateBuilder(args);
if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddProblemDetails();
builder.Services.AddOptions<ApiOptions>().BindConfiguration("Api")
    .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps
            || (builder.Environment.IsDevelopment() && uri.IsLoopback && uri.Scheme == Uri.UriSchemeHttp)),
        "Api:BaseUrl must use HTTPS (HTTP loopback is allowed in Development).")
    .ValidateOnStart();
builder.Services.AddHttpClient<SystemClient>((services, client) =>
{
    var options = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
}).AddStandardResilienceHandler(options => options.Retry.DisableForUnsafeHttpMethods());
builder.Services.AddHealthChecks();
builder.AddServerAuthentication("Admin");

var app = builder.Build();
app.UseExceptionHandler();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapServerAuthentication();
app.MapHealthChecks("/health", new HealthCheckOptions());
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
