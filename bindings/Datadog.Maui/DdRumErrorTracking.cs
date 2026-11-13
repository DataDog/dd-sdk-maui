/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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

        // The crash already reported by the Java uncaught exception handler, so
        // AppDomain.UnhandledException doesn't report it a second time when it arrives there.
        private static Exception? _reportedCrash;

#if ANDROID
        private static JavaUncaughtExceptionHandler? _javaHandler;
#endif

        /// <summary>
        /// Start tracking C# errors.
        /// Hooks into AppDomain.CurrentDomain.UnhandledException and
        /// TaskScheduler.UnobservedTaskException on both platforms, plus Java's default
        /// uncaught exception handler on Android.
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
#if ANDROID
            // Tracking starts after Datadog.initialize, so this lands in front of the native
            // SDK's JVM crash handler in Java's handler chain.
            _javaHandler = new JavaUncaughtExceptionHandler(Java.Lang.Thread.DefaultUncaughtExceptionHandler);
            Java.Lang.Thread.DefaultUncaughtExceptionHandler = _javaHandler;
#endif

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
#if ANDROID
            // Only unlink ourselves if nothing has been installed in front of us since.
            if (_javaHandler != null && ReferenceEquals(Java.Lang.Thread.DefaultUncaughtExceptionHandler, _javaHandler))
            {
                Java.Lang.Thread.DefaultUncaughtExceptionHandler = _javaHandler.Next;
            }

            _javaHandler = null;
#endif

            _reportedCrash = null;
            _isTracking = false;
            InternalLog.Log("DdRumErrorTracking: Stopped tracking C# errors.", SdkVerbosity.INFO);
        }

        internal static bool IsTracking => _isTracking;

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception != null && ReferenceEquals(UnwrapJavaException(exception), _reportedCrash))
            {
                InternalLog.Log("DdRumErrorTracking: Crash already reported by the Java uncaught exception handler.", SdkVerbosity.DEBUG);
                return;
            }

            var nativeCrashReportEnabled = DdSdk.Configuration?.NativeCrashReportEnabled == true;
            if (IsSkippedPlatformCrash(exception, IsPlatformThrowable(exception), nativeCrashReportEnabled))
            {
                InternalLog.Log("DdRumErrorTracking: Skipping Java crash because NativeCrashReportEnabled is false.", SdkVerbosity.DEBUG);
                return;
            }

            HandleException(exception, "Unhandled exception", e.IsTerminating, "AppDomain.UnhandledException");
        }

        /// <summary>
        /// A crash that never went through managed code is a native crash, reported only when
        /// NativeCrashReportEnabled is set. See IsPureJavaCrash.
        /// </summary>
        internal static bool IsSkippedPlatformCrash(Exception? exception, bool isPlatformThrowable, bool nativeCrashReportEnabled)
        {
            return !nativeCrashReportEnabled && IsPureJavaCrash(exception, isPlatformThrowable);
        }

        /// <summary>
        /// True for a crash that never went through managed code: a platform throwable with no
        /// managed exception inside it (including a JavaProxyThrowable's) and no managed stack
        /// frames. A Java exception that escaped a C# call was thrown through managed frames, so
        /// it counts as the app's own crash.
        /// </summary>
        private static bool IsPureJavaCrash(Exception? exception, bool isPlatformThrowable)
        {
            return isPlatformThrowable
                && exception != null
                && exception.InnerException == null
                && ReferenceEquals(UnwrapJavaException(exception), exception)
                && new StackTrace(exception).FrameCount == 0;
        }

        private static bool IsPlatformThrowable(Exception? exception)
        {
#if ANDROID
            return exception is Java.Lang.Throwable;
#else
            return false;
#endif
        }

        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            HandleException(e.Exception.GetBaseException(), "Unobserved task exception", false, "TaskScheduler.UnobservedTaskException");
        }

#if ANDROID
        // Java's default uncaught exception handler, so it only sees throwables that nothing
        // caught. Sits in front of the native SDK's JVM crash handler, which runs next.
        private sealed class JavaUncaughtExceptionHandler : Java.Lang.Object, Java.Lang.Thread.IUncaughtExceptionHandler
        {
            internal JavaUncaughtExceptionHandler(Java.Lang.Thread.IUncaughtExceptionHandler? next)
            {
                Next = next;
            }

            internal Java.Lang.Thread.IUncaughtExceptionHandler? Next { get; }

            public void UncaughtException(Java.Lang.Thread t, Java.Lang.Throwable e)
            {
                OnJavaUncaughtException(e, isPlatformThrowable: true, () => Next?.UncaughtException(t, e));
            }
        }
