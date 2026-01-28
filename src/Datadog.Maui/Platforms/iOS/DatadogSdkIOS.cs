#if IOS
namespace Datadog.Maui;

/// <summary>
/// iOS-specific partial implementation for platform instance creation.
/// </summary>
public static partial class DatadogSdk
{
    private static partial IDatadogSdk CreatePlatformInstance() => new DatadogSdkIOS();
}

/// <summary>
/// iOS implementation of the Datadog SDK.
/// </summary>
/// <remarks>
/// This is a stub implementation. Full iOS support will be added in Plan 02.
/// </remarks>
internal sealed class DatadogSdkIOS : IDatadogSdk
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

        // iOS native binding will be implemented in Plan 02
        // For now, just mark as initialized for build verification
        _isInitialized = true;
    }
}
#endif
