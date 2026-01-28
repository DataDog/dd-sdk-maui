namespace Datadog.Maui;

/// <summary>
/// Interface for Real User Monitoring (RUM) operations.
/// </summary>
public interface IRum
{
    /// <summary>
    /// Starts tracking a view with the specified key and optional name.
    /// </summary>
    /// <param name="key">A unique identifier for the view.</param>
    /// <param name="name">An optional human-readable name for the view. If null, the key is used.</param>
    /// <param name="attributes">Optional custom attributes to attach to the view.</param>
    void StartView(string key, string? name = null, IDictionary<string, object>? attributes = null);

    /// <summary>
    /// Stops tracking a view with the specified key.
    /// </summary>
    /// <param name="key">The unique identifier of the view to stop.</param>
    /// <param name="attributes">Optional custom attributes to attach when stopping the view.</param>
    void StopView(string key, IDictionary<string, object>? attributes = null);

    /// <summary>
    /// Adds a user action event.
    /// </summary>
    /// <param name="type">The type of action performed.</param>
    /// <param name="name">A human-readable name for the action.</param>
    /// <param name="attributes">Optional custom attributes to attach to the action.</param>
    void AddAction(RumActionType type, string name, IDictionary<string, object>? attributes = null);

    /// <summary>
    /// Adds an error event.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="source">The source of the error.</param>
    /// <param name="stackTrace">Optional stack trace associated with the error.</param>
    /// <param name="attributes">Optional custom attributes to attach to the error.</param>
    void AddError(string message, RumErrorSource source, string? stackTrace = null, IDictionary<string, object>? attributes = null);

    /// <summary>
    /// Starts tracking a resource request.
    /// </summary>
    /// <param name="key">A unique identifier for the resource.</param>
    /// <param name="httpMethod">The HTTP method used (e.g., GET, POST).</param>
    /// <param name="url">The URL of the resource.</param>
    /// <param name="attributes">Optional custom attributes to attach to the resource.</param>
    void StartResource(string key, string httpMethod, string url, IDictionary<string, object>? attributes = null);

    /// <summary>
    /// Stops tracking a resource request that completed successfully.
    /// </summary>
    /// <param name="key">The unique identifier of the resource.</param>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    /// <param name="size">Optional size of the resource in bytes.</param>
    /// <param name="attributes">Optional custom attributes to attach when stopping the resource.</param>
    void StopResource(string key, int statusCode, long? size = null, IDictionary<string, object>? attributes = null);

    /// <summary>
    /// Stops tracking a resource request that failed with an error.
    /// </summary>
    /// <param name="key">The unique identifier of the resource.</param>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="attributes">Optional custom attributes to attach when stopping the resource.</param>
    void StopResourceWithError(string key, string message, IDictionary<string, object>? attributes = null);
}