#endif

        /// <summary>
        /// Body of the Android Java uncaught exception handler: reports the crash, then always hands
        /// it on to the next handler (the native SDK's JVM handler, then .NET's), even if reporting
        /// failed, so they still see the crash and the process still terminates.
        /// </summary>
        internal static void OnJavaUncaughtException(Exception exception, bool isPlatformThrowable, Action next)
        {
            try
            {
                ReportUncaughtCrash(exception, isPlatformThrowable);
            }
            catch (Exception ex)
            {
                InternalLog.Log($"DdRumErrorTracking: Failed to report uncaught exception: {ex.Message}", SdkVerbosity.ERROR);
            }
            finally
            {
                next();
            }
        }

        /// <summary>
        /// Reports a throwable that reached Java's uncaught exception handler as a crash, ahead of
        /// the native SDK's JVM crash handler. RUM keeps the first fatal error of a view, so the
        /// native copy is then dropped as a duplicate. Crashes that never went through managed code
        /// are left to the JVM handler, which records them when NativeCrashReportEnabled is set.
        /// </summary>
        internal static bool ReportUncaughtCrash(Exception exception, bool isPlatformThrowable)
        {
            if (IsPureJavaCrash(exception, isPlatformThrowable))
            {
                return false;
            }

            // Report the unwrapped C# exception, as AppDomain.UnhandledException would, rather than
            // the JavaProxyThrowable shell around it.
            var unwrapped = UnwrapJavaException(exception);
            HandleException(unwrapped, "Unhandled exception", true, "Java.Lang.Thread.UncaughtExceptionHandler");

            // Marked only once the report went through, so if it failed, AppDomain.UnhandledException
            // still reports the crash instead of skipping it as a duplicate.
            _reportedCrash = unwrapped;
            return true;
        }

        /// <summary>
        /// Shared by both handlers so UnwrapJavaException is structurally guaranteed to apply
        /// identically to both — see its own doc comment for why that matters.
        /// </summary>
        private static void HandleException(Exception? rawException, string defaultMessage, bool isCrash, string handler)
        {
            var exception = UnwrapJavaException(rawException);
            var message = exception?.Message ?? defaultMessage;
            var renderedStackTrace = BuildStackTrace(exception, BuildJavaStackTrace(rawException));

            ReportError(message, renderedStackTrace, isCrash, handler);
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
            if (exception is Java.Lang.Throwable javaThrowable)
            {
                if (ReadProxiedException(javaThrowable) is Exception proxied)
                {
                    return proxied;
                }

                if (javaThrowable.InnerException != null)
                {
                    return javaThrowable.InnerException;
                }
            }
#endif
            return exception;
        }

        /// <summary>
        /// The C# exception carried by Android.Runtime.JavaProxyThrowable (internal to Mono.Android),
        /// which keeps it in its own public InnerException field and leaves Exception.InnerException
        /// null. Null for any other exception.
        /// </summary>
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Mono.Android reads JavaProxyThrowable.InnerException itself, so trimming keeps the field.")]
        internal static Exception? ReadProxiedException(Exception exception)
        {
            return exception.GetType()
                .GetField("InnerException", BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(exception) as Exception;
        }

        /// <summary>
        /// Renders the managed stack trace and builds sdk_frames in one pass. line_index is
        /// zero-based and addresses the final newline-delimited stack string. Because the
        /// text line and metadata entry are emitted together, a frame whose metadata can't
        /// be read cannot shift the mapping for any later frame.
        /// When <paramref name="stackTrace"/> is set, it replaces the outer exception's own
        /// frames (used for exceptions reported without being thrown); inner exceptions
        /// always render their own.
        /// </summary>
        internal static RenderedStackTrace BuildStackTrace(Exception? exception, IReadOnlyList<string>? javaStackTrace = null, StackTrace? stackTrace = null)
        {
            return BuildStackTrace(exception, javaStackTrace, BuildSdkFrameData, stackTrace);
        }

        internal static RenderedStackTrace BuildStackTrace(
            Exception? exception,
            IReadOnlyList<string>? javaStackTrace,
            Func<StackFrame, MethodBase, int, Dictionary<string, object>> sdkFrameFactory,
            StackTrace? stackTrace = null)
        {
            var lines = new List<string>();
            var frames = new List<Dictionary<string, object>>();

            if (exception != null)
            {
                RenderException(exception, lines, frames, sdkFrameFactory, isInnerException: false, stackTrace);
            }
            else
            {
                lines.Add("No stacktrace available");
            }

            if (javaStackTrace is { Count: > 0 })
            {
                foreach (var javaLine in javaStackTrace)
                {
                    AppendLines(lines, javaLine);
                }
            }

            return new RenderedStackTrace(string.Join('\n', lines), frames);
        }

        private static void RenderException(
            Exception exception,
            List<string> lines,
            List<Dictionary<string, object>> sdkFrames,
            Func<StackFrame, MethodBase, int, Dictionary<string, object>> sdkFrameFactory,
            bool isInnerException,
            StackTrace? stackTrace = null)
        {
            AppendLines(lines, $"{(isInnerException ? " ---> " : string.Empty)}{exception.GetType().FullName}: {exception.Message}");

            IEnumerable<Exception> innerExceptions = exception is AggregateException aggregate
                ? aggregate.InnerExceptions
                : exception.InnerException is { } inner
                    ? new[] { inner }
                    : Array.Empty<Exception>();

            foreach (var innerException in innerExceptions)
            {
                RenderException(innerException, lines, sdkFrames, sdkFrameFactory, isInnerException: true);
            }

            try
            {
                stackTrace ??= new StackTrace(exception, true);
                foreach (var frame in stackTrace.GetFrames() ?? Array.Empty<StackFrame>())
                {
                    RenderFrame(frame, lines, sdkFrames, sdkFrameFactory);
                }
            }
            catch (Exception ex)
            {
                InternalTelemetry.Error("DdRumErrorTracking: Failed to render managed stack frames", ex);
            }

            if (isInnerException)
            {
                lines.Add("   --- End of inner exception stack trace ---");
            }
        }

        private static void RenderFrame(
            StackFrame frame,
            List<string> lines,
            List<Dictionary<string, object>> sdkFrames,
            Func<StackFrame, MethodBase, int, Dictionary<string, object>> sdkFrameFactory)
        {
            MethodBase? method = null;
            try
            {
                method = frame.GetMethod();
                lines.Add(method == null ? "   at <unknown>" : DdRumErrorTrackingFormatHelpers.FormatManagedFrame(frame, method));
            }
            catch (Exception frameEx)
            {
                lines.Add("   at <unknown>");
                InternalLog.Log($"DdRumErrorTracking: Failed to render a managed frame: {frameEx.Message}", SdkVerbosity.DEBUG);
            }

            if (method == null)
            {
                return;
            }

            try
            {
                sdkFrames.Add(sdkFrameFactory(frame, method, lines.Count - 1));
            }
            catch (Exception frameEx)
            {
                InternalLog.Log($"DdRumErrorTracking: Failed to build sdk_frames entry for a frame: {frameEx.Message}", SdkVerbosity.DEBUG);
            }

            AppendRuntimeFrameMarkers(frame, lines);
        }

        private static Dictionary<string, object> BuildSdkFrameData(StackFrame frame, MethodBase method, int lineIndex)
        {
            var frameData = new Dictionary<string, object>
            {
                { "line_index", lineIndex },
                { "method_token", method.MetadataToken },
                { "il_offset", frame.GetILOffset() }
            };

            var assemblyId = AssemblyDebugId.TryGetDebugId(method.Module.Assembly);
            if (assemblyId != null)
            {
                frameData["assembly_id"] = assemblyId;
            }

            return frameData;
        }

        private static void AppendRuntimeFrameMarkers(StackFrame frame, List<string> lines)
        {
            try
            {
                // StackFrame does not publicly expose the flag set by
                // ExceptionDispatchInfo.Throw. Formatting that individual frame lets the
                // runtime append any associated boundary marker without making the raw
                // runtime-rendered method line authoritative for sdk_frames correlation.
                var runtimeStackTrace = new StackTrace(frame).ToString();
                var runtimeLines = runtimeStackTrace.Replace("\r\n", "\n")
                                                    .Replace('\r', '\n')
                                                    .Split('\n');
                var skippedFrameLine = false;
                foreach (var runtimeLine in runtimeLines)
                {
                    if (string.IsNullOrWhiteSpace(runtimeLine))
                    {
                        continue;
                    }

                    if (!skippedFrameLine)
                    {
                        skippedFrameLine = true;
                        continue;
                    }

                    lines.Add(runtimeLine);
                }
            }
            catch (Exception frameEx)
            {
                InternalLog.Log($"DdRumErrorTracking: Failed to render stack frame markers: {frameEx.Message}", SdkVerbosity.DEBUG);
            }
        }

        private static void AppendLines(List<string> lines, string text)
        {
            lines.AddRange(text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'));
        }

        private static IReadOnlyList<string>? BuildJavaStackTrace(Exception? rawException)
        {
#if ANDROID
            if (rawException is not Java.Lang.Throwable javaThrowable)
            {
                return null;
            }

            var lines = new List<string> { javaThrowable.ToString() };
            foreach (var frame in javaThrowable.GetStackTrace())
            {
                lines.Add($"   at {frame}");
            }

            return lines;
#else
            return null;
#endif
        }

        /// <summary>
        /// Reports an exception the app caught itself (DdRum.AddError's Exception overload) the
        /// way uncaught ones are reported: the same unwrapping, stack text and sdk_frames (and,
        /// on Android, the Java frames of a Java exception), but as a non-crash, with the
        /// caller's message, source, context, timestamp and fingerprint.
        /// <paramref name="stackTrace"/>, when set, replaces the exception's own frames.
        /// </summary>
        internal static void ReportHandledException(
            Exception rawException,
            string? message,
            RumErrorSource source,
            Dictionary<string, object>? context,
            long timestampMs,
            string? fingerprint,
            StackTrace? stackTrace)
        {
            var exception = UnwrapJavaException(rawException) ?? rawException;
            var renderedStackTrace = BuildStackTrace(exception, BuildJavaStackTrace(rawException), stackTrace);
            ReportIlOffsetZeroAsUnknown(renderedStackTrace.SdkFrames);

            // The SDK's keys win over the caller's: sdk_frames must describe the stack text
            // built here, so a caller-supplied value is dropped even when no frame was built.
            var errorContext = context != null ? new Dictionary<string, object>(context) : new Dictionary<string, object>();
            errorContext["_dd.error.is_crash"] = false;
            errorContext["_dd.error.handler"] = HandledExceptionHandler;
            errorContext.Remove("_dd.error.sdk_frames");
            if (renderedStackTrace.SdkFrames.Count > 0)
            {
                errorContext["_dd.error.sdk_frames"] = renderedStackTrace.SdkFrames;
            }

            // A custom exception's Message can return null, which the native SDKs don't accept.
            var errorMessage = message ?? exception.Message ?? exception.GetType().FullName ?? "Exception";
            DdRum.AddError(errorMessage, source, renderedStackTrace.Text, errorContext, timestampMs, fingerprint);
        }

        internal const string HandledExceptionHandler = "DdRum.AddError";

        /// <summary>
        /// Sends IL offset 0 as -1 ("unknown") in the handled-exception path's sdk_frames.
        /// </summary>
        /// <remarks>
        /// With an IL offset, the backend looks up the line at that exact offset; with -1, it
        /// uses the method's first line. For offset 0 the two only differ when the method starts
        /// with a hidden sequence point (async state machines' MoveNext, many lambdas): the
        /// backend's line table drops hidden points, so the exact lookup finds nothing and the
        /// frame is left without file and line, while the first-line lookup still finds one.
        /// When the method has a real line at offset 0, both give that line. So -1 is never
        /// worse than 0. This matters for stacks captured on the current thread (a never-thrown
        /// exception, or a stack the caller passed): under LLVM AOT (iOS device builds) Mono
        /// reports offset 0 for them, where an exception's own frames report -1.
        /// Applied on both platforms, and only here: crashes keep the runtime's offsets.
        /// </remarks>
        internal static void ReportIlOffsetZeroAsUnknown(List<Dictionary<string, object>> sdkFrames)
        {
            foreach (var frame in sdkFrames)
            {
                if (frame.TryGetValue("il_offset", out var ilOffset) && ilOffset is 0)
                {
                    frame["il_offset"] = -1;
                }
            }
        }

        private static void ReportError(string message, RenderedStackTrace renderedStackTrace, bool isCrash, string handler)
        {
            InternalLog.Log($"DdRumErrorTracking: Caught error via {handler}: {message}", SdkVerbosity.DEBUG);

            var context = new Dictionary<string, object>
            {
                { "_dd.error.is_crash", isCrash },
                { "_dd.error.handler", handler }
            };

            if (renderedStackTrace.SdkFrames.Count > 0)
            {
                context["_dd.error.sdk_frames"] = renderedStackTrace.SdkFrames;
            }

            var timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            DdRum.AddError(message, RumErrorSource.Source, renderedStackTrace.Text, context, timestampMs);

            if (isCrash)
            {
                // Give the native SDK time to persist the error event to disk
                // before the process terminates.
                Thread.Sleep(100);
            }
        }

        internal sealed record RenderedStackTrace(string Text, List<Dictionary<string, object>> SdkFrames);
    }
}
