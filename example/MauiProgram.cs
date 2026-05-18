/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

using DatadogSdk.Maui;
using DatadogSdk.Maui.Configuration;
using DatadogSdk.Maui.Hosting;
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

        // Load Datadog credentials
        var config = AppSettings.Load();
        var clientToken = config["Datadog"]!["ClientToken"]!.ToString();
        var environment = config["Datadog"]!["Environment"]!.ToString();
        var applicationId = config["Datadog"]!["ApplicationId"]!.ToString();

        builder
            .UseDatadogSdk(new DdSdkConfiguration
            {
                ClientToken = clientToken,
                Environment = environment,
                Service = "datadog-maui-test",
                Site = DatadogSite.Us1,
                TrackingConsent = TrackingConsent.Granted,
                Verbosity = SdkVerbosity.DEBUG,
                UploadFrequency = UploadFrequency.Frequent,
            })
            .UseDatadogLogs(new DdLogsConfiguration { })
            .UseDatadogTrace(new DdTraceConfiguration { })
            .UseDatadogRum(new DdRumConfiguration
            {
                ApplicationId = applicationId,
                SessionSampleRate = 100.0,
                TelemetrySampleRate = 100.0,
                ResourceTraceSampleRate = 100.0,
                TrackFrustrations = true,
                TrackBackgroundEvents = true,
                TrackMemoryWarnings = true,
                NativeLongTaskThresholdMs = 200.0,
                VitalsUpdateFrequency = VitalsUpdateFrequency.Average,
                FirstPartyHosts = new List<FirstPartyHost>
                {
                    new() { Match = "datadoghq.com", HeaderTypes = new List<TracingHeaderType> { TracingHeaderType.Datadog, TracingHeaderType.TraceContext } }
                },
                ErrorEventMapper = e =>
                {
                    e.Context["processedByErrorMapper"] = true;
                    return e;
                },
            })
            .UseDatadogSessionReplay(new SessionReplayConfiguration
            {
                ReplaySampleRate = 100.0,
                TextAndInputPrivacyLevel = TextAndInputPrivacy.MaskSensitiveInputs,
                ImagePrivacyLevel = ImagePrivacy.MaskNone,
                TouchPrivacyLevel = TouchPrivacy.Show,
            });

        return builder.Build();
    }
}
