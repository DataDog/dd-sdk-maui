/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

#if ANDROID
using NativeDdTelemetry = DatadogSdk.Android.Binding.DdTelemetry;
#elif IOS
using Foundation;
using NativeDdTelemetry = DatadogSdk.iOS.Binding.DdTelemetry;
#endif
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace DatadogSdk.Maui
{
    /// <summary>
    /// Forwards SDK-internal telemetry (errors, debug, configuration) from the
    /// MAUI binding layer down to the native iOS / Android SDKs, which in turn
    /// route them to Datadog's telemetry intake.
    ///
    /// Use this for *wrapper-level* failures — places where the C# layer would
    /// otherwise swallow an exception or hit an invalid-state branch silently.
    /// Application-level errors should still go through DdRum.AddError.
    /// </summary>
    internal static class InternalTelemetry
    {
        /// <summary>
        /// Forwards an exception caught in the wrapper layer.
        /// </summary>
        public static void Error(
            string message,
            System.Exception? exception = null,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
#if ANDROID
                NativeDdTelemetry.ErrorWithThrowable(message, exception != null ? Java.Lang.Throwable.FromException(exception) : null);
#elif IOS
                var id = BuildId(filePath, lineNumber);
                NativeDdTelemetry.Error(id, message, exception?.GetType().Name, exception?.StackTrace);
#endif
            }
            catch (System.Exception logFailure)
            {
                // Telemetry must never break the host app.
                InternalLog.Log($"Failed to forward telemetry error: {logFailure.Message}", SdkVerbosity.DEBUG);
            }
        }

        /// <summary>
        /// Forwards a debug message.
        /// </summary>
        public static void Debug(
            string message,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
#if ANDROID
                NativeDdTelemetry.Debug(message);
#elif IOS
                var id = BuildId(filePath, lineNumber);
                NativeDdTelemetry.Debug(id, message);
#endif
            }
            catch (System.Exception logFailure)
            {
                InternalLog.Log($"Failed to forward telemetry debug: {logFailure.Message}", SdkVerbosity.DEBUG);
            }
        }

        /// <summary>
        /// Reports SDK configuration telemetry. Called once during initialization
        /// after the native SDK is up. Fields not set in <paramref name="fields"/>
        /// are simply omitted — callers should set only what they know.
        /// </summary>
        /// <remarks>
        /// On iOS this populates the configuration telemetry event end-to-end via
        /// <c>core.telemetry.configuration(...)</c>. On Android, fields are accumulated
        /// in the wrapper and applied to emitted <c>TelemetryConfigurationEvent</c>s
        /// via a mapper installed in <c>DdRum.enableRum</c> (using the public
        /// <c>_RumInternalProxy.setTelemetryConfigurationEventMapper</c> API,
        /// the same path dd-sdk-flutter uses).
        /// </remarks>
        public static void ReportConfiguration(IDictionary<string, object> fields)
        {
            try
            {
#if ANDROID
                NativeDdTelemetry.ReportConfiguration(DdSdk.ToJavaDictionary(fields));
#elif IOS
                NativeDdTelemetry.ReportConfiguration(DdSdk.ToNSDictionary(fields));
#endif
            }
            catch (System.Exception logFailure)
            {
                InternalLog.Log($"Failed to forward telemetry configuration: {logFailure.Message}", SdkVerbosity.DEBUG);
            }
        }

#if IOS
        private static string BuildId(string filePath, int lineNumber)
        {
            var fileName = string.IsNullOrEmpty(filePath) ? "unknown" : Path.GetFileName(filePath);
            return $"{fileName}:{lineNumber}";
        }
#endif
    }
}
