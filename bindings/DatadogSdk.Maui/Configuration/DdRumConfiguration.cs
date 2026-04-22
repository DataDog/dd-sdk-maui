using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

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
        /// Mapper for RUM resource events. Not yet implemented - setting this will log a warning.
        /// </summary>
        public Func<object, object?>? ResourceEventMapper { get; set; }

        /// <summary>
        /// Mapper for RUM action events. Not yet implemented - setting this will log a warning.
        /// </summary>
        public Func<object, object?>? ActionEventMapper { get; set; }

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
                ["resourceTraceSampleRate"] = ResourceTraceSampleRate,
                ["trackFrustrations"] = TrackFrustrations,
                ["trackBackgroundEvents"] = TrackBackgroundEvents,
                ["nativeViewTracking"] = NativeViewTracking,
                ["nativeInteractionTracking"] = NativeInteractionTracking,
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
