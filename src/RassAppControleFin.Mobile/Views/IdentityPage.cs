using RassAppControleFin.Mobile.Services;

namespace RassAppControleFin.Mobile.Views;

public sealed class IdentityPage : ContentPage
{
    private readonly MobileSessionClient _client;
    private readonly Label _status = new() { LineBreakMode = LineBreakMode.WordWrap };
    private readonly Entry _email = new() { Placeholder = "E-mail", Keyboard = Keyboard.Email };
    private readonly Entry _password = new() { Placeholder = "Senha", IsPassword = true };
    private readonly Entry _name = new() { Placeholder = "Nome" };
    private readonly Entry _endpoint = new() { Placeholder = "API HTTPS", Keyboard = Keyboard.Url };
    private readonly Button _login = new() { Text = "Entrar", CornerRadius = 4 };
    private readonly Button _signup = new() { Text = "Criar conta", CornerRadius = 4 };
    private readonly Button _logout = new() { Text = "Sair", CornerRadius = 4 };
    public IdentityPage(MobileSessionClient client)
    {
        _client = client; Title = "Perfil"; _endpoint.Text = client.BaseUrl;
        _login.Clicked += async (_, _) => await Authenticate(false);
        _signup.Clicked += async (_, _) => await Authenticate(true);
        _logout.Clicked += async (_, _) =>
        {
            _logout.IsEnabled = false;
            try { _status.Text = await client.LogoutAsync() ? "Sessao encerrada." : "Nao foi possivel encerrar a sessao. Tente com conexao."; }
            finally { _logout.IsEnabled = true; UpdateState(); }
        };
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 20, Spacing = 12,
            Children = { new Label { Text = "RASS Controle Financeiro", FontSize = 22 }, _status, _endpoint,
                _email, _password, _login, _name, _signup, _logout } } };
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var restored = await _client.RestoreAsync();
        if (restored && Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            await _client.SelectTenantAsync();
        UpdateState();
        _status.Text = Connectivity.Current.NetworkAccess == NetworkAccess.Internet
            ? _client.IsAuthenticated ? "Sessao restaurada." : "Entre na sua conta." : "Sem conexao.";
    }
    private async Task Authenticate(bool signup)
    {
        _login.IsEnabled = _signup.IsEnabled = false;
        try
        {
            _client.BaseUrl = _endpoint.Text?.Trim() ?? "";
            var result = await _client.LoginAsync(_email.Text ?? "", _password.Text ?? "", signup ? _name.Text ?? "" : null);
            _status.Text = result?.Success == true ? "Conectado." : result?.Error?.Message ?? "Servico indisponivel.";
        }
        catch (ArgumentException) { _status.Text = "Informe uma API HTTPS valida."; }
        finally { _password.Text = ""; UpdateState(); }
    }
    private void UpdateState()
    {
        _login.IsEnabled = _signup.IsEnabled = !_client.IsAuthenticated;
        _email.IsVisible = _password.IsVisible = _name.IsVisible = _signup.IsVisible = _login.IsVisible = !_client.IsAuthenticated;
        _logout.IsVisible = _client.IsAuthenticated;
    }
}
