#if ANDROID
namespace Datadog.Maui;

/// <summary>
/// Android-specific partial implementation for platform instance creation.
/// </summary>
public static partial class DatadogSdk
{
    private static partial IDatadogSdk CreatePlatformInstance() => new DatadogSdkAndroid();
}

/// <summary>
/// Android implementation of the Datadog SDK.
/// </summary>
internal sealed class DatadogSdkAndroid : IDatadogSdk
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

        // Native wrapper call will be implemented in Task 2
        // For now, just mark as initialized for build verification
        _isInitialized = true;
    }
}
#endif
