/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System;
using System.Diagnostics;
using Datadog.Maui.Configuration;

namespace Datadog.Maui
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
            var exception = UnwrapJavaException(e.ExceptionObject as Exception);
            var message = exception?.Message ?? "Unhandled exception";
            var stacktrace = exception?.ToString() ?? "No stacktrace available";
            var isCrash = e.IsTerminating;

            ReportError(message, stacktrace, isCrash, "AppDomain.UnhandledException", exception);
        }

        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            var exception = UnwrapJavaException(e.Exception.GetBaseException());
            var message = exception?.Message ?? "Unobserved task exception";
            var stacktrace = exception?.ToString() ?? "No stacktrace available";

            ReportError(message, stacktrace, false, "TaskScheduler.UnobservedTaskException", exception);
        }

        /// <summary>
        /// On Android, an exception that crosses the JNI boundary can arrive wrapped in a
        /// Java.Lang.Throwable (JavaProxyThrowable) shell. The real C# exception — and the
        /// stack trace we actually want to report — is one level down, at InnerException.
        /// Both handlers must apply this identically so the raw-text stacktrace and the
        /// sdk_frames built in ReportError always describe the same exception instance;
        /// unwrapping only one of the two would silently desync them.
        /// </summary>
        internal static Exception? UnwrapJavaException(Exception? exception)
        {
#if ANDROID
            if (exception is Java.Lang.Throwable javaThrowable && javaThrowable.InnerException != null)
            {
                return javaThrowable.InnerException;
            }
#endif
            return exception;
        }

        /// <summary>
        /// Builds the structured, per-frame counterpart to the raw-text stacktrace.
        /// Each frame carries the assembly's native PE debug-directory id (GUID+Stamp —
        /// the same id build-time tooling reads off the compiled DLL/PDB, not
        /// Module.ModuleVersionId/MVID), the method's metadata token, and its IL offset.
        /// Frames whose assembly id can't be resolved (e.g. AOT/single-file bundling,
        /// where Assembly.Location is empty) are still included, just without that field.
        /// </summary>
        internal static List<Dictionary<string, object>> BuildSdkFrames(Exception? exception)
        {
            var frames = new List<Dictionary<string, object>>();
            if (exception == null)
            {
                return frames;
            }

            try
            {
                var stackTrace = new StackTrace(exception, false);
                foreach (var frame in stackTrace.GetFrames() ?? Array.Empty<StackFrame>())
                {
                    var method = frame.GetMethod();
                    if (method == null)
                    {
                        continue;
                    }

                    var frameData = new Dictionary<string, object>
                    {
                        { "method_token", method.MetadataToken },
                        { "il_offset", frame.GetILOffset() }
                    };

                    var assemblyId = AssemblyDebugId.TryGetDebugId(method.Module.Assembly);
                    if (assemblyId != null)
                    {
                        frameData["assembly_id"] = assemblyId;
                    }

                    frames.Add(frameData);
                }
            }
            catch (Exception ex)
            {
                InternalLog.Log($"DdRumErrorTracking: Failed to build sdk_frames: {ex.Message}", SdkVerbosity.DEBUG);
            }

            return frames;
        }

        private static void ReportError(string message, string stacktrace, bool isCrash, string handler, Exception? exception)
        {
            InternalLog.Log($"DdRumErrorTracking: Caught error via {handler}: {message}", SdkVerbosity.DEBUG);

            var context = new Dictionary<string, object>
            {
                { "_dd.error.is_crash", isCrash },
                { "_dd.error.handler", handler }
            };

            var sdkFrames = BuildSdkFrames(exception);
            if (sdkFrames.Count > 0)
            {
                context["_dd.error.sdk_frames"] = sdkFrames;
            }

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
