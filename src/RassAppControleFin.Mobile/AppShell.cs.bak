using RassAppControleFin.Mobile.Views;

namespace RassAppControleFin.Mobile;

public sealed class AppShell : Shell
{
    public AppShell(FoundationPage page, DashboardPage dashboard, AccountsPage accounts, IdentityPage identity)
    {
        var tab = new Tab { Title = "Inicio", Route = "home" };
        tab.Items.Add(new ShellContent { Content = page, Route = "status" });
        var tabs = new TabBar();
        tabs.Items.Add(tab);
        var dashboardTab = new Tab { Title = "Resumo", Route = "dashboard" };
        dashboardTab.Items.Add(new ShellContent { Content = dashboard, Route = "dashboard-summary" });
        tabs.Items.Add(dashboardTab);
        var accountsTab = new Tab { Title = "Contas", Route = "accounts" };
        accountsTab.Items.Add(new ShellContent { Content = accounts, Route = "accounts-list" });
        tabs.Items.Add(accountsTab);
        var profile = new Tab { Title = "Perfil", Route = "profile" };
        profile.Items.Add(new ShellContent { Content = identity, Route = "identity" });
        tabs.Items.Add(profile);
        Items.Add(tabs);
    }
}
