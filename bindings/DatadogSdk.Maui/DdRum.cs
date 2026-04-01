#if ANDROID
using NativeDdRum = DatadogSdk.Android.Binding.DdRum;
#elif IOS
using Foundation;
using NativeDdRum = DatadogSdk.iOS.Binding.DdRum;
#endif

using DatadogSdk.Maui.Configuration;

namespace DatadogSdk.Maui
{
    public static class DdRum
    {
        internal interface IRumBridge
        {
            void AddError(string message, string source, string stacktrace,
                          Dictionary<string, object> context, long timestampMs);
        }

        internal static IRumBridge? testBridge;

        // Stored from Enable() for use in AddError()
        internal static Func<DdRumErrorEvent, DdRumErrorEvent?>? errorEventMapper;

        /// <summary>
        /// Enable the RUM module with the provided configuration.
        /// Must be called after DdSdk.Initialize().
        /// </summary>
        /// <param name="configuration">Configuration for the RUM module.</param>
        public static void Enable(DdRumConfiguration configuration)
        {
            InternalLog.Log($"DdRum.Enable called with applicationId: {configuration.ApplicationId}", SdkVerbosity.DEBUG);

            // Store the error event mapper for use in AddError
            errorEventMapper = configuration.ErrorEventMapper;

            // Warn about stub event mappers
            if (configuration.ResourceEventMapper != null)
                InternalLog.Log("DdRum: ResourceEventMapper is not yet supported and will be ignored.", SdkVerbosity.WARN);
            if (configuration.ActionEventMapper != null)
                InternalLog.Log("DdRum: ActionEventMapper is not yet supported and will be ignored.", SdkVerbosity.WARN);

            var dict = configuration.ToDictionary();

#if ANDROID
            NativeDdRum.EnableRum(DdSdk.ToJavaDictionary(dict));
#elif IOS
            NativeDdRum.EnableRum(DdSdk.ToNSDictionary(dict));
#endif

            // Start C# error tracking
            DdRumErrorTracking.StartTracking();

            InternalLog.Log("DdRum.Enable completed", SdkVerbosity.DEBUG);
        }

        /// <summary>
        /// Report an error to the RUM monitor.
        /// If an ErrorEventMapper was set in DdRumConfiguration, it will be applied
        /// before sending. Return null from the mapper to drop the error.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="source">Error source (e.g., "source", "network", "console", "webview", "custom").</param>
        /// <param name="stacktrace">Error stacktrace string.</param>
        /// <param name="context">Additional context attributes. Optional.</param>
        /// <param name="timestampMs">Timestamp in milliseconds since epoch. Optional (0 = use current time).</param>
        /// <param name="fingerprint">Custom fingerprint for error grouping. Optional. When set, overrides the default grouping.</param>
        public static void AddError(string message, string source, string stacktrace,
                                     Dictionary<string, object>? context = null, long timestampMs = 0,
                                     string? fingerprint = null)
        {
            var ctx = context ?? new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(fingerprint))
            {
                ctx["_dd.error.fingerprint"] = fingerprint!;
            }

            // Apply error event mapper if configured
            if (errorEventMapper != null)
            {
                try
                {
                    var errorEvent = new DdRumErrorEvent(message, source, stacktrace, ctx, timestampMs);
                    var mappedEvent = errorEventMapper(errorEvent);
                    if (mappedEvent == null)
                    {
                        InternalLog.Log($"DdRum.AddError: Error dropped by ErrorEventMapper", SdkVerbosity.DEBUG);
                        return;
                    }
                    message = mappedEvent.Message;
                    source = mappedEvent.Source;
                    stacktrace = mappedEvent.Stacktrace;
                    ctx = mappedEvent.Context;
                    timestampMs = mappedEvent.TimestampMs;
                }
                catch (Exception ex)
                {
                    InternalLog.Log($"DdRum.AddError: ErrorEventMapper threw an exception, sending original error. {ex.Message}", SdkVerbosity.ERROR);
                }
            }

            InternalLog.Log($"DdRum.AddError: {message}", SdkVerbosity.DEBUG);

            if (testBridge is not null)
            {
                testBridge.AddError(message, source, stacktrace, ctx, timestampMs);
                return;
            }

#if ANDROID
            NativeDdRum.AddError(message, source, stacktrace, DdSdk.ToJavaDictionary(ctx), timestampMs);
#elif IOS
            NativeDdRum.AddError(message, source, stacktrace, DdSdk.ToNSDictionary(ctx), timestampMs);
#endif
        }
    }
}
