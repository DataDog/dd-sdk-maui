namespace Datadog.Maui;

/// <summary>
/// Configuration options for initializing the Datadog SDK.
/// </summary>
/// <remarks>
/// At minimum, you must provide <see cref="ClientToken"/> and <see cref="Env"/>.
/// </remarks>
public record DatadogConfiguration
{
    /// <summary>
    /// The client token for your Datadog application.
    /// </summary>
    /// <remarks>
    /// You can find your client token in the Datadog console under
    /// Organization Settings > Client Tokens.
    /// </remarks>
    public required string ClientToken { get; init; }

    /// <summary>
    /// The environment name (e.g., "production", "staging", "development").
    /// </summary>
    /// <remarks>
    /// This is used to filter and group data in the Datadog console.
    /// </remarks>
    public required string Env { get; init; }

    /// <summary>
    /// The Datadog site to send data to. Defaults to <see cref="DatadogSite.US1"/>.
    /// </summary>
    public DatadogSite Site { get; init; } = DatadogSite.US1;

    /// <summary>
    /// The service name for your application. If not specified, the application package name is used.
    /// </summary>
    public string? Service { get; init; }

    /// <summary>
    /// The RUM application ID. Required for RUM functionality.
    /// </summary>
    /// <remarks>
    /// You can find your RUM application ID in the Datadog console under
    /// UX Monitoring > RUM Applications.
    /// </remarks>
    public string? RumApplicationId { get; init; }

    /// <summary>
    /// The initial tracking consent status. Defaults to <see cref="TrackingConsent.Pending"/>.
    /// </summary>
    /// <remarks>
    /// If set to <see cref="TrackingConsent.Pending"/>, data will be stored locally
    /// until consent is explicitly granted or denied.
    /// </remarks>
    public TrackingConsent TrackingConsent { get; init; } = TrackingConsent.Pending;

    /// <summary>
    /// The batch size for data uploads. Defaults to <see cref="BatchSize.Medium"/>.
    /// </summary>
    public BatchSize BatchSize { get; init; } = BatchSize.Medium;

    /// <summary>
    /// The upload frequency for data batches. Defaults to <see cref="UploadFrequency.Average"/>.
    /// </summary>
    public UploadFrequency UploadFrequency { get; init; } = UploadFrequency.Average;

    /// <summary>
    /// Enables native crash reporting. Defaults to true.
    /// </summary>
    /// <remarks>
    /// When enabled, the SDK will capture native crashes (iOS/Android)
    /// and report them to Datadog Error Tracking.
    /// </remarks>
    public bool EnableCrashReporting { get; init; } = true;

    /// <summary>
    /// Enables tracking of application hangs/ANRs. Defaults to true.
    /// </summary>
    /// <remarks>
    /// On iOS, this tracks app hangs when the main thread is blocked.
    /// On Android, this tracks ANR (Application Not Responding) events.
    /// </remarks>
    public bool TrackAppHangs { get; init; } = true;

    /// <summary>
    /// Enables catching unhandled .NET exceptions and reporting them to RUM. Defaults to true.
    /// </summary>
    /// <remarks>
    /// When enabled, unhandled exceptions from AppDomain.CurrentDomain.UnhandledException
    /// and TaskScheduler.UnobservedTaskException will be captured and reported
    /// as RUM errors.
    /// </remarks>
    public bool CatchUnhandledExceptions { get; init; } = true;
}
