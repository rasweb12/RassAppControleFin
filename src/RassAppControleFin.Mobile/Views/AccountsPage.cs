using RassAppControleFin.Mobile.ViewModels;

namespace RassAppControleFin.Mobile.Views;

public sealed class AccountsPage : ContentPage
{
    private readonly AccountsViewModel _viewModel;
    private readonly Label _state = new() { TextColor = Color.FromArgb("#5B646B"), LineBreakMode = LineBreakMode.WordWrap };
    private readonly VerticalStackLayout _list = new() { Spacing = 8 };
    public AccountsPage(AccountsViewModel viewModel)
    {
        _viewModel = viewModel;
        Title = "Contas";
        var refresh = new Button { Text = "Atualizar", CornerRadius = 4 };
        refresh.Clicked += async (_, _) => await LoadAsync();
        Content = new ScrollView { Content = new VerticalStackLayout
        {
            Padding = 20, Spacing = 14,
            Children = { new Label { Text = "Contas", FontSize = 22, FontAttributes = FontAttributes.Bold },
                _state, refresh, _list }
        }};
    }
    protected override async void OnAppearing() { base.OnAppearing(); await LoadAsync(); }
    private async Task LoadAsync()
    {
        await _viewModel.RefreshAsync();
        _state.Text = _viewModel.State;
        _list.Children.Clear();
        foreach (var account in _viewModel.Accounts)
        {
            var name = new Label { Text = account.Name, FontSize = 16 };
            var balance = new Label { Text = $"{account.Currency} {account.Balance:N2}", FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.End };
            Grid.SetColumn(balance, 1);
            _list.Children.Add(new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new(GridLength.Star), new(GridLength.Auto)
                },
                Children = { name, balance }
            });
        }
    }
}
