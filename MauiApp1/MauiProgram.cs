using Datadog.Maui;
using Microsoft.Extensions.Logging;

namespace MauiApp1;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		// Replace with your Datadog credentials from https://app.datadoghq.com/rum/application/create
		var clientToken = "";
		var rumAppId = "";

		builder
			.UseMauiApp<App>()
			.UseDatadog(new DatadogConfiguration
			{
				ClientToken = clientToken,
				Env = "development",
				Site = DatadogSite.US1,  // Change to your Datadog site (US1, EU1, US3, US5, AP1)
				RumApplicationId = rumAppId,
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
