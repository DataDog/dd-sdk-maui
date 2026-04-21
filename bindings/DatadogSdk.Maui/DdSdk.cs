#if ANDROID
using NativeDatadogWrapper = DatadogSdk.Android.Binding.DatadogWrapper;
#elif IOS
using Foundation;
using NativeDatadogWrapper = DatadogSdk.iOS.Binding.DatadogWrapper;
#endif
using System.Collections.Concurrent;
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
            void AddAttribute(string key, object value);
            void AddAttributes(Dictionary<string, object> attributes);
            void RemoveAttribute(string key);
            void RemoveAttributes(List<string> keys);
        }

        internal static DdSdkConfiguration? Configuration { get; private set; }
        internal static INativeBridge? testBridge;
        private static readonly ConcurrentDictionary<string, object> _globalAttributes = new();

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
                androidConfig = ToJavaDictionary(mergedConfig);
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
                    iosConfig = ToNSDictionary(mergedConfig);
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

        /// <summary>
        /// Add a global attribute that will be attached to all future events.
        /// The attribute is stored both locally (for C#-level access) and on the native SDK.
        /// </summary>
        public static void AddAttribute(string key, object value)
        {
            InternalLog.Log($"DdSdk.AddAttribute: {key}", SdkVerbosity.DEBUG);

            _globalAttributes[key] = value;

            if (testBridge is not null)
            {
                testBridge.AddAttribute(key, value);
                return;
            }

#if ANDROID
            NativeDatadogWrapper.AddAttribute(key, ToJavaObject(value));
#elif IOS
            NativeDatadogWrapper.AddAttribute(key, ToNSObject(value));
#endif
        }

        /// <summary>
        /// Add multiple global attributes at once.
        /// Each attribute is stored both locally and on the native SDK.
        /// </summary>
        public static void AddAttributes(Dictionary<string, object> attributes)
        {
            InternalLog.Log($"DdSdk.AddAttributes: {attributes.Count} attributes", SdkVerbosity.DEBUG);

            foreach (var kvp in attributes)
            {
                _globalAttributes[kvp.Key] = kvp.Value;
            }

            if (testBridge is not null)
            {
                testBridge.AddAttributes(attributes);
                return;
            }

#if ANDROID
            NativeDatadogWrapper.AddAttributes(ToJavaDictionary(attributes));
#elif IOS
            NativeDatadogWrapper.AddAttributes(ToNSDictionary(attributes));
#endif
        }

        /// <summary>
        /// Remove a previously added global attribute.
        /// The attribute is removed both locally and from the native SDK.
        /// </summary>
        public static void RemoveAttribute(string key)
        {
            InternalLog.Log($"DdSdk.RemoveAttribute: {key}", SdkVerbosity.DEBUG);

            _globalAttributes.TryRemove(key, out _);

            if (testBridge is not null)
            {
                testBridge.RemoveAttribute(key);
                return;
            }

#if ANDROID
            NativeDatadogWrapper.RemoveAttribute(key);
#elif IOS
            NativeDatadogWrapper.RemoveAttribute(key);
#endif
        }

        /// <summary>
        /// Remove multiple global attributes at once.
        /// Each attribute is removed both locally and from the native SDK.
        /// </summary>
        public static void RemoveAttributes(List<string> keys)
        {
            InternalLog.Log($"DdSdk.RemoveAttributes: {keys.Count} keys", SdkVerbosity.DEBUG);

            foreach (var key in keys)
            {
                _globalAttributes.TryRemove(key, out _);
            }

            if (testBridge is not null)
            {
                testBridge.RemoveAttributes(keys);
                return;
            }

#if ANDROID
            NativeDatadogWrapper.RemoveAttributes(keys);
#elif IOS
            var iosArray = NSArray.FromStrings(keys.ToArray());
            NativeDatadogWrapper.RemoveAttributes(iosArray);
#endif
        }

        /// <summary>
        /// Returns a snapshot of all currently set global attributes.
        /// Returns a copy — mutations to the returned dictionary do not affect the SDK state.
        /// </summary>
        public static Dictionary<string, object> GetAttributes()
        {
            return new Dictionary<string, object>(_globalAttributes);
        }

        /// <summary>
        /// Clears all local attributes. For testing only.
        /// </summary>
        internal static void ClearAttributesForTesting()
        {
            _globalAttributes.Clear();
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

#if ANDROID
        /// <summary>
        /// Recursively converts a C# object to its Java equivalent.
        /// Supports primitives, strings, Dictionary → HashMap, and List → ArrayList.
        /// </summary>
        internal static Java.Lang.Object ToJavaObject(object? value)
        {
            return value switch
            {
                string s => new Java.Lang.String(s),
                int i => Java.Lang.Integer.ValueOf(i),
                bool b => Java.Lang.Boolean.ValueOf(b),
                long l => Java.Lang.Long.ValueOf(l),
                double d => Java.Lang.Double.ValueOf(d),
                IDictionary<string, object> dict => ToJavaHashMap(dict),
                System.Collections.IDictionary dict => ToJavaHashMapFromNonGeneric(dict),
                System.Collections.IEnumerable list => ToJavaList(list),
                _ => new Java.Lang.String(value?.ToString() ?? "")
            };
        }

        private static Java.Util.HashMap ToJavaHashMap(IDictionary<string, object> dict)
        {
            var map = new Java.Util.HashMap();
            foreach (var kvp in dict)
            {
                map.Put(kvp.Key, ToJavaObject(kvp.Value));
            }
            return map;
        }

        private static Java.Util.HashMap ToJavaHashMapFromNonGeneric(System.Collections.IDictionary dict)
        {
            var map = new Java.Util.HashMap();
            foreach (System.Collections.DictionaryEntry entry in dict)
            {
                map.Put(entry.Key?.ToString() ?? "", ToJavaObject(entry.Value));
            }
            return map;
        }

        internal static IDictionary<string, Java.Lang.Object> ToJavaDictionary(IDictionary<string, object> dict)
        {
            var result = new Dictionary<string, Java.Lang.Object>();
            foreach (var kvp in dict)
            {
                result[kvp.Key] = ToJavaObject(kvp.Value);
            }
            return result;
        }

        private static Java.Lang.Object ToJavaList(System.Collections.IEnumerable list)
        {
            var arrayList = new Java.Util.ArrayList();
            foreach (var item in list)
            {
                arrayList.Add(ToJavaObject(item));
            }
            return arrayList;
        }
#elif IOS
        /// <summary>
        /// Recursively converts a C# object to its NSObject equivalent.
        /// Supports primitives, strings, Dictionary → NSDictionary, and List → NSArray.
        /// </summary>
        internal static NSObject ToNSObject(object? value)
        {
            return value switch
            {
                null => NSNull.Null,
                string s => new NSString(s),
                bool b => NSNumber.FromBoolean(b),
                int i => NSNumber.FromInt32(i),
                long l => NSNumber.FromInt64(l),
                double d => NSNumber.FromDouble(d),
                float f => NSNumber.FromFloat(f),
                IDictionary<string, object> dict => ToNSDictionary(dict),
                System.Collections.IDictionary dict => ToNSDictionaryFromNonGeneric(dict),
                System.Collections.IEnumerable list => ToNSArray(list),
                _ => new NSString(value.ToString() ?? "")
            };
        }

        internal static NSDictionary ToNSDictionary(IDictionary<string, object> dict)
        {
            var keys = dict.Keys.Select(k => (NSObject)new NSString(k)).ToArray();
            var values = dict.Values.Select(v => ToNSObject(v)).ToArray();
            return NSDictionary.FromObjectsAndKeys(values, keys);
        }

        private static NSDictionary ToNSDictionaryFromNonGeneric(System.Collections.IDictionary dict)
        {
            var keys = new System.Collections.Generic.List<NSObject>();
            var values = new System.Collections.Generic.List<NSObject>();
            foreach (System.Collections.DictionaryEntry entry in dict)
            {
                keys.Add(new NSString(entry.Key?.ToString() ?? ""));
                values.Add(ToNSObject(entry.Value));
            }
            return NSDictionary.FromObjectsAndKeys(values.ToArray(), keys.ToArray());
        }

        private static NSArray ToNSArray(System.Collections.IEnumerable list)
        {
            var items = new System.Collections.Generic.List<NSObject>();
            foreach (var item in list)
            {
                items.Add(ToNSObject(item));
            }
            return NSArray.FromNSObjects(items.ToArray());
        }
#endif
    }
}
