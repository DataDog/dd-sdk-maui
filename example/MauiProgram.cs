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
            UploadFrequency = UploadFrequency.Frequent,
            // ProxyConfiguration = new ProxyConfiguration
            // {
            //     Type = ProxyType.Http,
            //     Address = "192.168.1.100",
            //     Port = 8080,
            //     // Username = "user",
            //     // Password = "pass",
            // },
            // AdditionalConfiguration = new Dictionary<string, object>
            // {
            //     { "_dd.needsClearTextHttp", true }
            // },
            // FirstPartyHosts = new List<DdFirstPartyHost>
            // {
            //     new() { Match = "datadoghq.com", HeaderTypes = new List<TracingHeaderType> { TracingHeaderType.Datadog, TracingHeaderType.TraceContext } }
            // }
        });

        return builder.Build();
    }
}
