#if ANDROID
using NativeDdRum = DatadogSdk.Android.Binding.DdRum;
#elif IOS
using Foundation;
using NativeDdRum = DatadogSdk.iOS.Binding.DdRum;
#endif

using DatadogSdk.Maui.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace DatadogSdk.Maui
{
    public static class DdRum
    {
        /// <summary>
        /// Enable the RUM module with the provided configuration.
        /// Must be called after DdSdk.Initialize().
        /// </summary>
        /// <param name="configuration">Configuration for the RUM module.</param>
        [RequiresUnreferencedCode("Calls DdRumConfiguration methods that use JSON serialization")]
        public static void Enable(DdRumConfiguration configuration)
        {
            InternalLog.Log($"DdRum.Enable called with applicationId: {configuration.ApplicationId}", SdkVerbosity.DEBUG);

            // Warn about stub event mappers
            if (configuration.ErrorEventMapper != null)
                InternalLog.Log("DdRum: ErrorEventMapper is not yet supported and will be ignored.", SdkVerbosity.WARN);
            if (configuration.ResourceEventMapper != null)
                InternalLog.Log("DdRum: ResourceEventMapper is not yet supported and will be ignored.", SdkVerbosity.WARN);
            if (configuration.ActionEventMapper != null)
                InternalLog.Log("DdRum: ActionEventMapper is not yet supported and will be ignored.", SdkVerbosity.WARN);

            var dict = configuration.ToDictionary();

#if ANDROID
            var androidDict = new Dictionary<string, Java.Lang.Object>();
            foreach (var kvp in dict)
            {
                androidDict[kvp.Key] = kvp.Value switch
                {
                    string s => new Java.Lang.String(s),
                    bool b => Java.Lang.Boolean.ValueOf(b),
                    double d => Java.Lang.Double.ValueOf(d),
                    int i => Java.Lang.Integer.ValueOf(i),
                    long l => Java.Lang.Long.ValueOf(l),
                    _ => new Java.Lang.String(kvp.Value?.ToString() ?? "")
                };
            }
            NativeDdRum.EnableRum(androidDict);
#elif IOS
            var keys = dict.Keys.Select(k => (NSObject)new NSString(k)).ToArray();
            var values = dict.Values.Select(v => NSObject.FromObject(v)).ToArray();
            var nsDict = NSDictionary.FromObjectsAndKeys(values, keys);
            NativeDdRum.EnableRum(nsDict);
#endif
            InternalLog.Log("DdRum.Enable completed", SdkVerbosity.DEBUG);
        }
    }
}
