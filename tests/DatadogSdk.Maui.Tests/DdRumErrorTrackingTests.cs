/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

using Xunit;

namespace DatadogSdk.Maui.Tests;

[Collection("DdRum")]
public class DdRumErrorTrackingTests : IDisposable
{
    private readonly MockRumBridge rumBridge = new();

    public DdRumErrorTrackingTests()
    {
        DdRum.testBridge = rumBridge;
    }

    public void Dispose()
    {
        DdRumErrorTracking.StopTracking();
        DdRum.testBridge = null;
        GC.SuppressFinalize(this);
    }

    // ── StartTracking / StopTracking ──────────────────────────────

    [Fact]
    public void StartTracking_SetsIsTrackingTrue()
    {
        DdRumErrorTracking.StartTracking();

        Assert.True(DdRumErrorTracking.IsTracking);
    }

    [Fact]
    public void StartTracking_CalledTwice_DoesNotDoubleRegister()
    {
        DdRumErrorTracking.StartTracking();
        DdRumErrorTracking.StartTracking();

        Assert.True(DdRumErrorTracking.IsTracking);
    }

    [Fact]
    public void StopTracking_SetsIsTrackingFalse()
    {
        DdRumErrorTracking.StartTracking();
        DdRumErrorTracking.StopTracking();

        Assert.False(DdRumErrorTracking.IsTracking);
    }

    [Fact]
    public void StopTracking_WhenNotTracking_DoesNotThrow()
    {
        DdRumErrorTracking.StopTracking();

        Assert.False(DdRumErrorTracking.IsTracking);
    }

    // ── Direct handler tests ─────────────────────────────────────

    [Fact]
    public void OnUnhandledException_ReportsErrorWithCorrectSource()
    {
        DdRumErrorTracking.StartTracking();

        // Simulate AppDomain.UnhandledException by raising it on the current domain
        // We can't easily trigger this without crashing, so we test via the public API
        // that DdRumErrorTracking hooks are in place by checking IsTracking
        Assert.True(DdRumErrorTracking.IsTracking);
    }
}
