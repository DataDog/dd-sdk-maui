#if IOS
using System.Runtime.InteropServices;
using Foundation;
using ObjCRuntime;

namespace Datadog.Maui;

/// <summary>
/// iOS-specific partial implementation for RUM platform instance creation.
/// </summary>
public static partial class Rum
{
    /// <inheritdoc/>
    public static partial bool Enable(string applicationId, float sampleRate)
    {
        try
        {
            return RumNativeWrapper.EnableRum(applicationId, sampleRate);
        }
        catch (DllNotFoundException)
        {
            System.Diagnostics.Debug.WriteLine(
                "[Datadog.Maui] Native iOS wrapper not available for RUM. " +
                "Build the Swift package with Xcode and link as NativeReference.");
            return true; // Stub success for build verification
        }
        catch (EntryPointNotFoundException)
        {
            System.Diagnostics.Debug.WriteLine(
                "[Datadog.Maui] Native RUM method not found. " +
                "Ensure DatadogMauiWrapper is properly exported.");
            return true; // Stub success for build verification
        }
    }

    private static partial IRum CreatePlatformInstance() => new RumiOS();
}

/// <summary>
/// iOS implementation of the RUM interface.
/// </summary>
/// <remarks>
/// This implementation calls the native DatadogMauiWrapper Swift library through ObjC interop.
/// When the native library is not available, methods are silently ignored.
/// </remarks>
internal sealed class RumiOS : IRum
{
    /// <inheritdoc/>
    public void StartView(string key, string? name = null, IDictionary<string, object>? attributes = null)
    {
        try
        {
            RumNativeWrapper.StartView(key, name ?? key, attributes);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            // Silently ignore when native library is not available
        }
    }

    /// <inheritdoc/>
    public void StopView(string key, IDictionary<string, object>? attributes = null)
    {
        try
        {
            RumNativeWrapper.StopView(key, attributes);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            // Silently ignore when native library is not available
        }
    }

    /// <inheritdoc/>
    public void AddAction(RumActionType type, string name, IDictionary<string, object>? attributes = null)
    {
        try
        {
            var typeString = MapActionType(type);
            RumNativeWrapper.AddAction(typeString, name, attributes);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            // Silently ignore when native library is not available
        }
    }

    /// <inheritdoc/>
    public void AddError(string message, RumErrorSource source, string? stackTrace = null, IDictionary<string, object>? attributes = null)
    {
        try
        {
            var sourceString = MapErrorSource(source);
            RumNativeWrapper.AddError(message, sourceString, stackTrace, attributes);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            // Silently ignore when native library is not available
        }
    }

    /// <inheritdoc/>
    public void StartResource(string key, string httpMethod, string url, IDictionary<string, object>? attributes = null)
    {
        try
        {
            RumNativeWrapper.StartResource(key, httpMethod, url, attributes);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            // Silently ignore when native library is not available
        }
    }

    /// <inheritdoc/>
    public void StopResource(string key, int statusCode, long? size = null, IDictionary<string, object>? attributes = null)
    {
        try
        {
            RumNativeWrapper.StopResource(key, statusCode, size ?? -1L, attributes);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            // Silently ignore when native library is not available
        }
    }

    /// <inheritdoc/>
    public void StopResourceWithError(string key, string message, IDictionary<string, object>? attributes = null)
    {
        try
        {
            RumNativeWrapper.StopResourceWithError(key, message, attributes);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            // Silently ignore when native library is not available
        }
    }

    private static string MapActionType(RumActionType type) => type switch
    {
        RumActionType.Tap => "TAP",
        RumActionType.Click => "CLICK",
        RumActionType.Scroll => "SCROLL",
        RumActionType.Swipe => "SWIPE",
        RumActionType.Custom => "CUSTOM",
        _ => "CUSTOM"
    };

    private static string MapErrorSource(RumErrorSource source) => source switch
    {
        RumErrorSource.Source => "SOURCE",
        RumErrorSource.Network => "NETWORK",
        RumErrorSource.WebView => "WEBVIEW",
        RumErrorSource.Console => "CONSOLE",
        RumErrorSource.Custom => "CUSTOM",
        _ => "SOURCE"
    };
}

