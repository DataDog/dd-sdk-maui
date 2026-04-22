/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

using System;
using DatadogSdk.Maui.Configuration;

namespace DatadogSdk.Maui
{
    /// <summary>
    /// Provides automatic C#-level error tracking for MAUI applications.
    /// Hooks into unhandled exception handlers and reports errors to Datadog RUM.
    /// </summary>
    internal static class DdRumErrorTracking
    {
        private static bool _isTracking;

        /// <summary>
        /// Start tracking C# errors.
        /// Hooks into AppDomain.CurrentDomain.UnhandledException and
        /// TaskScheduler.UnobservedTaskException on both platforms.
        /// Safe to call multiple times — subsequent calls are no-ops.
        /// </summary>
        internal static void StartTracking()
        {
            if (_isTracking)
            {
                InternalLog.Log("DdRumErrorTracking: Already tracking errors.", SdkVerbosity.WARN);
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            _isTracking = true;
            InternalLog.Log("DdRumErrorTracking: Started tracking C# errors.", SdkVerbosity.INFO);
        }

        /// <summary>
        /// Stop tracking C# errors. Unhooks all exception handlers.
        /// </summary>
        internal static void StopTracking()
        {
            if (!_isTracking)
            {
                return;
            }

            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

            _isTracking = false;
            InternalLog.Log("DdRumErrorTracking: Stopped tracking C# errors.", SdkVerbosity.INFO);
        }

        internal static bool IsTracking => _isTracking;

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
#if ANDROID
            // On Android, this receives a JavaProxyThrowable wrapper.
            // Extract the original C# exception from the inner exception.
            if (exception is Java.Lang.Throwable javaThrowable && javaThrowable.InnerException != null)
            {
                exception = javaThrowable.InnerException;
            }
#endif
            var message = exception?.Message ?? "Unhandled exception";
            var stacktrace = exception?.ToString() ?? "No stacktrace available";
            var isCrash = e.IsTerminating;

            ReportError(message, stacktrace, isCrash, "AppDomain.UnhandledException");
        }

        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            var exception = e.Exception.GetBaseException();
            var message = exception.Message;
            var stacktrace = exception.ToString();

            ReportError(message, stacktrace, false, "TaskScheduler.UnobservedTaskException");
        }

        private static void ReportError(string message, string stacktrace, bool isCrash, string handler)
        {
            InternalLog.Log($"DdRumErrorTracking: Caught error via {handler}: {message}", SdkVerbosity.DEBUG);

            var context = new Dictionary<string, object>
            {
                { "_dd.error.is_crash", isCrash },
                { "_dd.error.handler", handler }
            };

            var timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            DdRum.AddError(message, RumErrorSource.Source, stacktrace, context, timestampMs);

            if (isCrash)
            {
                // Give the native SDK time to persist the error event to disk
                // before the process terminates.
                Thread.Sleep(100);
            }
        }
    }
}
