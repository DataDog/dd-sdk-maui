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

            var verbosity = config.Verbosity.ToString().ToLowerInvariant();

            LogDebug($"DdSdk.Initialize called with service={config.Service}, env={config.Environment}, site={config.Site}, verbosity={verbosity}");

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

            LogDebug($"DdSdk.Initialize completed: {result}");
            return result;
        }

        internal static void LogDebug(string message)
        {
            if (Configuration?.Verbosity == SdkVerbosity.DEBUG)
            {
                Console.WriteLine($"[Datadog] {message}");
            }
        }
    }
}
