/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Maui.Controls;

namespace DatadogSdk.Maui.Configuration
{
    /// <summary>
    /// Configuration for the Datadog RUM module.
    /// </summary>
    public class DdRumConfiguration
    {
        // Required
        /// <summary>
        /// The Datadog RUM application ID.
        /// </summary>
        public required string ApplicationId { get; set; }

        // Sampling
        /// <summary>
        /// Percentage of sessions to track (0.0 to 100.0). Default: 100.0.
        /// </summary>
        public double SessionSampleRate { get; set; } = 100.0;

        /// <summary>
        /// Percentage of telemetry events to send (0.0 to 100.0). Default: 20.0.
        /// </summary>
        public double TelemetrySampleRate { get; set; } = 20.0;

        /// <summary>
        /// Percentage of configuration telemetry events to send, applied on top of
        /// <see cref="TelemetrySampleRate"/> (0.0 to 100.0). Default: 20.0.
        /// Honored on iOS (sets <c>RUM.Configuration.configurationTelemetrySampleRate</c>).
        /// On Android 3.10.0 the native SDK exposes no public setter for this sampler,
        /// so the value is reported in configuration telemetry but the actual sampler
        /// stays at the native default (20%).
        /// </summary>
        public double ConfigurationTelemetrySampleRate { get; set; } = 20.0;

        /// <summary>
        /// Percentage of traced resources to sample for distributed tracing (0.0 to 100.0). Default: 20.0.
        /// </summary>
        public double ResourceTraceSampleRate { get; set; } = 20.0;

        // Tracking flags
        /// <summary>
        /// Enable frustration signal tracking (rage taps, etc.). Default: true.
        /// </summary>
        public bool TrackFrustrations { get; set; } = true;

        /// <summary>
        /// Track RUM events when the app is in the background. Default: false.
        /// </summary>
        public bool TrackBackgroundEvents { get; set; } = false;

        /// <summary>
        /// Enable automatic native view tracking. Default: false.
        /// </summary>
        public bool NativeViewTracking { get; set; } = false;

        /// <summary>
        /// Enable automatic native user interaction tracking. Default: false.
        /// </summary>
        public bool NativeInteractionTracking { get; set; } = false;

        /// <summary>
        /// Track memory warnings. iOS only. Default: false.
        /// </summary>
        public bool TrackMemoryWarnings { get; set; } = false;

        // Thresholds
        /// <summary>
        /// Threshold in milliseconds for native long task detection. Default: 200.0.
        /// </summary>
        public double NativeLongTaskThresholdMs { get; set; } = 200.0;

        /// <summary>
        /// Threshold in seconds for initial resource identification. Optional.
        /// </summary>
        public double? InitialResourceThreshold { get; set; }

        /// <summary>
        /// Threshold in seconds for app hang detection. iOS only. Optional.
        /// </summary>
        public double? AppHangThreshold { get; set; }

        // Platform-specific optionals
        /// <summary>
        /// Track non-fatal ANRs. Android only. Optional.
        /// </summary>
        public bool? TrackNonFatalAnrs { get; set; }

        /// <summary>
        /// Track watchdog terminations. iOS only. Optional.
        /// </summary>
        public bool? TrackWatchdogTerminations { get; set; }

        // Enums
        /// <summary>
        /// Frequency at which mobile vitals are updated. Default: Average.
        /// </summary>
        public VitalsUpdateFrequency VitalsUpdateFrequency { get; set; } = VitalsUpdateFrequency.Average;

        // Endpoints & networking
        /// <summary>
        /// Custom server endpoint where RUM events are sent. Optional.
        /// </summary>
        public string? CustomEndpoint { get; set; }

        /// <summary>
        /// List of first-party hosts for distributed tracing. Optional.
        /// </summary>
        public List<FirstPartyHost>? FirstPartyHosts { get; set; }

        // Event mappers
        /// <summary>
        /// Mapper for RUM error events. Return the modified event to send it, or null to drop it.
        /// Applies to C# errors reported via DdRum.AddError and automatic error tracking.
        /// </summary>
        public Func<DdRumErrorEvent, DdRumErrorEvent?>? ErrorEventMapper { get; set; }

        /// <summary>
        /// Mapper for RUM resource events. Return the modified event to send it, or null to drop it.
        /// Applies to auto-tracked resources and can be used to filter or modify network requests.
        /// </summary>
        public Func<DdRumResourceEvent, DdRumResourceEvent?>? ResourceEventMapper { get; set; }

