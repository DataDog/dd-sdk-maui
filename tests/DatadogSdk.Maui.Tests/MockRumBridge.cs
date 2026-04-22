using DatadogSdk.Maui;

namespace DatadogSdk.Maui.Tests;

internal class MockRumBridge : DdRum.IRumBridge
{
    public int AddErrorCallCount { get; private set; }
    public string? LastMessage { get; private set; }
    public string? LastSource { get; private set; }
    public string? LastStacktrace { get; private set; }
    public Dictionary<string, object>? LastContext { get; private set; }
    public long LastTimestampMs { get; private set; }

    public void AddError(string message, string source, string stacktrace,
                         Dictionary<string, object> context, long timestampMs)
    {
        AddErrorCallCount++;
        LastMessage = message;
        LastSource = source;
        LastStacktrace = stacktrace;
        LastContext = context;
        LastTimestampMs = timestampMs;
    }

    public void Reset()
    {
        AddErrorCallCount = 0;
        LastMessage = null;
        LastSource = null;
        LastStacktrace = null;
        LastContext = null;
        LastTimestampMs = 0;
    }
}
