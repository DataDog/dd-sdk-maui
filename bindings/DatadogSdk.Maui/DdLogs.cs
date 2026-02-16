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
            InternalLog.Log("DdLogs.Enable", SdkVerbosity.DEBUG);

            NativeDdLogs.EnableLogs();

            InternalLog.Log("DdLogs.Enable completed", SdkVerbosity.DEBUG);
        }

        public static void Debug(string message)
        {
            InternalLog.Log($"DdLogs.Debug called: {message}", SdkVerbosity.DEBUG);

            NativeDdLogs.LogDebug(message);
        }

        public static void Info(string message)
        {
            InternalLog.Log($"DdLogs.Info called: {message}", SdkVerbosity.DEBUG);

            NativeDdLogs.LogInfo(message);
        }

        public static void Warn(string message)
        {
            InternalLog.Log($"DdLogs.Warn called: {message}", SdkVerbosity.DEBUG);

            NativeDdLogs.LogWarn(message);
        }

        public static void Error(string message)
        {
            InternalLog.Log($"DdLogs.Error called: {message}", SdkVerbosity.DEBUG);

            NativeDdLogs.LogError(message);
        }

        public static void LogWithAttributes(string level, string message, Dictionary<string, string> attributes)
        {
            InternalLog.Log($"DdLogs.LogWithAttributes called: level={level}, message={message}, attributes count={attributes.Count}", SdkVerbosity.DEBUG);

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
