namespace RassAppControleFin.Mobile.Navigation;

public interface INavigationService
{
    Task GoToAsync(string route);
}

public sealed class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route) => Shell.Current.GoToAsync(route);
}
