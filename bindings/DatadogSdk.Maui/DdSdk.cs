#if ANDROID
using NativeDatadogWrapper = DatadogSdk.Android.Binding.DatadogWrapper;
#elif IOS
using Foundation;
using NativeDatadogWrapper = DatadogSdk.iOS.Binding.DatadogWrapper;
#endif
using System.Collections.Generic;
using System.Linq;
using DatadogSdk.Maui.Configuration;

namespace DatadogSdk.Maui
{
    public static class DdSdk
    {
        internal static DdSdkConfiguration? Configuration { get; private set; }

        public static bool Initialize(DdSdkConfiguration config)
        {
            Configuration = config;
            InternalLog.Verbosity = config.Verbosity;

            // Convert enums to lowercase strings for native bridge
            var verbosity = (config.Verbosity ?? SdkVerbosity.ERROR).ToString().ToLowerInvariant();
            var site = ConvertSite(config.Site);
            var trackingConsent = ConvertTrackingConsent(config.TrackingConsent);
            var batchSize = config.BatchSize?.ToString().ToLowerInvariant();
            var uploadFrequency = config.UploadFrequency?.ToString().ToLowerInvariant();
            var batchProcessingLevel = config.BatchProcessingLevel?.ToString().ToLowerInvariant();

            InternalLog.Log(
                $"DdSdk.Initialize called with env={config.Environment}, site={site}, consent={trackingConsent}",
                SdkVerbosity.DEBUG
            );

            // Merge version/versionSuffix into additionalConfiguration
            var mergedConfig = config.AdditionalConfiguration != null
                ? new Dictionary<string, object>(config.AdditionalConfiguration)
                : null;

            if (config.Version != null)
            {
                mergedConfig ??= new Dictionary<string, object>();
                mergedConfig["_dd.version"] = config.Version;
            }

            if (config.VersionSuffix != null)
            {
                mergedConfig ??= new Dictionary<string, object>();
                mergedConfig["_dd.version_suffix"] = config.VersionSuffix;
            }

            var sdkInitialized = false;

#if ANDROID
            var context = global::Android.App.Application.Context;

            IDictionary<string, Java.Lang.Object>? androidConfig = null;
            if (mergedConfig != null)
            {
                androidConfig = new Dictionary<string, Java.Lang.Object>();
                foreach (var kvp in mergedConfig)
                {
                    androidConfig[kvp.Key] = kvp.Value switch
                    {
                        string s => new Java.Lang.String(s),
                        int i => new Java.Lang.Integer(i),
                        bool b => new Java.Lang.Boolean(b),
                        long l => new Java.Lang.Long(l),
                        double d => new Java.Lang.Double(d),
                        _ => new Java.Lang.String(kvp.Value?.ToString() ?? "")
                    };
                }
            }

            sdkInitialized = NativeDatadogWrapper.Initialize(
                context,
                config.ClientToken,
                config.Environment,
                config.Service,
                site,
                verbosity,
                trackingConsent,
                batchSize,
                uploadFrequency,
                batchProcessingLevel,
                androidConfig
            );
#elif IOS
            NSDictionary? iosConfig = null;
            if (mergedConfig != null)
            {
                iosConfig = NSDictionary.FromObjectsAndKeys(
                    mergedConfig.Values.Select(v => NSObject.FromObject(v)).ToArray(),
                    mergedConfig.Keys.Select(k => (NSObject)new NSString(k)).ToArray()
                );
            }

            sdkInitialized = NativeDatadogWrapper.Initialize(
                config.ClientToken,
                config.Environment,
                config.Service,
                site,
                verbosity,
                trackingConsent,
                batchSize,
                uploadFrequency,
                batchProcessingLevel,
                iosConfig
            );
#endif

            InternalLog.Log($"DdSdk.Initialize completed: {sdkInitialized}", SdkVerbosity.INFO);
            return sdkInitialized;
        }

        internal static string ConvertSite(DatadogSite site) => site switch
        {
            DatadogSite.Us1 => "us1",
            DatadogSite.Us3 => "us3",
            DatadogSite.Us5 => "us5",
            DatadogSite.Eu1 => "eu1",
            DatadogSite.Ap1 => "ap1",
            DatadogSite.Ap2 => "ap2",
            DatadogSite.Us1Fed => "us1_fed",
            _ => "us1"
        };

        internal static string ConvertTrackingConsent(TrackingConsent consent) => consent switch
        {
            TrackingConsent.Granted => "granted",
            TrackingConsent.NotGranted => "not_granted",
            TrackingConsent.Pending => "pending",
            _ => "granted"
        };
    }
}
