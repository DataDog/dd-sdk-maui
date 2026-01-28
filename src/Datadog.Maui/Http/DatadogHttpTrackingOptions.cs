namespace Datadog.Maui.Http;

/// <summary>
/// Configuration options for Datadog HTTP tracking.
/// </summary>
public class DatadogHttpTrackingOptions
{
    /// <summary>
    /// Gets or sets a list of hosts to exclude from tracking.
    /// </summary>
    /// <remarks>
    /// Requests to these hosts will not be reported to RUM.
    /// Datadog endpoints (datadoghq.com, datad0g.com) are always excluded.
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = new DatadogHttpTrackingOptions
    /// {
    ///     ExcludedHosts = new[] { "internal-api.example.com", "localhost" }
    /// };
    /// </code>
    /// </example>
    public IEnumerable<string>? ExcludedHosts { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to track request headers.
    /// </summary>
    /// <remarks>
    /// When enabled, select request headers will be added as RUM attributes.
    /// Default is false.
    /// </remarks>
    public bool TrackRequestHeaders { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to track response headers.
    /// </summary>
    /// <remarks>
    /// When enabled, select response headers (like Content-Type) will be added as RUM attributes.
    /// Default is false.
    /// </remarks>
    public bool TrackResponseHeaders { get; set; } = false;
}
