#if ANDROID
namespace Datadog.Maui;

/// <summary>
/// Android-specific partial implementation for crash reporting.
/// </summary>
public static partial class CrashReporting
{
    /// <inheritdoc/>
    public static partial bool EnableNative()
    {
        return Com.Datadog.Maui.DatadogMauiWrapper.EnableCrashReporting();
    }
}
#endif
