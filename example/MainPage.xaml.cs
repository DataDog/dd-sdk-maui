/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

using DatadogSdk.Maui;
using DatadogSdk.Maui.Configuration;

namespace example;

public partial class MainPage : ContentPage
{
    private readonly Stack<(int id, string spanId)> _activeSpans = new();
    private int _nextSpanId;
    private const string OperationName = "checkout";
    private const string OperationKey = "example-op-1";

    public MainPage()
    {
        InitializeComponent();
        RuntimeLabel.Text = $".NET {Environment.Version.ToString(2)} · {DeviceInfo.Platform}";

        // Global attributes
        DdSdk.AddAttribute("StringAttribute", "AttributeValue");
        DdSdk.AddAttribute("ArrayAttribute", new string[] { "AttributeValue", "AttributeValue" });
        DdSdk.AddAttribute("DictionaryAttribute", new Dictionary<string, object>
        {
            { "string", "test" },
            { "int", 123 },
            { "boolean", true },
            { "nested", new Dictionary<string, object> { { "value", "test" } } },
        });

        DdSdk.AddAttribute("DeleteAttribute", "DeleteMe");
        DdSdk.RemoveAttribute("DeleteAttribute");
        DdSdk.AddAttributes(new Dictionary<string, object>
        {
            { "BatchAttribute1", "string" },
            { "BatchAttribute2", 123 },
            { "BatchDeleteMe", false },
        });
        DdSdk.RemoveAttributes(new List<string> { "BatchDeleteMe" });

        // User info
        DdSdk.SetUserInfo("UserId", "Username", "user@datadog.com",
            new Dictionary<string, object> { { "plan", "premium" } });
        DdSdk.AddUserExtraInfo(new Dictionary<string, object> { { "extra", 123 } });

        // Account info
        DdSdk.SetAccountInfo("AccountId", "AccountName",
            new Dictionary<string, object> { { "type", "subscription" } });
        DdSdk.AddAccountExtraInfo(new Dictionary<string, object> { { "extra", "test" } });

        // View loading time
        DdRum.AddViewLoadingTime(true);
        DdRum.AddTiming("CustomTiming");
    }

    private void OnSendLogsClicked(object? sender, EventArgs e)
    {
        var platform = DeviceInfo.Platform.ToString();
        DdLogs.Info($"DDLogs - {platform} - LogInfo");
        DdLogs.Debug($"DDLogs - {platform} - LogDebug");
        DdLogs.Warn($"DDLogs - {platform} - LogWarn");
        DdLogs.Error($"DDLogs - {platform} - LogError");
    }

    private void OnAddActionClicked(object? sender, EventArgs e)
    {
        DdRum.AddAction(RumActionType.Custom, "CustomAction");
    }

    private void OnTrackResourceClicked(object? sender, EventArgs e)
    {
        var resourceKey = $"api-call-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        DdRum.StartResource(resourceKey, RumResourceMethod.Get, "https://api.example.com/data");
        DdRum.StopResource(resourceKey, 200, RumResourceKind.Xhr, 1024);
    }

    private async void OnSendHttpRequestsClicked(object? sender, EventArgs e)
    {
        using var client = new HttpClient();

        try
        {
            // JSON API — should be tracked as "xhr"
            await client.GetAsync("https://jsonplaceholder.typicode.com/posts/1");

            // Another JSON API
            await client.GetAsync("https://jsonplaceholder.typicode.com/users/1");

            // Image
            await client.GetAsync("https://picsum.photos/200");

            // POST request
            var content = new StringContent("{\"title\":\"test\",\"body\":\"hello\",\"userId\":1}", System.Text.Encoding.UTF8, "application/json");
            await client.PostAsync("https://jsonplaceholder.typicode.com/posts", content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"HTTP request failed: {ex.Message}");
        }
    }

    private async void OnSetTrackingConsentClicked(object? sender, EventArgs e)
    {
#if NET10_0_OR_GREATER
        string? choice = await DisplayActionSheetAsync(
#else
        string? choice = await DisplayActionSheet(
#endif
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
    private void OnStartOperationClicked(object? sender, EventArgs e)
    {
        DdRum.StartOperation(OperationName, OperationKey,
            new Dictionary<string, object> { { "source", "example_app" } });
        OperationStatusLabel.Text = $"Operation '{OperationName}' started (key: {OperationKey})";
    }

    private void OnSucceedOperationClicked(object? sender, EventArgs e)
    {
        DdRum.SucceedOperation(OperationName, OperationKey,
            new Dictionary<string, object> { { "result", "success" } });
        OperationStatusLabel.Text = $"Operation '{OperationName}' succeeded";
    }

    private void OnFailOperationClicked(object? sender, EventArgs e)
    {
        DdRum.FailOperation(OperationName, OperationFailure.Error, OperationKey,
            new Dictionary<string, object> { { "error_code", 500 } });
        OperationStatusLabel.Text = $"Operation '{OperationName}' failed (reason: Error)";
    }

    private async void OnNavigateClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("DetailPage");
    }

    private async void OnNavNavigateClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new NavDetailPage(), true);
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
