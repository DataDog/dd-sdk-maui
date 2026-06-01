/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

#if ANDROID
using NativeDdLogs = Datadog.Android.Binding.DdLogs;
#elif IOS
using Foundation;
using NativeDdLogs = Datadog.iOS.Binding.DdLogs;
#endif

using Datadog.Maui.Configuration;

namespace Datadog.Maui
{
    public static class DdLogs
    {
        /// <summary>
        /// Enable the Logs module with optional configuration.
        /// Must be called after DdSdk.Initialize().
        /// </summary>
        /// <param name="configuration">Optional configuration for the Logs module. If null, default configuration is used.</param>
        public static void Enable(DdLogsConfiguration? configuration = null)
        {
            InternalLog.Log($"DdLogs.Enable called with config: {(configuration != null ? "provided" : "null")}", SdkVerbosity.DEBUG);

            var customEndpoint = configuration?.CustomEndpoint;

            NativeDdLogs.EnableLogs(customEndpoint);
            InternalLog.Log("DdLogs.Enable completed", SdkVerbosity.DEBUG);
        }

        public static void Debug(string message)
        {
            InternalLog.Log($"DdLogs.Debug called: {message}", SdkVerbosity.DEBUG);
            LogWithAttributes("debug", message, new Dictionary<string, object>());
        }

        public static void Info(string message)
        {
            InternalLog.Log($"DdLogs.Info called: {message}", SdkVerbosity.DEBUG);
            LogWithAttributes("info", message, new Dictionary<string, object>());
        }

        public static void Warn(string message)
        {
            InternalLog.Log($"DdLogs.Warn called: {message}", SdkVerbosity.DEBUG);
            LogWithAttributes("warn", message, new Dictionary<string, object>());
        }

        public static void Error(string message)
        {
            InternalLog.Log($"DdLogs.Error called: {message}", SdkVerbosity.DEBUG);
            LogWithAttributes("error", message, new Dictionary<string, object>());
        }

        public static void LogWithAttributes(string level, string message, Dictionary<string, object> attributes)
        {
            InternalLog.Log($"DdLogs.LogWithAttributes called: level={level}, message={message}, attributes count={attributes.Count}", SdkVerbosity.DEBUG);

            var merged = MergeWithGlobalAttributes(attributes);

#if ANDROID
            NativeDdLogs.LogWithAttributes(level, message, DdSdk.ToJavaDictionary(merged));
#elif IOS
            NativeDdLogs.LogWithAttributes(level, message, DdSdk.ToNSDictionary(merged));
#endif
        }

        private static Dictionary<string, object> MergeWithGlobalAttributes(Dictionary<string, object>? perCallAttributes)
        {
            var globalAttrs = DdSdk.GetAttributes();
            if (perCallAttributes != null)
            {
                // Per-call attributes take precedence
                foreach (var kvp in perCallAttributes)
                {
                    globalAttrs[kvp.Key] = kvp.Value;
                }
            }
            return globalAttrs;
        }
    }
}
