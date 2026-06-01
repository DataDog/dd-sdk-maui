/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using Datadog.Maui.Configuration;
using Xunit;

namespace Datadog.Maui.Tests;

[Collection("DdRum")]
public class DdRumAddErrorTests : IDisposable
{
    private readonly MockRumBridge rumBridge = new();

    public DdRumAddErrorTests()
    {
        DdRum.testBridge = rumBridge;
    }

    public void Dispose()
    {
        DdRum.testBridge = null;
        DdRum.errorEventMapper = null;
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void AddError_PassesMessageToNative()
    {
        DdRum.AddError("Test error", RumErrorSource.Source, "at Test.Method()");

        Assert.Equal(1, rumBridge.AddErrorCallCount);
        Assert.Equal("Test error", rumBridge.LastMessage);
    }

    [Fact]
    public void AddError_PassesSourceToNative()
    {
        DdRum.AddError("Error", RumErrorSource.Network, "stacktrace");

        Assert.Equal(RumErrorSource.Network, rumBridge.LastSource);
    }

    [Fact]
    public void AddError_PassesStacktraceToNative()
    {
        var stacktrace = "System.Exception: test\n   at Foo.Bar() in /src/Foo.cs:line 42";
        DdRum.AddError("Error", RumErrorSource.Source, stacktrace);

        Assert.Equal(stacktrace, rumBridge.LastStacktrace);
    }

    [Fact]
    public void AddError_WithContext_PassesContextToNative()
    {
        var context = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", 42 }
        };

        DdRum.AddError("Error", RumErrorSource.Source, "stacktrace", context);

        Assert.NotNull(rumBridge.LastContext);
        Assert.Equal("value1", rumBridge.LastContext["key1"]);
        Assert.Equal(42, rumBridge.LastContext["key2"]);
    }

    [Fact]
    public void AddError_WithoutContext_PassesEmptyDictionary()
    {
        DdRum.AddError("Error", RumErrorSource.Source, "stacktrace");

        Assert.NotNull(rumBridge.LastContext);
        Assert.Empty(rumBridge.LastContext);
    }

    [Fact]
    public void AddError_WithTimestamp_PassesTimestampToNative()
    {
        DdRum.AddError("Error", RumErrorSource.Source, "stacktrace", timestampMs: 1234567890L);

        Assert.Equal(1234567890L, rumBridge.LastTimestampMs);
    }

    [Fact]
    public void AddError_WithoutTimestamp_DefaultsToZero()
    {
        DdRum.AddError("Error", RumErrorSource.Source, "stacktrace");

        Assert.Equal(0L, rumBridge.LastTimestampMs);
    }

    [Fact]
    public void AddError_CalledMultipleTimes_IncrementsCallCount()
    {
        DdRum.AddError("Error 1", RumErrorSource.Source, "stack1");
        DdRum.AddError("Error 2", RumErrorSource.Network, "stack2");

        Assert.Equal(2, rumBridge.AddErrorCallCount);
        Assert.Equal("Error 2", rumBridge.LastMessage);
        Assert.Equal(RumErrorSource.Network, rumBridge.LastSource);
    }

    // ── Fingerprint ────────────────────────────────────────────

    [Fact]
    public void AddError_WithFingerprint_InjectsIntoContext()
    {
        DdRum.AddError("Error", RumErrorSource.Source, "stacktrace", fingerprint: "custom-group");

        Assert.NotNull(rumBridge.LastContext);
        Assert.Equal("custom-group", rumBridge.LastContext["_dd.error.fingerprint"]);
    }

    [Fact]
    public void AddError_WithEmptyFingerprint_DoesNotInjectIntoContext()
    {
        DdRum.AddError("Error", RumErrorSource.Source, "stacktrace", fingerprint: "");

        Assert.NotNull(rumBridge.LastContext);
        Assert.False(rumBridge.LastContext.ContainsKey("_dd.error.fingerprint"));
    }

    [Fact]
    public void AddError_WithNullFingerprint_DoesNotInjectIntoContext()
    {
        DdRum.AddError("Error", RumErrorSource.Source, "stacktrace", fingerprint: null);

        Assert.NotNull(rumBridge.LastContext);
        Assert.False(rumBridge.LastContext.ContainsKey("_dd.error.fingerprint"));
    }

    [Fact]
    public void AddError_WithFingerprintAndContext_MergesBoth()
    {
        var context = new Dictionary<string, object> { { "key", "value" } };

        DdRum.AddError("Error", RumErrorSource.Source, "stacktrace", context, fingerprint: "my-fingerprint");

        Assert.NotNull(rumBridge.LastContext);
        Assert.Equal("value", rumBridge.LastContext["key"]);
        Assert.Equal("my-fingerprint", rumBridge.LastContext["_dd.error.fingerprint"]);
    }

    // ── ErrorEventMapper ─────────────────────────────────────

    [Fact]
    public void AddError_WithMapper_AppliesModifications()
    {
        DdRum.errorEventMapper = e =>
        {
            e.Message = "Modified: " + e.Message;
            return e;
        };

        DdRum.AddError("Original error", RumErrorSource.Source, "stacktrace");

        Assert.Equal("Modified: Original error", rumBridge.LastMessage);
    }

    [Fact]
    public void AddError_WithMapper_ReturningNull_DropsError()
    {
        DdRum.errorEventMapper = _ => null;

        DdRum.AddError("Should be dropped", RumErrorSource.Source, "stacktrace");

        Assert.Equal(0, rumBridge.AddErrorCallCount);
    }

    [Fact]
    public void AddError_WithMapper_CanModifyAllFields()
    {
        DdRum.errorEventMapper = e =>
        {
            e.Message = "new message";
            e.Source = RumErrorSource.Network;
            e.Stacktrace = "new stack";
            e.Context["custom"] = "value";
            e.TimestampMs = 999L;
            return e;
        };

        DdRum.AddError("old", RumErrorSource.Source, "old stack", timestampMs: 0);

        Assert.Equal("new message", rumBridge.LastMessage);
        Assert.Equal(RumErrorSource.Network, rumBridge.LastSource);
        Assert.Equal("new stack", rumBridge.LastStacktrace);
        Assert.Equal("value", rumBridge.LastContext!["custom"]);
        Assert.Equal(999L, rumBridge.LastTimestampMs);
    }

    [Fact]
    public void AddError_WithoutMapper_PassesThroughUnchanged()
    {
        DdRum.errorEventMapper = null;

        DdRum.AddError("Original", RumErrorSource.Source, "stack");

        Assert.Equal(1, rumBridge.AddErrorCallCount);
        Assert.Equal("Original", rumBridge.LastMessage);
    }
}
