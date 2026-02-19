#if ANDROID
using NativeDdLogs = DatadogSdk.Android.Binding.DdLogs;
#elif IOS
using Foundation;
using NativeDdLogs = DatadogSdk.iOS.Binding.DdLogs;
#endif

namespace DatadogSdk.Maui
{
    public static class DdLogs
    {
        public static void Enable()
        {
            DdSdk.LogDebug("DdLogs.Enable called");

            NativeDdLogs.EnableLogs();

            DdSdk.LogDebug("DdLogs.Enable completed");
        }

        public static void Debug(string message)
        {
            DdSdk.LogDebug($"DdLogs.Debug called: {message}");

            NativeDdLogs.LogDebug(message);
        }

        public static void Info(string message)
        {
            DdSdk.LogDebug($"DdLogs.Info called: {message}");

            NativeDdLogs.LogInfo(message);
        }

        public static void Warn(string message)
        {
            DdSdk.LogDebug($"DdLogs.Warn called: {message}");

            NativeDdLogs.LogWarn(message);
        }

        public static void Error(string message)
        {
            DdSdk.LogDebug($"DdLogs.Error called: {message}");

            NativeDdLogs.LogError(message);
        }

        public static void LogWithAttributes(string level, string message, Dictionary<string, string> attributes)
        {
            DdSdk.LogDebug($"DdLogs.LogWithAttributes called: level={level}, message={message}, attributes count={attributes.Count}");

#if ANDROID
            NativeDdLogs.LogWithAttributes(level, message, attributes);
#elif IOS
            var keys = attributes.Keys.Select(k => new NSString(k)).ToArray();
            var values = attributes.Values.Select(v => new NSString(v)).ToArray();
            var nsDict = NSDictionary<NSString, NSString>.FromObjectsAndKeys(values, keys);
            NativeDdLogs.LogWithAttributes(level, message, nsDict);
#endif
        }
    }
}