        /// <summary>
        /// Mapper for RUM action events. Return the modified event to send it, or null to drop it.
        /// Applies to auto-tracked actions and manual AddAction calls.
        /// </summary>
        public Func<DdRumActionEvent, DdRumActionEvent?>? ActionEventMapper { get; set; }

        // Auto-tracking

        /// <summary>
        /// Enable automatic MAUI page navigation tracking as RUM views. Default: true.
        /// Tracks Shell navigation, Page.Appearing events, and modal push/pop.
        /// </summary>
        public bool AutomaticViewTracking { get; set; } = true;

        /// <summary>
        /// Enable automatic MAUI user interaction tracking as RUM actions. Default: true.
        /// Tracks Button.Clicked, Switch.Toggled, CheckBox.CheckedChanged,
        /// TapGestureRecognizer.Tapped, SwipeGestureRecognizer.Swiped, etc.
        /// </summary>
        public bool AutomaticActionTracking { get; set; } = true;

        /// <summary>
        /// Enable automatic HTTP request tracking as RUM resources. Default: true.
        /// Intercepts all HttpClient requests via DiagnosticListener.
        /// </summary>
        public bool AutomaticResourceTracking { get; set; } = true;

        /// <summary>
        /// Custom predicate to determine the view name for a page.
        /// Return a string to override the default name, or null to use the default.
        /// </summary>
        public Func<Page, string?>? ViewNamePredicate { get; set; }

        /// <summary>
        /// Custom predicate to determine whether a page should be tracked as a view.
        /// Return true to track, false to skip. Default: null (track all pages).
        /// </summary>
        public Func<Page, bool>? ViewTrackingPredicate { get; set; }

        /// <summary>
        /// Converts this configuration to a flat dictionary for passing to the native bridge.
        /// </summary>
        internal Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>
            {
                ["applicationId"] = ApplicationId,
                ["sessionSampleRate"] = SessionSampleRate,
                ["telemetrySampleRate"] = TelemetrySampleRate,
                ["configurationTelemetrySampleRate"] = ConfigurationTelemetrySampleRate,
                ["resourceTraceSampleRate"] = ResourceTraceSampleRate,
                ["trackFrustrations"] = TrackFrustrations,
                ["trackBackgroundEvents"] = TrackBackgroundEvents,
                ["nativeViewTracking"] = NativeViewTracking,
                ["nativeInteractionTracking"] = NativeInteractionTracking,
                ["automaticResourceTracking"] = AutomaticResourceTracking,
                ["trackMemoryWarnings"] = TrackMemoryWarnings,
                ["nativeLongTaskThresholdMs"] = NativeLongTaskThresholdMs,
                ["vitalsUpdateFrequency"] = ConvertVitalsUpdateFrequency(VitalsUpdateFrequency),
            };

            if (InitialResourceThreshold.HasValue)
                dict["initialResourceThreshold"] = InitialResourceThreshold.Value;

            if (AppHangThreshold.HasValue)
                dict["appHangThreshold"] = AppHangThreshold.Value;

            if (TrackNonFatalAnrs.HasValue)
                dict["trackNonFatalAnrs"] = TrackNonFatalAnrs.Value;

            if (TrackWatchdogTerminations.HasValue)
                dict["trackWatchdogTerminations"] = TrackWatchdogTerminations.Value;

            if (CustomEndpoint != null)
                dict["customEndpoint"] = CustomEndpoint;

            return dict;
        }

        internal static string ConvertVitalsUpdateFrequency(VitalsUpdateFrequency frequency) => frequency switch
        {
            VitalsUpdateFrequency.Never => "never",
            VitalsUpdateFrequency.Rare => "rare",
            VitalsUpdateFrequency.Average => "average",
            VitalsUpdateFrequency.Frequent => "frequent",
            _ => "average"
        };

        internal static string ConvertTracingHeaderType(TracingHeaderType headerType) => headerType switch
        {
            TracingHeaderType.Datadog => "datadog",
            TracingHeaderType.B3 => "b3",
            TracingHeaderType.B3Multi => "b3multi",
            TracingHeaderType.TraceContext => "tracecontext",
            _ => "datadog"
        };

        [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
        internal static string SerializeFirstPartyHosts(List<FirstPartyHost> hosts)
        {
            var serializable = hosts.Select(h => new
            {
                match = h.Match,
                headerTypes = h.HeaderTypes.Select(ConvertTracingHeaderType).ToList()
            }).ToList();

            return JsonSerializer.Serialize(serializable);
        }
    }
}
