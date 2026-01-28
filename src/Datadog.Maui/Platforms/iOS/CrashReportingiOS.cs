#if IOS
using System.Runtime.InteropServices;
using ObjCRuntime;

namespace Datadog.Maui;

/// <summary>
/// iOS-specific partial implementation for crash reporting.
/// </summary>
public static partial class CrashReporting
{
    /// <inheritdoc/>
    public static partial bool EnableNative()
    {
        try
        {
            return CrashReportingNativeWrapper.EnableCrashReporting();
        }
        catch (DllNotFoundException)
        {
            System.Diagnostics.Debug.WriteLine(
                "[Datadog.Maui] Native iOS wrapper not available for crash reporting. " +
                "Build the Swift package with Xcode and link as NativeReference.");
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            System.Diagnostics.Debug.WriteLine(
                "[Datadog.Maui] Native crash reporting method not found. " +
                "Ensure DatadogMauiWrapper is properly exported.");
            return false;
        }
    }
}

/// <summary>
/// Native wrapper binding for crash reporting methods in DatadogMauiWrapper Swift class.
/// </summary>
internal static class CrashReportingNativeWrapper
{
    private static readonly Selector EnableCrashReportingSelector = new Selector("enableCrashReporting");

    private static IntPtr ClassHandle
    {
        get
        {
            var handle = Class.GetHandle("DDMauiWrapper");
            if (handle == IntPtr.Zero)
            {
                throw new DllNotFoundException("DDMauiWrapper class not found");
            }
            return handle;
        }
    }

    public static bool EnableCrashReporting()
    {
        return CrashReportingMessaging.bool_objc_msgSend(
            ClassHandle,
            EnableCrashReportingSelector.Handle);
    }
}

/// <summary>
/// ObjC messaging helpers for crash reporting native wrapper.
/// </summary>
internal static class CrashReportingMessaging
{
    private const string LIBOBJC_DYLIB = "/usr/lib/libobjc.dylib";

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    public static extern bool bool_objc_msgSend(
        IntPtr receiver,
        IntPtr selector);
}
#endif
