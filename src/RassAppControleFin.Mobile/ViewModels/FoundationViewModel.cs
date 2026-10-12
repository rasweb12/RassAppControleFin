using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RassAppControleFin.Mobile.ViewModels;

public sealed class FoundationViewModel : ObservableObject
{
    private string _connectionStatus = "Verificando conexao...";

    public FoundationViewModel()
    {
        RefreshCommand = new RelayCommand(Refresh);
        Refresh();
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        private set => SetProperty(ref _connectionStatus, value);
    }

    public IRelayCommand RefreshCommand { get; }

    private void Refresh() => ConnectionStatus =
        Connectivity.Current.NetworkAccess == NetworkAccess.Internet
            ? "Conexao disponivel"
            : "Offline";
}
