using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using RassAppControleFin.Mobile.Views;

namespace RassAppControleFin.Mobile;

public sealed class AppShell : Shell
{
    public AppShell(
        FoundationPage page,
        DashboardPage dashboard,
        AccountsPage accounts,
        IdentityPage identity)
    {
        Shell.SetNavBarIsVisible(this, true);

	Shell.SetBackgroundColor(this, Colors.White);
	Shell.SetForegroundColor(this, Colors.White);
	Shell.SetTitleColor(this, Colors.White);
	Shell.SetUnselectedColor(this, Colors.LightGray);

	Shell.SetTabBarBackgroundColor(this, Color.FromArgb("#293E52"));
	Shell.SetTabBarForegroundColor(this, Colors.White);
	Shell.SetTabBarTitleColor(this, Colors.White);
	Shell.SetTabBarUnselectedColor(this, Colors.LightGray);

        var tabs = new TabBar();

        tabs.Items.Add(new Tab
        {
            Title = "Início",
            Route = "home",
            Items =
            {
                new ShellContent
                {
                    Title = "Início",
                    Route = "status",
                    Content = page
                }
            }
        });

        tabs.Items.Add(new Tab
        {
            Title = "Resumo",
            Route = "dashboard",
            Items =
            {
                new ShellContent
                {
                    Title = "Resumo",
                    Route = "dashboard-summary",
                    Content = dashboard
                }
            }
        });

        tabs.Items.Add(new Tab
        {
            Title = "Contas",
            Route = "accounts",
            Items =
            {
                new ShellContent
                {
                    Title = "Contas",
                    Route = "accounts-list",
                    Content = accounts
                }
            }
        });

        tabs.Items.Add(new Tab
        {
            Title = "Perfil",
            Route = "profile",
            Items =
            {
                new ShellContent
                {
                    Title = "Perfil",
                    Route = "identity",
                    Content = identity
                }
            }
        });

        Items.Add(tabs);
    }
}