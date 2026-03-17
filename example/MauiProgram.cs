using DatadogSdk.Maui;
using DatadogSdk.Maui.Configuration;
using Microsoft.Extensions.Logging;

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
            Site = DatadogSite.Us1,
            TrackingConsent = TrackingConsent.Granted,
            Verbosity = SdkVerbosity.DEBUG,
            // Uncomment to test against a local mock HTTP server (see README for setup):
            // AdditionalConfiguration = new Dictionary<string, object>
            // {
            //     { "_dd.needsClearTextHttp", true }
            // },
        });

        return builder.Build();
    }
}
