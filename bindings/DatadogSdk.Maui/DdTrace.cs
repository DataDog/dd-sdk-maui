#if ANDROID
using NativeDdTrace = DatadogSdk.Android.Binding.DdTrace;
#elif IOS
using Foundation;
using NativeDdTrace = DatadogSdk.iOS.Binding.DdTrace;
#endif

using DatadogSdk.Maui.Configuration;

namespace DatadogSdk.Maui
{
    public static class DdTrace
    {
        /// <summary>
        /// Enable the Trace module with optional configuration.
        /// Must be called after DdSdk.Initialize().
        /// </summary>
        /// <param name="configuration">Optional configuration for the Trace module. If null, default configuration is used.</param>
        public static void Enable(DdTraceConfiguration? configuration = null)
        {
            InternalLog.Log($"DdTrace.Enable called with config: {(configuration != null ? "provided" : "null")}", SdkVerbosity.DEBUG);

            var customEndpoint = configuration?.CustomEndpoint;

            NativeDdTrace.EnableTrace(customEndpoint);
            InternalLog.Log("DdTrace.Enable completed", SdkVerbosity.DEBUG);
        }

        /// <summary>
        /// Start a new span with the given operation name.
        /// </summary>
        /// <param name="operation">The name of the operation being traced.</param>
        /// <param name="context">Additional context attributes for the span.</param>
        /// <param name="timestampMs">The start timestamp in milliseconds since epoch.</param>
        /// <returns>A span ID string that can be used to finish the span later.</returns>
        public static string StartSpan(string operation, Dictionary<string, string> context, long timestampMs)
        {
            InternalLog.Log($"DdTrace.StartSpan called: operation={operation}, timestampMs={timestampMs}", SdkVerbosity.DEBUG);

#if ANDROID
            return NativeDdTrace.StartSpan(operation, context, timestampMs);
#elif IOS
            var keys = context.Keys.Select(k => new NSString(k)).ToArray();
            var values = context.Values.Select(v => new NSString(v)).ToArray();
            var nsDict = NSDictionary<NSString, NSString>.FromObjectsAndKeys(values, keys);
            return NativeDdTrace.StartSpan(operation, nsDict, timestampMs);
#endif
        }

        /// <summary>
        /// Finish a previously started span.
        /// </summary>
        /// <param name="spanId">The span ID returned by StartSpan.</param>
        /// <param name="context">Additional context attributes to add before finishing.</param>
        /// <param name="timestampMs">The finish timestamp in milliseconds since epoch.</param>
        public static void FinishSpan(string spanId, Dictionary<string, string> context, long timestampMs)
        {
            InternalLog.Log($"DdTrace.FinishSpan called: spanId={spanId}, timestampMs={timestampMs}", SdkVerbosity.DEBUG);

#if ANDROID
            NativeDdTrace.FinishSpan(spanId, context, timestampMs);
#elif IOS
            var keys = context.Keys.Select(k => new NSString(k)).ToArray();
            var values = context.Values.Select(v => new NSString(v)).ToArray();
            var nsDict = NSDictionary<NSString, NSString>.FromObjectsAndKeys(values, keys);
            NativeDdTrace.FinishSpan(spanId, nsDict, timestampMs);
#endif
        }
    }
}
