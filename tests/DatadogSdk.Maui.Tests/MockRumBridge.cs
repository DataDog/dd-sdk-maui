/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using DatadogSdk.Maui;
using DatadogSdk.Maui.Configuration;

namespace DatadogSdk.Maui.Tests;

internal class MockRumBridge : DdRum.IRumBridge
{
    // AddError
    public int AddErrorCallCount { get; private set; }
    public string? LastMessage { get; private set; }
    public RumErrorSource? LastSource { get; private set; }
    public string? LastStacktrace { get; private set; }
    public Dictionary<string, object>? LastContext { get; private set; }
    public long LastTimestampMs { get; private set; }

    // Views
    public List<(string Key, string Name, Dictionary<string, object> Context, long TimestampMs)> StartViewCalls { get; } = new();
    public List<(string Key, Dictionary<string, object> Context, long TimestampMs)> StopViewCalls { get; } = new();

    // Actions
    public List<(RumActionType Type, string Name, Dictionary<string, object> Context, long TimestampMs)> StartActionCalls { get; } = new();
    public List<(RumActionType Type, string Name, Dictionary<string, object> Context, long TimestampMs)> StopActionCalls { get; } = new();
    public List<(RumActionType Type, string Name, Dictionary<string, object> Context, long TimestampMs)> AddActionCalls { get; } = new();

    // Resources
    public List<(string Key, RumResourceMethod Method, string Url, Dictionary<string, object> Context, long TimestampMs)> StartResourceCalls { get; } = new();
    public List<(string Key, int StatusCode, RumResourceKind Kind, long Size, Dictionary<string, object> Context, long TimestampMs)> StopResourceCalls { get; } = new();

    // Timing
    public List<string> AddTimingCalls { get; } = new();
    public List<bool> AddViewLoadingTimeCalls { get; } = new();

    // Session
    public int StopSessionCallCount { get; private set; }

    // View Attributes
    public List<(string Key, object Value)> AddViewAttributeCalls { get; } = new();
    public List<string> RemoveViewAttributeCalls { get; } = new();
    public List<Dictionary<string, object>> AddViewAttributesCalls { get; } = new();
    public List<List<string>> RemoveViewAttributesCalls { get; } = new();

    // Feature Operations
    public List<(string Name, string? OperationKey, Dictionary<string, object> Attributes)> StartOperationCalls { get; } = new();
    public List<(string Name, string? OperationKey, Dictionary<string, object> Attributes)> SucceedOperationCalls { get; } = new();
    public List<(string Name, string? OperationKey, string Reason, Dictionary<string, object> Attributes)> FailOperationCalls { get; } = new();

    public void AddError(string message, RumErrorSource source, string stacktrace,
                         Dictionary<string, object> context, long timestampMs)
    {
        AddErrorCallCount++;
        LastMessage = message;
        LastSource = source;
        LastStacktrace = stacktrace;
        LastContext = context;
        LastTimestampMs = timestampMs;
    }

    public void StartView(string key, string name, Dictionary<string, object> context, long timestampMs)
        => StartViewCalls.Add((key, name, context, timestampMs));
    public void StopView(string key, Dictionary<string, object> context, long timestampMs)
        => StopViewCalls.Add((key, context, timestampMs));
    public void StartAction(RumActionType type, string name, Dictionary<string, object> context, long timestampMs)
        => StartActionCalls.Add((type, name, context, timestampMs));
    public void StopAction(RumActionType type, string name, Dictionary<string, object> context, long timestampMs)
        => StopActionCalls.Add((type, name, context, timestampMs));
    public void AddAction(RumActionType type, string name, Dictionary<string, object> context, long timestampMs)
        => AddActionCalls.Add((type, name, context, timestampMs));
    public void StartResource(string key, RumResourceMethod method, string url, Dictionary<string, object> context, long timestampMs)
        => StartResourceCalls.Add((key, method, url, context, timestampMs));
    public void StopResource(string key, int statusCode, RumResourceKind kind, long size, Dictionary<string, object> context, long timestampMs)
        => StopResourceCalls.Add((key, statusCode, kind, size, context, timestampMs));
    public void AddTiming(string name)
        => AddTimingCalls.Add(name);
    public void AddViewLoadingTime(bool overwrite)
        => AddViewLoadingTimeCalls.Add(overwrite);
    public void StopSession()
        => StopSessionCallCount++;
    public void AddViewAttribute(string key, object value)
        => AddViewAttributeCalls.Add((key, value));
    public void RemoveViewAttribute(string key)
        => RemoveViewAttributeCalls.Add(key);
    public void AddViewAttributes(Dictionary<string, object> attributes)
        => AddViewAttributesCalls.Add(attributes);
    public void RemoveViewAttributes(List<string> keys)
        => RemoveViewAttributesCalls.Add(keys);
    public void StartOperation(string name, string? operationKey, Dictionary<string, object> attributes)
        => StartOperationCalls.Add((name, operationKey, attributes));
    public void SucceedOperation(string name, string? operationKey, Dictionary<string, object> attributes)
        => SucceedOperationCalls.Add((name, operationKey, attributes));
    public void FailOperation(string name, string? operationKey, string reason, Dictionary<string, object> attributes)
        => FailOperationCalls.Add((name, operationKey, reason, attributes));

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
