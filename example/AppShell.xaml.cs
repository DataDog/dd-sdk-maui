namespace example;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("DetailPage", typeof(DetailPage));
    }
}
