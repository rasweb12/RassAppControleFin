using RassAppControleFin.Mobile.ViewModels;

namespace RassAppControleFin.Mobile.Views;

public sealed class FoundationPage : ContentPage
{
    public FoundationPage(FoundationViewModel viewModel)
    {
        Title = "RASS Controle Financeiro";
        BindingContext = viewModel;
        BackgroundColor = Color.FromArgb("#F3F5F6");

        var status = new Label { TextColor = Color.FromArgb("#25604E"), FontSize = 16 };
        status.SetBinding(Label.TextProperty, nameof(FoundationViewModel.ConnectionStatus));
        var refresh = new Button { Text = "Atualizar", CornerRadius = 4 };
        refresh.SetBinding(Button.CommandProperty, nameof(FoundationViewModel.RefreshCommand));
        Content = new VerticalStackLayout
        {
            Padding = 24, Spacing = 20,
            Children =
            {
                new Label { Text = "RASS Controle Financeiro", FontSize = 24,
                    TextColor = Color.FromArgb("#24282B"), FontAttributes = FontAttributes.Bold },
                new Label { Text = "1.0.0-alpha.1", TextColor = Color.FromArgb("#5B646B") },
                status, refresh
            }
        };
    }
}
