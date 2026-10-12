using RassAppControleFin.Mobile.Navigation;
using RassAppControleFin.Mobile.ViewModels;
using RassAppControleFin.Mobile.Views;
using RassAppControleFin.Mobile.Services;
using Microsoft.Extensions.Http.Resilience;
using RassAppControleFin.Synchronization;

namespace RassAppControleFin.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder().UseMauiApp<App>();
        builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
        builder.Services.AddSingleton<FoundationViewModel>();
        builder.Services.AddSingleton<FoundationPage>();
        builder.Services.AddSingleton<DashboardViewModel>();
        builder.Services.AddSingleton<DashboardPage>();
        builder.Services.AddSingleton<AccountsViewModel>();
        builder.Services.AddSingleton<AccountsPage>();
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<MobileSyncScope>();
        builder.Services.AddSingleton<SqliteSyncStore>();
        builder.Services.AddSingleton<ISyncQueue>(p => p.GetRequiredService<SqliteSyncStore>());
        builder.Services.AddSingleton<IOutboxStore>(p => p.GetRequiredService<SqliteSyncStore>());
        builder.Services.AddSingleton<IInboxStore>(p => p.GetRequiredService<SqliteSyncStore>());
        builder.Services.AddSingleton<ISyncStateStore>(p => p.GetRequiredService<SqliteSyncStore>());
        builder.Services.AddSingleton<MobileSessionClient>();
        builder.Services.AddSingleton<IdentityPage>();
        builder.Services.AddHttpClient("mobile-api").RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false })
            .AddStandardResilienceHandler(options => options.Retry.DisableForUnsafeHttpMethods());
        return builder.Build();
    }
}
