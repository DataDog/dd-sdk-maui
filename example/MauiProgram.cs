using Microsoft.Extensions.Logging;
#if ANDROID
using DatadogSdk.Android.Binding;
#elif IOS
using DatadogSdk.iOS.Binding;
#endif

namespace example;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Load configuration
		var config = AppSettings.Load();
		var clientToken = config["Datadog"]!["ClientToken"]!.ToString();
		var environment = config["Datadog"]!["Environment"]!.ToString();

		// Initialize Datadog SDK
#if ANDROID
		var initialized = DatadogWrapper.Initialize(
			context: Android.App.Application.Context,
			clientToken: clientToken,
			environment: environment,
			service: "datadog-maui-test",
			site: "us1"
		);
#elif IOS
		var initialized = DatadogWrapper.Initialize(
			clientToken,
			environment,
			"datadog-maui-test"
		);
#endif

		Console.WriteLine($"[Datadog] SDK initialized: {initialized}");

		return builder.Build();
	}
}
