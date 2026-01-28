namespace Datadog.Maui;

/// <summary>
/// Provides access to the Datadog RUM (Real User Monitoring) instance.
/// </summary>
public static partial class Rum
{
    private static readonly Lazy<IRum> _instance = new(() => CreatePlatformInstance());

    /// <summary>
    /// Gets the singleton RUM instance for the current platform.
    /// </summary>
    public static IRum Instance => _instance.Value;

    /// <summary>
    /// Enables RUM with the specified application ID.
    /// Must be called after the Datadog SDK is initialized.
    /// </summary>
    /// <param name="applicationId">The RUM application ID from Datadog.</param>
    /// <param name="sampleRate">The sample rate for RUM sessions (0.0 to 100.0). Default is 100.0.</param>
    /// <returns>True if RUM was enabled successfully, false otherwise.</returns>
    public static partial bool Enable(string applicationId, float sampleRate = 100.0f);

    /// <summary>
    /// Creates the platform-specific RUM implementation.
    /// </summary>
    /// <returns>The platform-specific <see cref="IRum"/> implementation.</returns>
    private static partial IRum CreatePlatformInstance();
}
