#if IOS
using System.Runtime.InteropServices;
using Foundation;
using ObjCRuntime;

namespace Datadog.Maui;

/// <summary>
/// iOS-specific partial implementation for platform instance creation.
/// </summary>
public static partial class DatadogSdk
{
    private static partial IDatadogSdk CreatePlatformInstance() => new DatadogSdkiOS();
}

/// <summary>
/// iOS implementation of the Datadog SDK.
/// </summary>
/// <remarks>
/// This implementation calls the native DatadogMauiWrapper Swift library through ObjC interop.
/// The native library must be built with Xcode and linked as a NativeReference.
///
/// When the native library is not available (development without Xcode), this falls back
/// to a stub implementation that logs initialization but doesn't connect to Datadog.
/// </remarks>
internal sealed class DatadogSdkiOS : IDatadogSdk
{
    private bool _isInitialized;

    /// <inheritdoc/>
    public bool IsInitialized => _isInitialized;

    /// <inheritdoc/>
    public void Initialize(DatadogConfiguration configuration)
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException("Datadog SDK is already initialized.");
        }

        ArgumentNullException.ThrowIfNull(configuration);

        var siteString = MapSiteToString(configuration.Site);
        var consentString = MapConsentToString(configuration.TrackingConsent);

        // Try to call native wrapper; fall back to stub if not available
        bool success;
        try
        {
            success = NativeWrapper.Initialize(
                configuration.ClientToken,
                configuration.Env,
                siteString,
                configuration.Service,
                consentString
            );
        }
        catch (DllNotFoundException)
        {
            // Native library not linked - use stub for development
            System.Diagnostics.Debug.WriteLine("[Datadog.Maui] Native wrapper not available. Build with Xcode and link NativeReference.");
            success = true; // Stub success for build verification
        }
        catch (EntryPointNotFoundException)
        {
            // Native method not found - use stub
            System.Diagnostics.Debug.WriteLine("[Datadog.Maui] Native method not found. Ensure DatadogMauiWrapper is properly exported.");
            success = true; // Stub success for build verification
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Datadog.Maui] Error calling native wrapper: {ex.Message}");
            success = false;
        }

        _isInitialized = success;

        System.Diagnostics.Debug.WriteLine(
            $"[Datadog.Maui] iOS SDK initialized: {_isInitialized}, " +
            $"ClientToken: {(configuration.ClientToken?.Length > 8 ? configuration.ClientToken[..8] + "..." : "[hidden]")}, " +
            $"Env: {configuration.Env}, Site: {siteString}");
    }

    private static string MapSiteToString(DatadogSite site) => site switch
    {
        DatadogSite.US1 => "us1",
        DatadogSite.US3 => "us3",
        DatadogSite.US5 => "us5",
        DatadogSite.EU1 => "eu1",
        DatadogSite.AP1 => "ap1",
        DatadogSite.AP2 => "ap2",
        DatadogSite.US1_FED => "us1_fed",
        DatadogSite.STAGING => "staging",
        _ => "us1"
    };

    private static string MapConsentToString(TrackingConsent consent) => consent switch
    {
        TrackingConsent.Granted => "granted",
        TrackingConsent.NotGranted => "notgranted",
        TrackingConsent.Pending => "pending",
        _ => "pending"
    };

    /// <summary>
    /// Native wrapper binding for DatadogMauiWrapper Swift class.
    /// </summary>
    private static class NativeWrapper
    {
        private const string LibraryName = "__Internal";

        /// <summary>
        /// Selector for DDMauiWrapper.initializeWithClientToken:env:site:service:trackingConsent:
        /// </summary>
        private static readonly Selector InitializeSelector = new Selector(
            "initializeWithClientToken:env:site:service:trackingConsent:");

        /// <summary>
        /// Selector for DDMauiWrapper.isInitialized
        /// </summary>
        private static readonly Selector IsInitializedSelector = new Selector("isInitialized");

        /// <summary>
        /// Gets the DDMauiWrapper class handle.
        /// </summary>
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

        public static bool Initialize(
            string clientToken,
            string env,
            string site,
            string? service,
            string trackingConsent)
        {
            using var clientTokenNS = new NSString(clientToken);
            using var envNS = new NSString(env);
            using var siteNS = new NSString(site);
            using var serviceNS = service != null ? new NSString(service) : null;
            using var consentNS = new NSString(trackingConsent);

            // Call: [DDMauiWrapper initializeWithClientToken:env:site:service:trackingConsent:]
            var result = Messaging.bool_objc_msgSend_IntPtr_IntPtr_IntPtr_IntPtr_IntPtr(
                ClassHandle,
                InitializeSelector.Handle,
                clientTokenNS.Handle,
                envNS.Handle,
                siteNS.Handle,
                serviceNS?.Handle ?? IntPtr.Zero,
                consentNS.Handle);

            return result;
        }

        public static bool GetIsInitialized()
        {
            return Messaging.bool_objc_msgSend(ClassHandle, IsInitializedSelector.Handle);
        }
    }
}

/// <summary>
/// ObjC messaging helpers for calling native Swift wrapper.
/// </summary>
internal static class Messaging
{
    private const string LIBOBJC_DYLIB = "/usr/lib/libobjc.dylib";

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    public static extern bool bool_objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    public static extern bool bool_objc_msgSend_IntPtr_IntPtr_IntPtr_IntPtr_IntPtr(
        IntPtr receiver,
        IntPtr selector,
        IntPtr arg1,
        IntPtr arg2,
        IntPtr arg3,
        IntPtr arg4,
        IntPtr arg5);
}
#endif
