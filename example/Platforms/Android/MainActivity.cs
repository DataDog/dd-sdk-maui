using Android.App;
using Android.Content.PM;
using Android.OS;
using DatadogSdk.Android.Binding;

namespace example;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);

		// Initialize Datadog SDK (use dummy token for validation)
		// Note: Android requires Context parameter
		var initialized = DatadogWrapper.Initialize(
			context: this, // Android Context
			clientToken: "CLIENT_TOKEN",
			environment: "ENV",
			service: "datadog-maui-test",
			site: "us1"
		);

		Android.Util.Log.Info("Datadog", $"Android SDK initialized: {initialized}");

		// Enable and test logging
		LogsWrapper.EnableLogs();
		LogsWrapper.LogInfo("Android binding validation - LogInfo works!");
		LogsWrapper.LogDebug("Android binding validation - LogDebug works!");
		LogsWrapper.LogWarn("Android binding validation - LogWarn works!");
		LogsWrapper.LogError("Android binding validation - LogError works!");

		Android.Util.Log.Info("Datadog", "Android logging test complete");
	}
}
