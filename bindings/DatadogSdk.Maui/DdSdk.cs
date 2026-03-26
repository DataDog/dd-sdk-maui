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
        internal interface INativeBridge
        {
            bool Initialize(
                string clientToken,
                string environment,
                string? service,
                string site,
                string verbosity,
                string trackingConsent,
                string? batchSize,
                string? uploadFrequency,
                string? batchProcessingLevel,
                Dictionary<string, object>? additionalConfiguration);

            void SetTrackingConsent(string consent);
        }

        internal static DdSdkConfiguration? Configuration { get; private set; }
        internal static INativeBridge? testBridge;

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

            var mergedConfig = BuildAdditionalConfiguration(
                config.AdditionalConfiguration,
                config.Version,
                config.VersionSuffix
            );

            var sdkInitialized = false;

            if (testBridge is not null)
            {
                sdkInitialized = testBridge.Initialize(
                    config.ClientToken,
                    config.Environment,
                    config.Service,
                    site,
                    verbosity,
                    trackingConsent,
                    batchSize,
                    uploadFrequency,
                    batchProcessingLevel,
                    mergedConfig);
            }
            else
            {
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
                        int i => Java.Lang.Integer.ValueOf(i),
                        bool b => Java.Lang.Boolean.ValueOf(b),
                        long l => Java.Lang.Long.ValueOf(l),
                        double d => Java.Lang.Double.ValueOf(d),
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
            }

            InternalLog.Log($"DdSdk.Initialize completed: {sdkInitialized}", SdkVerbosity.INFO);
            return sdkInitialized;
        }

        public static void SetTrackingConsent(TrackingConsent consent)
        {
            var consentString = ConvertTrackingConsent(consent);

            InternalLog.Log(
                $"Setting tracking consent to {consentString}",
                SdkVerbosity.DEBUG
            );

            if (testBridge is not null)
            {
                testBridge.SetTrackingConsent(consentString);
                return;
            }

#if ANDROID
            NativeDatadogWrapper.SetTrackingConsent(consentString);
#elif IOS
            NativeDatadogWrapper.SetTrackingConsent(consentString);
#endif
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
            _ => "pending"
        };

        /// Merges Version and VersionSuffix into the additionalConfiguration dictionary
        /// as the reserved keys _dd.version and _dd.version_suffix.
        /// Any other internal keys are passed directly
        /// via AdditionalConfiguration and flow through unchanged.
        /// Returns null when all three inputs are null/empty.
        internal static Dictionary<string, object>? BuildAdditionalConfiguration(
            Dictionary<string, object>? additionalConfiguration,
            string? version,
            string? versionSuffix)
        {
            Dictionary<string, object>? merged = additionalConfiguration != null
                ? new(additionalConfiguration)
                : null;

            if (version != null)
            {
                merged ??= [];
                merged["_dd.version"] = version;
            }

            if (versionSuffix != null)
            {
                merged ??= [];
                merged["_dd.version_suffix"] = versionSuffix;
            }

            return merged;
        }
    }
}
