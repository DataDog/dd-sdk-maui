/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

#if ANDROID
using NativeDdTrace = Datadog.Android.Binding.DdTrace;
#elif IOS
using Foundation;
using NativeDdTrace = Datadog.iOS.Binding.DdTrace;
#endif

using Datadog.Maui.Configuration;

namespace Datadog.Maui
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

            ReportEnableTelemetry();

            InternalLog.Log("DdTrace.Enable completed", SdkVerbosity.DEBUG);
        }

        private static void ReportEnableTelemetry()
        {
            InternalTelemetry.ReportConfiguration(new Dictionary<string, object>
            {
                ["useTracing"] = true,
                ["tracerAPI"] = "DatadogTrace",
            });
        }

        /// <summary>
        /// Start a new span with the given operation name.
        /// </summary>
        /// <param name="operation">The name of the operation being traced.</param>
        /// <param name="context">Additional context attributes for the span.</param>
        /// <param name="timestampMs">The start timestamp in milliseconds since epoch.</param>
        /// <returns>A span ID string that can be used to finish the span later.</returns>
        public static string StartSpan(string operation, Dictionary<string, object> context, long timestampMs)
        {
            InternalLog.Log($"DdTrace.StartSpan called: operation={operation}, timestampMs={timestampMs}", SdkVerbosity.DEBUG);

            var merged = MergeWithGlobalAttributes(context);

#if ANDROID
            return NativeDdTrace.StartSpan(operation, DdSdk.ToJavaDictionary(merged), timestampMs);
#elif IOS
            return NativeDdTrace.StartSpan(operation, DdSdk.ToNSDictionary(merged), timestampMs);
#endif
        }

        /// <summary>
        /// Finish a previously started span.
        /// </summary>
        /// <param name="spanId">The span ID returned by StartSpan.</param>
        /// <param name="context">Additional context attributes to add before finishing.</param>
        /// <param name="timestampMs">The finish timestamp in milliseconds since epoch.</param>
        public static void FinishSpan(string spanId, Dictionary<string, object> context, long timestampMs)
        {
            InternalLog.Log($"DdTrace.FinishSpan called: spanId={spanId}, timestampMs={timestampMs}", SdkVerbosity.DEBUG);

            var merged = MergeWithGlobalAttributes(context);

#if ANDROID
            NativeDdTrace.FinishSpan(spanId, DdSdk.ToJavaDictionary(merged), timestampMs);
#elif IOS
            NativeDdTrace.FinishSpan(spanId, DdSdk.ToNSDictionary(merged), timestampMs);
#endif
        }

        private static Dictionary<string, object> MergeWithGlobalAttributes(Dictionary<string, object>? perCallAttributes)
        {
            var globalAttrs = DdSdk.GetAttributes();
            if (perCallAttributes != null)
            {
                foreach (var kvp in perCallAttributes)
                {
                    globalAttrs[kvp.Key] = kvp.Value;
                }
            }
            return globalAttrs;
        }
    }
}
