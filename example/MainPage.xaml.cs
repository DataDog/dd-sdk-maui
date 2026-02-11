#if ANDROID
using DatadogSdk.Android.Binding;
#elif IOS
using DatadogSdk.iOS.Binding;
#endif

namespace example;

public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage()
	{
		InitializeComponent();
		DdLogs.EnableLogs();
	}

	private void OnSendLogsClicked(object? sender, EventArgs e)
	{
		var platform = DeviceInfo.Platform.ToString();
		DdLogs.LogInfo($"DDLogs - {platform} - LogInfo");
		DdLogs.LogDebug($"DDLogs - {platform} - LogDebug");
		DdLogs.LogWarn($"DDLogs - {platform} - LogWarn");
		DdLogs.LogError($"DDLogs - {platform} - LogError");

		Console.WriteLine("[Datadog] Logging test complete");
	}
}
