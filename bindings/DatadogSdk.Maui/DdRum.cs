#if ANDROID
using NativeDdRum = DatadogSdk.Android.Binding.DdRum;
#elif IOS
using Foundation;
using NativeDdRum = DatadogSdk.iOS.Binding.DdRum;
#endif

using DatadogSdk.Maui.AutoTracking;
using DatadogSdk.Maui.Configuration;

namespace DatadogSdk.Maui
{
    public static class DdRum
    {
        internal interface IRumBridge
        {
            void AddError(string message, RumErrorSource source, string stacktrace,
                          Dictionary<string, object> context, long timestampMs);

            // Views
            void StartView(string key, string name, Dictionary<string, object> context, long timestampMs);
            void StopView(string key, Dictionary<string, object> context, long timestampMs);

            // Actions
            void StartAction(RumActionType type, string name, Dictionary<string, object> context, long timestampMs);
            void StopAction(RumActionType type, string name, Dictionary<string, object> context, long timestampMs);
            void AddAction(RumActionType type, string name, Dictionary<string, object> context, long timestampMs);

            // Resources
            void StartResource(string key, RumResourceMethod method, string url, Dictionary<string, object> context, long timestampMs);
            void StopResource(string key, int statusCode, RumResourceKind kind, long size, Dictionary<string, object> context, long timestampMs);

            // Timing
            void AddTiming(string name);
            void AddViewLoadingTime(bool overwrite);

            // Session
            void StopSession();

            // View Attributes
            void AddViewAttribute(string key, object value);
            void RemoveViewAttribute(string key);
            void AddViewAttributes(Dictionary<string, object> attributes);
            void RemoveViewAttributes(List<string> keys);
        }

        internal static IRumBridge? testBridge;

        // Stored from Enable() for use in AddError()
        internal static Func<DdRumErrorEvent, DdRumErrorEvent?>? errorEventMapper;
        internal static Func<DdRumActionEvent, DdRumActionEvent?>? actionEventMapper;
        private static DdAutoViewTracker? viewTracker;
        private static DdAutoActionTracker? actionTracker;

        /// <summary>
        /// Resets mapper fields for testing purposes.
        /// </summary>
        internal static void ResetForTesting()
        {
            errorEventMapper = null;
            actionEventMapper = null;
        }

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

            // Store the action event mapper for use in AddAction
            actionEventMapper = configuration.ActionEventMapper;

            // Warn about stub event mappers
            if (configuration.ResourceEventMapper != null)
                InternalLog.Log("DdRum: ResourceEventMapper is not yet supported and will be ignored.", SdkVerbosity.WARN);

            var dict = configuration.ToDictionary();

#if ANDROID
            NativeDdRum.EnableRum(DdSdk.ToJavaDictionary(dict));
#elif IOS
            NativeDdRum.EnableRum(DdSdk.ToNSDictionary(dict));
#endif

            // Start C# error tracking
            DdRumErrorTracking.StartTracking();

            // Start automatic view tracking
            if (configuration.AutomaticViewTracking && Application.Current != null)
            {
                viewTracker = new DdAutoViewTracker(
                    configuration.ViewNamePredicate,
                    configuration.ViewTrackingPredicate);
                viewTracker.Start(Application.Current);
                InternalLog.Log("DdRum: Automatic view tracking enabled", SdkVerbosity.INFO);
            }

            // Start automatic action tracking
            if (configuration.AutomaticActionTracking && Application.Current != null)
            {
                actionTracker = new DdAutoActionTracker();
                actionTracker.Start(Application.Current);
                InternalLog.Log("DdRum: Automatic action tracking enabled", SdkVerbosity.INFO);
            }

            InternalLog.Log("DdRum.Enable completed", SdkVerbosity.DEBUG);
        }

