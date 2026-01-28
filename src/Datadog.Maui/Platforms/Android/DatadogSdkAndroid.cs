#if ANDROID
namespace Datadog.Maui;

/// <summary>
/// Android-specific partial implementation for platform instance creation.
/// </summary>
public static partial class DatadogSdk
{
    private static partial IDatadogSdk CreatePlatformInstance() => new DatadogSdkAndroid();
}

/// <summary>
/// Android implementation of the Datadog SDK.
/// </summary>
internal sealed class DatadogSdkAndroid : IDatadogSdk
{
    /// <inheritdoc/>
    public bool IsInitialized => Com.Datadog.Maui.DatadogMauiWrapper.IsInitialized;

    /// <inheritdoc/>
    public void Initialize(DatadogConfiguration configuration)
    {
        if (IsInitialized)
        {
            throw new InvalidOperationException("Datadog SDK is already initialized.");
        }

        var context = Android.App.Application.Context;
        var siteString = MapSite(configuration.Site);
        Android.Util.Log.Info("Datadog.Maui", $"Site enum value: {(int)configuration.Site}, name: {configuration.Site}, mapped to: {siteString}");

        var success = Com.Datadog.Maui.DatadogMauiWrapper.Initialize(
            context,
            configuration.ClientToken,
            configuration.Env,
            siteString,
            configuration.Service,
            MapTrackingConsent(configuration.TrackingConsent),
            MapBatchSize(configuration.BatchSize),
            MapUploadFrequency(configuration.UploadFrequency)
        );

        if (!success)
        {
            throw new InvalidOperationException("Failed to initialize Datadog SDK.");
        }
    }

    private static string MapSite(DatadogSite site) => site switch
    {
        DatadogSite.US1 => "US1",
        DatadogSite.US3 => "US3",
        DatadogSite.US5 => "US5",
        DatadogSite.EU1 => "EU1",
        DatadogSite.AP1 => "AP1",
        DatadogSite.AP2 => "AP2",
        DatadogSite.US1_FED => "US1_FED",
        DatadogSite.STAGING => "STAGING",
        _ => "US1"
    };

    private static string MapTrackingConsent(TrackingConsent consent) => consent switch
    {
        TrackingConsent.Granted => "GRANTED",
        TrackingConsent.NotGranted => "NOT_GRANTED",
        TrackingConsent.Pending => "PENDING",
        _ => "PENDING"
    };

    private static string MapBatchSize(BatchSize size) => size switch
    {
        BatchSize.Small => "SMALL",
        BatchSize.Medium => "MEDIUM",
        BatchSize.Large => "LARGE",
        _ => "MEDIUM"
    };

    private static string MapUploadFrequency(UploadFrequency frequency) => frequency switch
    {
        UploadFrequency.Frequent => "FREQUENT",
        UploadFrequency.Average => "AVERAGE",
        UploadFrequency.Rare => "RARE",
        _ => "AVERAGE"
    };
}
#endif
