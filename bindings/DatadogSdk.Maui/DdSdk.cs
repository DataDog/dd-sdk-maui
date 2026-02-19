#if ANDROID
using NativeDatadogWrapper = DatadogSdk.Android.Binding.DatadogWrapper;
#elif IOS
using NativeDatadogWrapper = DatadogSdk.iOS.Binding.DatadogWrapper;
#endif

namespace DatadogSdk.Maui
{
    public static class DdSdk
    {
        internal static DdSdkConfiguration? Configuration { get; private set; }

        public static bool Initialize(DdSdkConfiguration config)
        {
            Configuration = config;

            InternalLog.Verbosity = config.Verbosity;

            var verbosity = (config.Verbosity ?? SdkVerbosity.ERROR).ToString().ToLowerInvariant();

            InternalLog.Log($"DdSdk.Initialize called with service={config.Service}, env={config.Environment}, site={config.Site}", SdkVerbosity.DEBUG);

#if ANDROID
            var context = global::Android.App.Application.Context;
            var result = NativeDatadogWrapper.Initialize(
                context,
                config.ClientToken,
                config.Environment,
                config.Service,
                config.Site,
                verbosity
            );
#elif IOS
            var result = NativeDatadogWrapper.Initialize(
                config.ClientToken,
                config.Environment,
                config.Service,
                config.Site,
                verbosity
            );
#endif

            InternalLog.Log($"DdSdk.Initialize completed: {result}", SdkVerbosity.INFO);
            return result;
        }
    }
}
