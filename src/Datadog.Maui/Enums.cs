namespace Datadog.Maui;

/// <summary>
/// Datadog data centers (sites) for data ingestion.
/// </summary>
public enum DatadogSite
{
    /// <summary>US1 data center (datadoghq.com)</summary>
    US1,
    /// <summary>US3 data center (us3.datadoghq.com)</summary>
    US3,
    /// <summary>US5 data center (us5.datadoghq.com)</summary>
    US5,
    /// <summary>EU1 data center (datadoghq.eu)</summary>
    EU1,
    /// <summary>AP1 data center (ap1.datadoghq.com)</summary>
    AP1,
    /// <summary>AP2 data center (ap2.datadoghq.com)</summary>
    AP2,
    /// <summary>US1-FED data center (ddog-gov.com)</summary>
    US1_FED,
    /// <summary>Staging data center (datad0g.com)</summary>
    STAGING
}

/// <summary>
/// User's consent for data tracking.
/// </summary>
public enum TrackingConsent
{
    /// <summary>Data tracking is allowed and data will be sent to Datadog.</summary>
    Granted,
    /// <summary>Data tracking is not allowed; data will be discarded.</summary>
    NotGranted,
    /// <summary>Data tracking consent is pending; data will be stored locally until consent is given or denied.</summary>
    Pending
}

/// <summary>
/// Controls the size of batches before upload.
/// </summary>
public enum BatchSize
{
    /// <summary>Smaller batches, uploaded more frequently.</summary>
    Small,
    /// <summary>Medium-sized batches (default).</summary>
    Medium,
    /// <summary>Larger batches, uploaded less frequently.</summary>
    Large
}

/// <summary>
/// Controls how frequently batches are uploaded.
/// </summary>
public enum UploadFrequency
{
    /// <summary>More frequent uploads (more battery usage).</summary>
    Frequent,
    /// <summary>Average upload frequency (default).</summary>
    Average,
    /// <summary>Less frequent uploads (less battery usage).</summary>
    Rare
}

/// <summary>
/// Type of user action tracked by RUM.
/// </summary>
public enum RumActionType
{
    /// <summary>A tap action on a UI element.</summary>
    Tap,
    /// <summary>A click action on a UI element.</summary>
    Click,
    /// <summary>A scroll action.</summary>
    Scroll,
    /// <summary>A swipe gesture.</summary>
    Swipe,
    /// <summary>A custom user action.</summary>
    Custom
}

/// <summary>
/// Source of an error tracked by RUM.
/// </summary>
public enum RumErrorSource
{
    /// <summary>Error originating from source code.</summary>
    Source,
    /// <summary>Error originating from network operations.</summary>
    Network,
    /// <summary>Error originating from a WebView.</summary>
    WebView,
    /// <summary>Error originating from console logs.</summary>
    Console,
    /// <summary>A custom error source.</summary>
    Custom
}
