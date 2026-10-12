using CommunityToolkit.Mvvm.ComponentModel;
using RassAppControleFin.Contracts;
using RassAppControleFin.Mobile.Services;

namespace RassAppControleFin.Mobile.ViewModels;

public sealed class DashboardViewModel(MobileSessionClient client) : ObservableObject
{
    private string _state = "Entre na sua conta para ver o resumo.";
    private IReadOnlyList<DashboardMetricDto> _metrics = [];

    public string State { get => _state; private set => SetProperty(ref _state, value); }
    public IReadOnlyList<DashboardMetricDto> Metrics { get => _metrics; private set => SetProperty(ref _metrics, value); }

    public async Task RefreshAsync()
    {
        if (!client.IsAuthenticated) { Metrics = []; State = "Entre na sua conta para ver o resumo."; return; }
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            Metrics = [];
            State = "Sem conexao. As alteracoes continuam na fila local para sincronizacao.";
            return;
        }
        if (client.TenantId is null && !await client.SelectTenantAsync())
        {
            Metrics = []; State = "Nao foi possivel selecionar o tenant."; return;
        }

        var from = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddMonths(1);
        State = "Carregando resumo...";
        var response = await client.SendAsync<DashboardDto>(HttpMethod.Get,
            $"/api/v1/dashboard?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}");
        Metrics = response?.Data?.Metrics ?? [];
        State = response?.Success == true
            ? Metrics.Count == 0 ? "Ainda nao ha movimentacoes neste periodo." : ""
            : response?.Error?.Message ?? "Nao foi possivel carregar o resumo.";
    }
}
