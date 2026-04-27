using DatadogSdk.Maui;

namespace example;

public partial class DetailPage : ContentPage
{
    public DetailPage()
    {
        InitializeComponent();

        DdRum.AddViewAttribute("navigation_type", "shell");
    }

    private async void OnGoBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..", true);
    }
}
