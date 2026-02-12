using DatadogSdk.Maui;

namespace example;

public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage()
	{
		InitializeComponent();
		DdLogs.Enable();
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