        /// <summary>
        /// Report an error to the RUM monitor.
        /// If an ErrorEventMapper was set in DdRumConfiguration, it will be applied
        /// before sending. Return null from the mapper to drop the error.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="source">Error source.</param>
        /// <param name="stacktrace">Error stacktrace string.</param>
        /// <param name="context">Additional context attributes. Optional.</param>
        /// <param name="timestampMs">Timestamp in milliseconds since epoch. Optional (0 = use current time).</param>
        /// <param name="fingerprint">Custom fingerprint for error grouping. Optional. When set, overrides the default grouping.</param>
        public static void AddError(string message, RumErrorSource source, string stacktrace,
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

            var sourceStr = ConvertErrorSource(source);

#if ANDROID
            NativeDdRum.AddError(message, sourceStr, stacktrace, DdSdk.ToJavaDictionary(ctx), timestampMs);
#elif IOS
            NativeDdRum.AddError(message, sourceStr, stacktrace, DdSdk.ToNSDictionary(ctx), timestampMs);
#endif
        }

        // ── Views ────────────────────────────────────────────

        public static void StartView(string key, string name,
                                      Dictionary<string, object>? context = null, long timestampMs = 0)
        {
            InternalLog.Log($"DdRum.StartView: key={key}, name={name}", SdkVerbosity.DEBUG);
            var ctx = context ?? new Dictionary<string, object>();

            if (testBridge is not null) { testBridge.StartView(key, name, ctx, timestampMs); return; }

#if ANDROID
            NativeDdRum.StartView(key, name, DdSdk.ToJavaDictionary(ctx), timestampMs);
#elif IOS
            NativeDdRum.StartView(key, name, DdSdk.ToNSDictionary(ctx), timestampMs);
#endif
        }

        public static void StopView(string key,
                                     Dictionary<string, object>? context = null, long timestampMs = 0)
        {
            InternalLog.Log($"DdRum.StopView: key={key}", SdkVerbosity.DEBUG);
            var ctx = context ?? new Dictionary<string, object>();

            if (testBridge is not null) { testBridge.StopView(key, ctx, timestampMs); return; }

#if ANDROID
            NativeDdRum.StopView(key, DdSdk.ToJavaDictionary(ctx), timestampMs);
#elif IOS
            NativeDdRum.StopView(key, DdSdk.ToNSDictionary(ctx), timestampMs);
#endif
        }

        // ── Actions ──────────────────────────────────────────

        public static void StartAction(RumActionType type, string name,
                                        Dictionary<string, object>? context = null, long timestampMs = 0)
        {
            InternalLog.Log($"DdRum.StartAction: type={type}, name={name}", SdkVerbosity.DEBUG);
            var ctx = context ?? new Dictionary<string, object>();

            if (testBridge is not null) { testBridge.StartAction(type, name, ctx, timestampMs); return; }

            var typeStr = ConvertActionType(type);

#if ANDROID
            NativeDdRum.StartAction(typeStr, name, DdSdk.ToJavaDictionary(ctx), timestampMs);
#elif IOS
            NativeDdRum.StartAction(typeStr, name, DdSdk.ToNSDictionary(ctx), timestampMs);
#endif
        }

        public static void StopAction(RumActionType type, string name,
                                       Dictionary<string, object>? context = null, long timestampMs = 0)
        {
            InternalLog.Log($"DdRum.StopAction: type={type}, name={name}", SdkVerbosity.DEBUG);
            var ctx = context ?? new Dictionary<string, object>();

            if (testBridge is not null) { testBridge.StopAction(type, name, ctx, timestampMs); return; }

            var typeStr = ConvertActionType(type);

#if ANDROID
            NativeDdRum.StopAction(typeStr, name, DdSdk.ToJavaDictionary(ctx), timestampMs);
#elif IOS
            NativeDdRum.StopAction(typeStr, name, DdSdk.ToNSDictionary(ctx), timestampMs);
#endif
        }

