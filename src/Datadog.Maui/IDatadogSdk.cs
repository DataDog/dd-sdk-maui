namespace Datadog.Maui;

/// <summary>
/// Interface for the Datadog SDK providing core functionality.
/// </summary>
public interface IDatadogSdk
{
    /// <summary>
    /// Initializes the Datadog SDK with the specified configuration.
    /// </summary>
    /// <param name="configuration">The configuration options for the SDK.</param>
    /// <exception cref="InvalidOperationException">Thrown if the SDK is already initialized.</exception>
    void Initialize(DatadogConfiguration configuration);

    /// <summary>
    /// Gets a value indicating whether the SDK has been initialized.
    /// </summary>
    bool IsInitialized { get; }
}
