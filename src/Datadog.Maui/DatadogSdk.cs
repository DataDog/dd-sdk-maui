namespace Datadog.Maui;

/// <summary>
/// Provides access to the Datadog SDK instance.
/// </summary>
public static partial class DatadogSdk
{
    private static readonly Lazy<IDatadogSdk> _instance = new(() => CreatePlatformInstance());

    /// <summary>
    /// Gets the singleton Datadog SDK instance for the current platform.
    /// </summary>
    public static IDatadogSdk Instance => _instance.Value;

    /// <summary>
    /// Creates the platform-specific SDK implementation.
    /// </summary>
    /// <returns>The platform-specific <see cref="IDatadogSdk"/> implementation.</returns>
    private static partial IDatadogSdk CreatePlatformInstance();
}
