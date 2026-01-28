using Datadog.Maui;
using Microsoft.Extensions.Logging;

namespace MauiApp1;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseDatadog(new DatadogConfiguration
			{
				ClientToken = "pub_test_token_for_verification",
				Env = "development",
				Site = DatadogSite.US1,
				TrackingConsent = TrackingConsent.Granted
			})
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Verify Datadog initialization
		System.Diagnostics.Debug.WriteLine($"Datadog initialized: {DatadogSdk.Instance.IsInitialized}");

		return builder.Build();
	}
}