        public static void AddAction(RumActionType type, string name,
                                      Dictionary<string, object>? context = null, long timestampMs = 0)
        {
            InternalLog.Log($"DdRum.AddAction: type={type}, name={name}", SdkVerbosity.DEBUG);
            var ctx = context ?? new Dictionary<string, object>();

            // Apply action event mapper if configured
            if (actionEventMapper != null)
            {
                try
                {
                    DdRumActionEvent actionEvent = new DdRumActionEvent(type, name, ctx, timestampMs);
                    DdRumActionEvent? mappedEvent = actionEventMapper(actionEvent);
                    if (mappedEvent == null)
                    {
                        InternalLog.Log($"DdRum.AddAction: Action dropped by ActionEventMapper", SdkVerbosity.DEBUG);
                        return;
                    }
                    type = mappedEvent.Type;
                    name = mappedEvent.Name;
                    ctx = mappedEvent.Context;
                    timestampMs = mappedEvent.TimestampMs;
                }
                catch (Exception ex)
                {
                    InternalLog.Log($"DdRum.AddAction: ActionEventMapper threw an exception, sending original action. {ex.Message}", SdkVerbosity.ERROR);
                }
            }

            if (testBridge is not null) { testBridge.AddAction(type, name, ctx, timestampMs); return; }

            var typeStr = ConvertActionType(type);

#if ANDROID
            NativeDdRum.AddAction(typeStr, name, DdSdk.ToJavaDictionary(ctx), timestampMs);
#elif IOS
            NativeDdRum.AddAction(typeStr, name, DdSdk.ToNSDictionary(ctx), timestampMs);
#endif
        }

        // ── Resources ────────────────────────────────────────

        public static void StartResource(string key, RumResourceMethod method, string url,
                                          Dictionary<string, object>? context = null, long timestampMs = 0)
        {
            InternalLog.Log($"DdRum.StartResource: key={key}, method={method}, url={url}", SdkVerbosity.DEBUG);
            var ctx = context ?? new Dictionary<string, object>();

            if (testBridge is not null) { testBridge.StartResource(key, method, url, ctx, timestampMs); return; }

            var methodStr = ConvertResourceMethod(method);

#if ANDROID
            NativeDdRum.StartResource(key, methodStr, url, DdSdk.ToJavaDictionary(ctx), timestampMs);
#elif IOS
            NativeDdRum.StartResource(key, methodStr, url, DdSdk.ToNSDictionary(ctx), timestampMs);
#endif
        }

        public static void StopResource(string key, int statusCode, RumResourceKind kind,
                                         long size = -1, Dictionary<string, object>? context = null, long timestampMs = 0)
        {
            InternalLog.Log($"DdRum.StopResource: key={key}, statusCode={statusCode}, kind={kind}", SdkVerbosity.DEBUG);
            var ctx = context ?? new Dictionary<string, object>();

            if (testBridge is not null) { testBridge.StopResource(key, statusCode, kind, size, ctx, timestampMs); return; }

            var kindStr = ConvertResourceKind(kind);

#if ANDROID
            NativeDdRum.StopResource(key, statusCode, kindStr, size, DdSdk.ToJavaDictionary(ctx), timestampMs);
#elif IOS
            NativeDdRum.StopResource(key, statusCode, kindStr, size, DdSdk.ToNSDictionary(ctx), timestampMs);
#endif
        }

        // ── Timing ───────────────────────────────────────────

        public static void AddTiming(string name)
        {
            InternalLog.Log($"DdRum.AddTiming: name={name}", SdkVerbosity.DEBUG);

            if (testBridge is not null) { testBridge.AddTiming(name); return; }

#if ANDROID
            NativeDdRum.AddTiming(name);
#elif IOS
            NativeDdRum.AddTiming(name);
#endif
        }

        public static void AddViewLoadingTime(bool overwrite)
        {
            InternalLog.Log($"DdRum.AddViewLoadingTime: overwrite={overwrite}", SdkVerbosity.DEBUG);

            if (testBridge is not null) { testBridge.AddViewLoadingTime(overwrite); return; }

#if ANDROID
            NativeDdRum.AddViewLoadingTime(overwrite);
#elif IOS
            NativeDdRum.AddViewLoadingTime(overwrite);
#endif
        }

        // ── Session ──────────────────────────────────────────

        public static void StopSession()
        {
            InternalLog.Log("DdRum.StopSession", SdkVerbosity.DEBUG);

            if (testBridge is not null) { testBridge.StopSession(); return; }

#if ANDROID
            NativeDdRum.StopSession();
#elif IOS
            NativeDdRum.StopSession();
#endif
        }

