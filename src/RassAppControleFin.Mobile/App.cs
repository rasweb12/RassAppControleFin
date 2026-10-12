namespace RassAppControleFin.Mobile;

public sealed class App(AppShell shell) : Application
{
    protected override Window CreateWindow(IActivationState? activationState) => new(shell);
}
