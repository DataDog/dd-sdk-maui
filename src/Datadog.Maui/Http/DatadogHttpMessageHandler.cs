using System.Diagnostics;

namespace Datadog.Maui.Http;

/// <summary>
/// A <see cref="DelegatingHandler"/> that automatically instruments HTTP requests for Datadog RUM.
/// </summary>
/// <remarks>
/// This handler wraps outgoing HTTP requests and reports them as RUM resources,
/// including request timing, status codes, and response sizes.
///
/// Requests to Datadog endpoints (datadoghq.com, datad0g.com) are automatically
/// excluded from tracking to prevent infinite loops.
/// </remarks>
/// <example>
/// <code>
/// // Option 1: Use with IHttpClientFactory
/// services.AddHttpClient&lt;IMyService, MyService&gt;()
///     .AddDatadogTracking();
///
/// // Option 2: Use directly with HttpClient
/// var handler = new DatadogHttpMessageHandler();
/// var client = new HttpClient(handler);
/// </code>
/// </example>
public class DatadogHttpMessageHandler : DelegatingHandler
{
    private readonly DatadogHttpTrackingOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatadogHttpMessageHandler"/> class.
    /// </summary>
    public DatadogHttpMessageHandler()
        : this(new DatadogHttpTrackingOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatadogHttpMessageHandler"/> class
    /// with the specified options.
    /// </summary>
    /// <param name="options">The tracking options to use.</param>
    public DatadogHttpMessageHandler(DatadogHttpTrackingOptions options)
        : base(new HttpClientHandler())
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatadogHttpMessageHandler"/> class
    /// with the specified inner handler.
    /// </summary>
    /// <param name="innerHandler">The inner handler to wrap.</param>
    public DatadogHttpMessageHandler(HttpMessageHandler innerHandler)
        : this(new DatadogHttpTrackingOptions(), innerHandler)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatadogHttpMessageHandler"/> class
    /// with the specified options and inner handler.
    /// </summary>
    /// <param name="options">The tracking options to use.</param>
    /// <param name="innerHandler">The inner handler to wrap.</param>
    public DatadogHttpMessageHandler(DatadogHttpTrackingOptions options, HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var url = request.RequestUri?.ToString() ?? "unknown";

        // Skip Datadog endpoints to prevent infinite loops
        if (ShouldSkipUrl(url))
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        // Generate a unique resource key for this request
        var resourceKey = $"{request.Method}_{Guid.NewGuid():N}";
        var httpMethod = request.Method.Method;

        // Start tracking the resource
        var attributes = BuildRequestAttributes(request);
        Rum.Instance.StartResource(resourceKey, httpMethod, url, attributes);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            // Get response size if available
            var size = response.Content.Headers.ContentLength;

            // Build response attributes
            var responseAttributes = BuildResponseAttributes(response, stopwatch.ElapsedMilliseconds);

            // Stop tracking with success
            Rum.Instance.StopResource(
                resourceKey,
                (int)response.StatusCode,
                size,
                responseAttributes);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Build error attributes
            var errorAttributes = new Dictionary<string, object>
            {
                ["error.kind"] = ex.GetType().Name,
                ["duration_ms"] = stopwatch.ElapsedMilliseconds
            };

            // Stop tracking with error
            Rum.Instance.StopResourceWithError(resourceKey, ex.Message, errorAttributes);

            throw;
        }
    }

    private bool ShouldSkipUrl(string url)
    {
        // Always skip Datadog endpoints
        if (url.Contains("datadoghq.com", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("datad0g.com", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check user-defined exclusions
        if (_options.ExcludedHosts != null)
        {
            foreach (var host in _options.ExcludedHosts)
            {
                if (url.Contains(host, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private Dictionary<string, object>? BuildRequestAttributes(HttpRequestMessage request)
    {
        if (!_options.TrackRequestHeaders)
        {
            return null;
        }

        var attributes = new Dictionary<string, object>();

        // Add select request headers as attributes
        if (request.Headers.TryGetValues("Content-Type", out var contentTypes))
        {
            attributes["http.request.content_type"] = string.Join(", ", contentTypes);
        }

        return attributes.Count > 0 ? attributes : null;
    }

    private Dictionary<string, object> BuildResponseAttributes(
        HttpResponseMessage response,
        long durationMs)
    {
        var attributes = new Dictionary<string, object>
        {
            ["duration_ms"] = durationMs
        };

        if (_options.TrackResponseHeaders)
        {
            if (response.Content.Headers.ContentType != null)
            {
                attributes["http.response.content_type"] = response.Content.Headers.ContentType.ToString();
            }
        }

        return attributes;
    }
}