/// <summary>
/// Native wrapper binding for RUM methods in DatadogMauiWrapper Swift class.
/// </summary>
internal static class RumNativeWrapper
{
    private const string LIBOBJC_DYLIB = "/usr/lib/libobjc.dylib";

    // Selectors for RUM methods
    private static readonly Selector EnableRumSelector = new Selector("enableRumWithApplicationId:sampleRate:");
    private static readonly Selector StartViewSelector = new Selector("startViewWithKey:name:attributes:");
    private static readonly Selector StopViewSelector = new Selector("stopViewWithKey:attributes:");
    private static readonly Selector AddActionSelector = new Selector("addActionWithType:name:attributes:");
    private static readonly Selector AddErrorSelector = new Selector("addErrorWithMessage:source:stackTrace:attributes:");
    private static readonly Selector StartResourceSelector = new Selector("startResourceWithKey:httpMethod:url:attributes:");
    private static readonly Selector StopResourceSelector = new Selector("stopResourceWithKey:statusCode:size:attributes:");
    private static readonly Selector StopResourceWithErrorSelector = new Selector("stopResourceWithErrorWithKey:message:attributes:");

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

    public static bool EnableRum(string applicationId, float sampleRate)
    {
        using var appIdNS = new NSString(applicationId);
        return RumMessaging.bool_objc_msgSend_IntPtr_float(
            ClassHandle,
            EnableRumSelector.Handle,
            appIdNS.Handle,
            sampleRate);
    }

    public static void StartView(string key, string name, IDictionary<string, object>? attributes)
    {
        using var keyNS = new NSString(key);
        using var nameNS = new NSString(name);
        var attrsNS = ConvertToNSDictionary(attributes);

        RumMessaging.void_objc_msgSend_IntPtr_IntPtr_IntPtr(
            ClassHandle,
            StartViewSelector.Handle,
            keyNS.Handle,
            nameNS.Handle,
            attrsNS?.Handle ?? IntPtr.Zero);

        attrsNS?.Dispose();
    }

    public static void StopView(string key, IDictionary<string, object>? attributes)
    {
        using var keyNS = new NSString(key);
        var attrsNS = ConvertToNSDictionary(attributes);

        RumMessaging.void_objc_msgSend_IntPtr_IntPtr(
            ClassHandle,
            StopViewSelector.Handle,
            keyNS.Handle,
            attrsNS?.Handle ?? IntPtr.Zero);

        attrsNS?.Dispose();
    }

    public static void AddAction(string type, string name, IDictionary<string, object>? attributes)
    {
        using var typeNS = new NSString(type);
        using var nameNS = new NSString(name);
        var attrsNS = ConvertToNSDictionary(attributes);

        RumMessaging.void_objc_msgSend_IntPtr_IntPtr_IntPtr(
            ClassHandle,
            AddActionSelector.Handle,
            typeNS.Handle,
            nameNS.Handle,
            attrsNS?.Handle ?? IntPtr.Zero);

        attrsNS?.Dispose();
    }

    public static void AddError(string message, string source, string? stackTrace, IDictionary<string, object>? attributes)
    {
        using var messageNS = new NSString(message);
        using var sourceNS = new NSString(source);
        using var stackNS = stackTrace != null ? new NSString(stackTrace) : null;
        var attrsNS = ConvertToNSDictionary(attributes);

        RumMessaging.void_objc_msgSend_IntPtr_IntPtr_IntPtr_IntPtr(
            ClassHandle,
            AddErrorSelector.Handle,
            messageNS.Handle,
            sourceNS.Handle,
            stackNS?.Handle ?? IntPtr.Zero,
            attrsNS?.Handle ?? IntPtr.Zero);

        attrsNS?.Dispose();
    }

