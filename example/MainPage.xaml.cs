using DatadogSdk.Maui;
using DatadogSdk.Maui.Configuration;

namespace example;

public partial class MainPage : ContentPage
{
    private readonly Stack<(int id, string spanId)> _activeSpans = new();
    private int _nextSpanId;

    public MainPage()
    {
        InitializeComponent();

        // Enable Logs
        var logsConfiguration = new DdLogsConfiguration
        {
            //CustomEndpoint = "http://custom.endpoint"
        };
        DdLogs.Enable(logsConfiguration);

        var traceConfiguration = new DdTraceConfiguration
        {
            //CustomEndpoint = "http://custom.endpoint"
        };
        DdTrace.Enable(traceConfiguration);

        // Enable RUM
        var config = AppSettings.Load();
        var applicationId = config["Datadog"]!["ApplicationId"]!.ToString();
        var rumConfiguration = new DdRumConfiguration
        {
            ApplicationId = applicationId,
            SessionSampleRate = 100.0,
            TelemetrySampleRate = 100.0,
            ResourceTraceSampleRate = 100.0,
            TrackFrustrations = true,
            TrackBackgroundEvents = true,
            NativeViewTracking = true,
            NativeInteractionTracking = true,
            TrackMemoryWarnings = true,
            NativeLongTaskThresholdMs = 200.0,
            VitalsUpdateFrequency = VitalsUpdateFrequency.Average,
            FirstPartyHosts = new List<FirstPartyHost>
            {
                new() { Match = "datadoghq.com", HeaderTypes = new List<TracingHeaderType> { TracingHeaderType.Datadog, TracingHeaderType.TraceContext } }
            },
            ErrorEventMapper = e =>
            {
                e.Context["processedByErrorMapper"] = true;
                return e;
            }
        };

        DdRum.Enable(rumConfiguration);

        // Set global attributes
        DdSdk.AddAttribute("StringAttribute", "AttributeValue");
        DdSdk.AddAttribute("ArrayAttribute", new string[] { "AttributeValue", "AttributeValue" });
        DdSdk.AddAttribute("DictionaryAttribute", new Dictionary<string, object>
        {
            { "string", "test" },
            { "int", 123 },
            { "boolean", true },
            { "nested", new Dictionary<string, object>
                {
                    { "value", "test" }
                }
            }
        });

        DdSdk.AddAttribute("DeleteAttribute", "DeleteMe");
        DdSdk.RemoveAttribute("DeleteAttribute");
        DdSdk.AddAttributes(new Dictionary<string, object>
        {
            { "BatchAttribute1", "string" },
            { "BatchAttribute2", 123 },
            { "BatchDeleteMe", false }
        });
        DdSdk.RemoveAttributes(new List<string> { "BatchDeleteMe" });

        // Set user info
        DdSdk.SetUserInfo("UserId", "Username", "user@datadog.com",
            new Dictionary<string, object> { { "plan", "premium" } });

        DdSdk.AddUserExtraInfo(new Dictionary<string, object> { { "extra", 123 } });

        // Set account info
        DdSdk.SetAccountInfo("AccountId", "AccountName",
            new Dictionary<string, object> { { "type", "subscription" } });

        DdSdk.AddAccountExtraInfo(new Dictionary<string, object> { { "extra", "test" } });
    }

    private void OnSendLogsClicked(object? sender, EventArgs e)
    {
        var platform = DeviceInfo.Platform.ToString();
        DdLogs.Info($"DDLogs - {platform} - LogInfo");
        DdLogs.Debug($"DDLogs - {platform} - LogDebug");
        DdLogs.Warn($"DDLogs - {platform} - LogWarn");
        DdLogs.Error($"DDLogs - {platform} - LogError");
    }

    private async void OnSetTrackingConsentClicked(object? sender, EventArgs e)
    {
        string? choice = await DisplayActionSheetAsync(
            "Set Tracking Consent",
            "Cancel",
            null,
            "Granted",
            "Not Granted",
            "Pending");

        switch (choice)
        {
            case "Granted":
                DdSdk.SetTrackingConsent(TrackingConsent.Granted);
                break;
            case "Not Granted":
                DdSdk.SetTrackingConsent(TrackingConsent.NotGranted);
                break;
            case "Pending":
                DdSdk.SetTrackingConsent(TrackingConsent.Pending);
                break;
        }
    }

    private void OnStartTraceClicked(object? sender, EventArgs e)
    {
        var platform = DeviceInfo.Platform.ToString();
        var timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var id = ++_nextSpanId;
        var context = new Dictionary<string, object>
        {
            { "platform", platform },
            { "action", "manual_trace" },
            { "span_number", id.ToString() }
        };

        var spanId = DdTrace.StartSpan($"example.{platform}.manual_trace.{id}", context, timestampMs);
        _activeSpans.Push((id, spanId));
        UpdateTraceLabel();
    }

    private void OnStopTraceClicked(object? sender, EventArgs e)
    {
        if (_activeSpans.Count == 0) return;

        var (id, spanId) = _activeSpans.Pop();
        var timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var context = new Dictionary<string, object>
        {
            { "status", "completed" },
            { "span_number", id.ToString() }
        };

        DdTrace.FinishSpan(spanId, context, timestampMs);
        UpdateTraceLabel();
    }

    private void UpdateTraceLabel()
    {
        TraceStatusLabel.Text = _activeSpans.Count > 0
            ? $"Active spans: {string.Join(", ", _activeSpans.Select(s => $"#{s.id}"))}"
            : "No active spans";
    }
    private void OnManagedCrashClicked(object? sender, EventArgs e) =>
        throw new InvalidOperationException("C# crash example");

    private void OnNativeCrashClicked(object? sender, EventArgs e) =>
        NativeCrashHelper.TriggerNativeCrash();

    private void OnNdkCrashClicked(object? sender, EventArgs e)
    {
#if ANDROID
        NativeCrashHelper.TriggerNdkCrash();
#endif
    }
}