        // ── View Attributes ──────────────────────────────────

        public static void AddViewAttribute(string key, object value)
        {
            InternalLog.Log($"DdRum.AddViewAttribute: key={key}", SdkVerbosity.DEBUG);

            if (testBridge is not null) { testBridge.AddViewAttribute(key, value); return; }

#if ANDROID
            NativeDdRum.AddViewAttribute(key, DdSdk.ToJavaObject(value));
#elif IOS
            NativeDdRum.AddViewAttribute(key, DdSdk.ToNSObject(value));
#endif
        }

        public static void RemoveViewAttribute(string key)
        {
            InternalLog.Log($"DdRum.RemoveViewAttribute: key={key}", SdkVerbosity.DEBUG);

            if (testBridge is not null) { testBridge.RemoveViewAttribute(key); return; }

#if ANDROID
            NativeDdRum.RemoveViewAttribute(key);
#elif IOS
            NativeDdRum.RemoveViewAttribute(key);
#endif
        }

        public static void AddViewAttributes(Dictionary<string, object> attributes)
        {
            InternalLog.Log($"DdRum.AddViewAttributes: {attributes.Count} attributes", SdkVerbosity.DEBUG);

            if (testBridge is not null) { testBridge.AddViewAttributes(attributes); return; }

#if ANDROID
            NativeDdRum.AddViewAttributes(DdSdk.ToJavaDictionary(attributes));
#elif IOS
            NativeDdRum.AddViewAttributes(DdSdk.ToNSDictionary(attributes));
#endif
        }

        public static void RemoveViewAttributes(List<string> keys)
        {
            InternalLog.Log($"DdRum.RemoveViewAttributes: {keys.Count} keys", SdkVerbosity.DEBUG);

            if (testBridge is not null) { testBridge.RemoveViewAttributes(keys); return; }

#if ANDROID
            NativeDdRum.RemoveViewAttributes(keys);
#elif IOS
            var iosArray = NSArray.FromStrings(keys.ToArray());
            NativeDdRum.RemoveViewAttributes(iosArray);
#endif
        }
        // ── Enum-to-string conversions ─────────────────────

        private static string ConvertActionType(RumActionType type) => type switch
        {
            RumActionType.Tap => "tap",
            RumActionType.Scroll => "scroll",
            RumActionType.Swipe => "swipe",
            RumActionType.Click => "click",
            RumActionType.Back => "back",
            RumActionType.ApplicationStart => "application_start",
            _ => "custom"
        };

        private static string ConvertResourceMethod(RumResourceMethod method) => method switch
        {
            RumResourceMethod.Get => "get",
            RumResourceMethod.Post => "post",
            RumResourceMethod.Put => "put",
            RumResourceMethod.Delete => "delete",
            RumResourceMethod.Head => "head",
            RumResourceMethod.Patch => "patch",
            RumResourceMethod.Connect => "connect",
            RumResourceMethod.Trace => "trace",
            RumResourceMethod.Options => "options",
            _ => "get"
        };

        private static string ConvertResourceKind(RumResourceKind kind) => kind switch
        {
            RumResourceKind.Xhr => "xhr",
            RumResourceKind.Native => "native",
            RumResourceKind.Fetch => "fetch",
            RumResourceKind.Document => "document",
            RumResourceKind.Beacon => "beacon",
            RumResourceKind.Image => "image",
            RumResourceKind.Font => "font",
            RumResourceKind.Css => "css",
            RumResourceKind.Media => "media",
            RumResourceKind.Js => "js",
            _ => "other"
        };

        private static string ConvertErrorSource(RumErrorSource source) => source switch
        {
            RumErrorSource.Network => "network",
            RumErrorSource.Source => "source",
            RumErrorSource.Console => "console",
            RumErrorSource.Logger => "logger",
            RumErrorSource.Agent => "agent",
            RumErrorSource.Webview => "webview",
            RumErrorSource.Report => "report",
            _ => "custom"
        };
    }
}
