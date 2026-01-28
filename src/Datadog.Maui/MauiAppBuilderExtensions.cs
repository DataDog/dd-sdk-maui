using Microsoft.Maui.LifecycleEvents;

namespace Datadog.Maui;

/// <summary>
/// Extension methods for configuring Datadog in a MAUI application.
/// </summary>
public static class MauiAppBuilderExtensions
{
    /// <summary>
    /// Configures the Datadog SDK for the MAUI application.
    /// </summary>
    /// <param name="builder">The MAUI app builder.</param>
    /// <param name="configuration">The Datadog configuration options.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <remarks>
    /// This method sets up Datadog to initialize at the appropriate time
    /// in the application lifecycle for each platform.
    /// </remarks>
    /// <example>
    /// <code>
    /// var builder = MauiApp.CreateBuilder();
    /// builder
    ///     .UseMauiApp&lt;App&gt;()
    ///     .UseDatadog(new DatadogConfiguration
    ///     {
    ///         ClientToken = "your_client_token",
    ///         Env = "production",
    ///         Site = DatadogSite.US1
    ///     });
    /// </code>
    /// </example>
    public static MauiAppBuilder UseDatadog(this MauiAppBuilder builder, DatadogConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android
                .OnCreate((activity, bundle) =>
                {
                    if (!DatadogSdk.Instance.IsInitialized)
                    {
                        DatadogSdk.Instance.Initialize(configuration);
                    }
                }));
#elif IOS
            events.AddiOS(ios => ios
                .FinishedLaunching((app, options) =>
                {
                    if (!DatadogSdk.Instance.IsInitialized)
                    {
                        DatadogSdk.Instance.Initialize(configuration);
                    }
                    return true;
                }));
#endif
        });

        return builder;
    }
}
