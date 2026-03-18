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
    }

    private void OnSendLogsClicked(object? sender, EventArgs e)
    {
        var platform = DeviceInfo.Platform.ToString();
        DdLogs.Info($"DDLogs - {platform} - LogInfo");
        DdLogs.Debug($"DDLogs - {platform} - LogDebug");
        DdLogs.Warn($"DDLogs - {platform} - LogWarn");
        DdLogs.Error($"DDLogs - {platform} - LogError");
    }

    private void OnStartTraceClicked(object? sender, EventArgs e)
    {
        var platform = DeviceInfo.Platform.ToString();
        var timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var id = ++_nextSpanId;
        var context = new Dictionary<string, string>
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
        var context = new Dictionary<string, string>
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
}
