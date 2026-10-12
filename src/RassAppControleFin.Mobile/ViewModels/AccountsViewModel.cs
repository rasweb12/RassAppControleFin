using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RassAppControleFin.Contracts;
using RassAppControleFin.Mobile.Services;

namespace RassAppControleFin.Mobile.ViewModels;

public sealed class AccountsViewModel(MobileSessionClient client) : ObservableObject
{
    private string _state = "Entre na sua conta para ver suas contas.";
    private IReadOnlyList<AccountDto> _accounts = [];
    public string State { get => _state; private set => SetProperty(ref _state, value); }
    public IReadOnlyList<AccountDto> Accounts { get => _accounts; private set => SetProperty(ref _accounts, value); }
    public IAsyncRelayCommand RefreshCommand => new AsyncRelayCommand(RefreshAsync);
    public async Task RefreshAsync()
    {
        if (!client.IsAuthenticated) { Accounts = []; State = "Entre na sua conta para ver suas contas."; return; }
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        { Accounts = []; State = "Sem conexao. Alteracoes locais aguardam sincronizacao."; return; }
        if (client.TenantId is null && !await client.SelectTenantAsync())
        { Accounts = []; State = "Nao foi possivel selecionar o tenant."; return; }
        State = "Carregando contas...";
        var response = await client.SendAsync<PageDto<AccountDto>>(HttpMethod.Get, "/api/v1/accounts?page=1&pageSize=50");
        Accounts = response?.Data?.Items ?? [];
        State = response?.Success == true
            ? Accounts.Count == 0 ? "Nenhuma conta cadastrada." : ""
            : response?.Error?.Message ?? "Nao foi possivel carregar as contas.";
    }
}
