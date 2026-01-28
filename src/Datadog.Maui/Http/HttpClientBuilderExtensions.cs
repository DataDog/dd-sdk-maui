using Microsoft.Extensions.DependencyInjection;

namespace Datadog.Maui.Http;

/// <summary>
/// Extension methods for adding Datadog HTTP tracking to <see cref="IHttpClientBuilder"/>.
/// </summary>
public static class HttpClientBuilderExtensions
{
    /// <summary>
    /// Adds Datadog HTTP tracking to the configured <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/> to configure.</param>
    /// <returns>The <see cref="IHttpClientBuilder"/> for method chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddHttpClient&lt;IMyService, MyService&gt;()
    ///     .AddDatadogTracking();
    /// </code>
    /// </example>
    public static IHttpClientBuilder AddDatadogTracking(this IHttpClientBuilder builder)
    {
        return builder.AddDatadogTracking(new DatadogHttpTrackingOptions());
    }

    /// <summary>
    /// Adds Datadog HTTP tracking to the configured <see cref="HttpClient"/> with custom options.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/> to configure.</param>
    /// <param name="options">The tracking options to use.</param>
    /// <returns>The <see cref="IHttpClientBuilder"/> for method chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddHttpClient&lt;IMyService, MyService&gt;()
    ///     .AddDatadogTracking(new DatadogHttpTrackingOptions
    ///     {
    ///         ExcludedHosts = new[] { "internal-api.example.com" },
    ///         TrackResponseHeaders = true
    ///     });
    /// </code>
    /// </example>
    public static IHttpClientBuilder AddDatadogTracking(
        this IHttpClientBuilder builder,
        DatadogHttpTrackingOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        return builder.AddHttpMessageHandler(() => new DatadogHttpMessageHandler(options));
    }

    /// <summary>
    /// Adds Datadog HTTP tracking to the configured <see cref="HttpClient"/> with an options configuration delegate.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/> to configure.</param>
    /// <param name="configureOptions">A delegate to configure the tracking options.</param>
    /// <returns>The <see cref="IHttpClientBuilder"/> for method chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddHttpClient&lt;IMyService, MyService&gt;()
    ///     .AddDatadogTracking(options =>
    ///     {
    ///         options.ExcludedHosts = new[] { "internal-api.example.com" };
    ///         options.TrackResponseHeaders = true;
    ///     });
    /// </code>
    /// </example>
    public static IHttpClientBuilder AddDatadogTracking(
        this IHttpClientBuilder builder,
        Action<DatadogHttpTrackingOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var options = new DatadogHttpTrackingOptions();
        configureOptions(options);

        return builder.AddDatadogTracking(options);
    }
}
