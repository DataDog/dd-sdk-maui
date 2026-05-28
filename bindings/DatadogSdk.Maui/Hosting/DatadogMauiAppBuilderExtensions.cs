/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using DatadogSdk.Maui.Configuration;
#if IOS || ANDROID
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.LifecycleEvents;
#endif

namespace DatadogSdk.Maui.Hosting
{
    /// <summary>
    /// <see cref="MauiAppBuilder"/> extensions for initializing the Datadog
    /// SDK from <c>MauiProgram.CreateMauiApp()</c>. Native modules enable
    /// synchronously on the chain; the RUM auto-trackers attach at the first
    /// post-launch MAUI lifecycle event so they can subscribe to the
    /// <see cref="Application"/> instance once MAUI has constructed it.
    /// </summary>
    public static class DatadogMauiAppBuilderExtensions
    {
        /// <summary>Initializes the core Datadog SDK.</summary>
        public static MauiAppBuilder UseDatadogSdk(this MauiAppBuilder builder, DdSdkConfiguration configuration)
        {
            DdSdk.Initialize(configuration);
            return builder;
        }

        /// <summary>Enables Datadog Logs. Requires <see cref="UseDatadogSdk"/> to have run first.</summary>
        public static MauiAppBuilder UseDatadogLogs(this MauiAppBuilder builder, DdLogsConfiguration? configuration = null)
        {
            DdLogs.Enable(configuration);
            return builder;
        }

        /// <summary>Enables Datadog Trace. Requires <see cref="UseDatadogSdk"/> to have run first.</summary>
        public static MauiAppBuilder UseDatadogTrace(this MauiAppBuilder builder, DdTraceConfiguration? configuration = null)
        {
            DdTrace.Enable(configuration);
            return builder;
        }

        /// <summary>
        /// Enables Datadog RUM. Native RUM is enabled synchronously so SDK
        /// calls that route through <c>RUMMonitor</c> (e.g. <see cref="DdSdk.AddAttribute"/>
        /// on iOS) work immediately. The MAUI auto-trackers
        /// (<see cref="DdRumConfiguration.AutomaticViewTracking"/>,
        /// <see cref="DdRumConfiguration.AutomaticActionTracking"/>,
        /// <see cref="DdRumConfiguration.AutomaticResourceTracking"/>) attach
        /// at the first post-launch lifecycle event, once MAUI has resolved
        /// <c>IApplication</c> and the platform shim has populated its
        /// platform-application static.
        /// </summary>
        public static MauiAppBuilder UseDatadogRum(this MauiAppBuilder builder, DdRumConfiguration configuration)
        {
            // Enable RUM synchronously so DdSdk.AddAttribute (which routes
            // through RUMMonitor on iOS) works for any setup code that runs
            // after this returns.
            DdRum.EnableCore(configuration);

            var anyAutoTracking = configuration.AutomaticViewTracking
                || configuration.AutomaticActionTracking
                || configuration.AutomaticResourceTracking;
            if (!anyAutoTracking)
            {
                return builder;
            }

#if IOS || ANDROID
            builder.ConfigureLifecycleEvents(lifecycle =>
            {
#if IOS
                // FinishedLaunching fires after MauiUIApplicationDelegate has
                // resolved IApplication.
                lifecycle.AddiOS(ios => ios.FinishedLaunching((application, launchOptions) =>
                {
                    var platformApplication = application.Delegate as IPlatformApplication
                        ?? launchOptions?["application"] as IPlatformApplication;
                    AttachAutoTrackersFromPlatform(platformApplication, configuration);
                    return true;
                }));
#elif ANDROID
                // OnApplicationCreating fires inside MauiApplication.OnCreate
                // — before any Activity is created — so the auto-trackers are
                // ready to observe the very first page lifecycle event.
                lifecycle.AddAndroid(android => android.OnApplicationCreating(application =>
                {
                    AttachAutoTrackersFromPlatform(application as IPlatformApplication, configuration);
                }));
#endif
            });
#else
            // Fallback: Attach immediately
            if (Application.Current != null)
            {
                DdRum.AttachAutoTrackers(Application.Current, configuration);
            }
#endif
            return builder;
        }

        /// <summary>Enables Datadog Session Replay. Requires <see cref="UseDatadogSdk"/> to have run first.</summary>
        public static MauiAppBuilder UseDatadogSessionReplay(this MauiAppBuilder builder, SessionReplayConfiguration configuration)
        {
            DdSessionReplay.Enable(configuration);
            return builder;
        }

#if IOS || ANDROID
        /// <summary>
        /// Resolves the MAUI <see cref="Application"/> from the platform
        /// service provider and hands it to the RUM auto-trackers. Resolution
        /// goes through DI rather than reading <see cref="Application.Current"/>
        /// so it works even when called from a lifecycle event where the
        /// static has not yet been populated by the platform shim.
        /// </summary>
        private static void AttachAutoTrackersFromPlatform(IPlatformApplication? platformApplication, DdRumConfiguration configuration)
        {
            if (platformApplication is null)
            {
                InternalLog.Log(
                    "UseDatadogRum: platform application was not available at lifecycle time; auto-trackers will not be attached.",
                    SdkVerbosity.WARN);
                return;
            }

            var app = platformApplication.Services.GetService<IApplication>();
            if (app is not Application application)
            {
                InternalLog.Log(
                    "UseDatadogRum: resolved IApplication is not a Microsoft.Maui.Controls.Application; auto-trackers will not be attached.",
                    SdkVerbosity.WARN);
                return;
            }

            DdRum.AttachAutoTrackers(application, configuration);
        }
#endif
    }
}
