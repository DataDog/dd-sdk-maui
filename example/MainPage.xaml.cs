using DatadogSdk.Maui;
using DatadogSdk.Maui.Configuration;

namespace example;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        var logsConfiguration = new DdLogsConfiguration
        {
            //CustomEndpoint = "http://custom.endpoint"
        };
        DdLogs.Enable(logsConfiguration);
    }

    private void OnSendLogsClicked(object? sender, EventArgs e)
    {
        var platform = DeviceInfo.Platform.ToString();
        DdLogs.Info($"DDLogs - {platform} - LogInfo");
        DdLogs.Debug($"DDLogs - {platform} - LogDebug");
        DdLogs.Warn($"DDLogs - {platform} - LogWarn");
        DdLogs.Error($"DDLogs - {platform} - LogError");
    }
}