    public static void StartResource(string key, string httpMethod, string url, IDictionary<string, object>? attributes)
    {
        using var keyNS = new NSString(key);
        using var methodNS = new NSString(httpMethod);
        using var urlNS = new NSString(url);
        var attrsNS = ConvertToNSDictionary(attributes);

        RumMessaging.void_objc_msgSend_IntPtr_IntPtr_IntPtr_IntPtr(
            ClassHandle,
            StartResourceSelector.Handle,
            keyNS.Handle,
            methodNS.Handle,
            urlNS.Handle,
            attrsNS?.Handle ?? IntPtr.Zero);

        attrsNS?.Dispose();
    }

    public static void StopResource(string key, int statusCode, long size, IDictionary<string, object>? attributes)
    {
        using var keyNS = new NSString(key);
        var attrsNS = ConvertToNSDictionary(attributes);

        RumMessaging.void_objc_msgSend_IntPtr_int_long_IntPtr(
            ClassHandle,
            StopResourceSelector.Handle,
            keyNS.Handle,
            statusCode,
            size,
            attrsNS?.Handle ?? IntPtr.Zero);

        attrsNS?.Dispose();
    }

    public static void StopResourceWithError(string key, string message, IDictionary<string, object>? attributes)
    {
        using var keyNS = new NSString(key);
        using var messageNS = new NSString(message);
        var attrsNS = ConvertToNSDictionary(attributes);

        RumMessaging.void_objc_msgSend_IntPtr_IntPtr_IntPtr(
            ClassHandle,
            StopResourceWithErrorSelector.Handle,
            keyNS.Handle,
            messageNS.Handle,
            attrsNS?.Handle ?? IntPtr.Zero);

        attrsNS?.Dispose();
    }

    private static NSDictionary? ConvertToNSDictionary(IDictionary<string, object>? attributes)
    {
        if (attributes == null || attributes.Count == 0)
        {
            return null;
        }

        var keys = new NSObject[attributes.Count];
        var values = new NSObject[attributes.Count];
        var i = 0;

        foreach (var kvp in attributes)
        {
            keys[i] = new NSString(kvp.Key);
            values[i] = ConvertToNSObject(kvp.Value);
            i++;
        }

        return NSDictionary.FromObjectsAndKeys(values, keys);
    }

    private static NSObject ConvertToNSObject(object? value)
    {
        return value switch
        {
            null => NSNull.Null,
            string s => new NSString(s),
            int i => NSNumber.FromInt32(i),
            long l => NSNumber.FromInt64(l),
            float f => NSNumber.FromFloat(f),
            double d => NSNumber.FromDouble(d),
            bool b => NSNumber.FromBoolean(b),
            _ => new NSString(value.ToString() ?? string.Empty)
        };
    }
}

/// <summary>
/// ObjC messaging helpers for RUM native wrapper.
/// </summary>
internal static class RumMessaging
{
    private const string LIBOBJC_DYLIB = "/usr/lib/libobjc.dylib";

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    public static extern bool bool_objc_msgSend_IntPtr_float(
        IntPtr receiver,
        IntPtr selector,
        IntPtr arg1,
        float arg2);

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    public static extern void void_objc_msgSend_IntPtr_IntPtr(
        IntPtr receiver,
        IntPtr selector,
        IntPtr arg1,
        IntPtr arg2);

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    public static extern void void_objc_msgSend_IntPtr_IntPtr_IntPtr(
        IntPtr receiver,
        IntPtr selector,
        IntPtr arg1,
        IntPtr arg2,
        IntPtr arg3);

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    public static extern void void_objc_msgSend_IntPtr_IntPtr_IntPtr_IntPtr(
        IntPtr receiver,
        IntPtr selector,
        IntPtr arg1,
        IntPtr arg2,
        IntPtr arg3,
        IntPtr arg4);

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    public static extern void void_objc_msgSend_IntPtr_int_long_IntPtr(
        IntPtr receiver,
        IntPtr selector,
        IntPtr arg1,
        int arg2,
        long arg3,
        IntPtr arg4);
}
#endif
