using Microsoft.Extensions.Logging;
using DatadogSdk.Maui;

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
		DdSdk.Initialize(new DdSdkConfiguration
		{
			ClientToken = clientToken,
			Environment = environment,
			Service = "datadog-maui-test",
			Verbosity = SdkVerbosity.DEBUG
		});

		return builder.Build();
	}
}
