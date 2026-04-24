using DatadogSdk.Maui;

namespace example;

public partial class NavDetailPage : ContentPage
{
    public NavDetailPage()
    {
        InitializeComponent();

        DdRum.AddViewAttribute("navigation_type", "navigation_page");
    }

    private async void OnGoBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync(true);
    }
}
