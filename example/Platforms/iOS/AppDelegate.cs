using Foundation;
using UIKit;
using DatadogSdk.iOS.Binding;

namespace example;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
	{
		// Initialize Datadog SDK (use dummy token for validation - won't send real data)
		var initialized = DatadogWrapper.Initialize(
			"CLIENT_TOKEN",
			"ENV",
			"datadog-maui-test"
		);

		Console.WriteLine($"[Datadog] iOS SDK initialized: {initialized}");

		// Enable and test logging
		LogsWrapper.EnableLogs();
		LogsWrapper.LogInfo("iOS binding validation - LogInfo works!");
		LogsWrapper.LogDebug("iOS binding validation - LogDebug works!");
		LogsWrapper.LogWarn("iOS binding validation - LogWarn works!");
		LogsWrapper.LogError("iOS binding validation - LogError works!");

		Console.WriteLine("[Datadog] iOS logging test complete");

		return base.FinishedLaunching(application, launchOptions);
	}
}
