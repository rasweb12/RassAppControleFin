using RassAppControleFin.Mobile.ViewModels;

namespace RassAppControleFin.Mobile.Views;

public sealed class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;
    private readonly Label _state = new() { TextColor = Color.FromArgb("#5B646B"), LineBreakMode = LineBreakMode.WordWrap };
    private readonly VerticalStackLayout _metrics = new() { Spacing = 12 };

    public DashboardPage(DashboardViewModel viewModel)
    {
        _viewModel = viewModel;
        Title = "Resumo";
        var refresh = new Button { Text = "Atualizar", CornerRadius = 4 };
        refresh.Clicked += async (_, _) => await LoadAsync();
        Content = new ScrollView { Content = new VerticalStackLayout
        {
            Padding = 20, Spacing = 14,
            Children =
            {
                new Label { Text = "Resumo do mes", FontSize = 22, FontAttributes = FontAttributes.Bold },
                _state, refresh, _metrics
            }
        }};
    }

    protected override async void OnAppearing() { base.OnAppearing(); await LoadAsync(); }

    private async Task LoadAsync()
    {
        await _viewModel.RefreshAsync();
        _state.Text = _viewModel.State;
        _metrics.Children.Clear();
        foreach (var metric in _viewModel.Metrics)
        {
            var values = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = metric.Currency, FontAttributes = FontAttributes.Bold, FontSize = 17 },
                    new Label { Text = $"Patrimonio: {metric.Currency} {metric.NetWorth:N2}" },
                    new Label { Text = $"Receitas: {metric.Currency} {metric.Income:N2}  Despesas: {metric.Currency} {metric.Expense:N2}" },
                    new Label { Text = $"Economia: {metric.SavingsRate:P1}  Orcamento usado: {metric.BudgetUsed:P1}" },
                    new Label { Text = $"Metas ativas: {metric.ActiveGoals}", TextColor = Color.FromArgb("#5B646B") }
                }
            };
            _metrics.Children.Add(values);
        }
    }
}
