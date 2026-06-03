/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using Datadog.Maui.AutoTracking;
using Datadog.Maui.Configuration;
using Xunit;

namespace Datadog.Maui.Tests;

/// <summary>
/// Sanity tests for the tracing path of DdAutoResourceTracker.
/// These tests verify construction-time behaviour (reading DdSdk.Configuration)
/// without driving the DiagnosticListener plumbing.
/// </summary>
[Collection("InternalLog")]
public class DdAutoResourceTrackerTracingTests : IDisposable
{
    private readonly MockNativeSdkBridge _sdkBridge = new();
    private readonly MockRumBridge _rumBridge = new();

    public DdAutoResourceTrackerTracingTests()
    {
        DdSdk.testBridge = _sdkBridge;
        DdRum.testBridge = _rumBridge;
    }

    public void Dispose()
    {
        DdSdk.testBridge = null;
        DdRum.testBridge = null;
        DdSdk.Initialize(new DdSdkConfiguration
        {
            ClientToken = "reset",
            Environment = "reset",
            Site = DatadogSite.Us1,
        });
        GC.SuppressFinalize(this);
    }

    // ── Construction with no first-party hosts ────────────────────────────────

    [Fact]
    public void Constructor_WithNoFirstPartyHosts_DoesNotThrow()
    {
        // Explicitly initialize with no FirstPartyHosts so the matcher receives null.
        DdSdk.Initialize(new DdSdkConfiguration
        {
            ClientToken = "fake-token",
            Environment = "test",
            Site = DatadogSite.Us1,
        });

        var exception = Record.Exception(() => new DdAutoResourceTracker(20.0));
        Assert.Null(exception);
    }

    // ── Construction with first-party hosts ───────────────────────────────────

    [Fact]
    public void Constructor_WithFirstPartyHosts_DoesNotThrow()
    {
        DdSdk.Initialize(new DdSdkConfiguration
        {
            ClientToken = "fake-token",
            Environment = "test",
            Site = DatadogSite.Us1,
            FirstPartyHosts = new List<FirstPartyHost>
            {
                new() { Match = "api.example.com", HeaderTypes = new List<TracingHeaderType> { TracingHeaderType.Datadog } },
                new() { Match = "cdn.example.com", HeaderTypes = new List<TracingHeaderType> { TracingHeaderType.TraceContext } },
            }
        });

        var ex = Record.Exception(() => new DdAutoResourceTracker(50.0));
        Assert.Null(ex);
    }

    // ── resourceTraceSampleRate clamping and bounds ───────────────────────────

    [Fact]
    public void Constructor_ResourceTraceSampleRate_ClampsToValidRange()
    {
        Assert.Equal(0.0, new DdAutoResourceTracker(0.0).ResourceTraceSampleRate);
        Assert.Equal(100.0, new DdAutoResourceTracker(100.0).ResourceTraceSampleRate);
        Assert.Equal(33.3, new DdAutoResourceTracker(33.3).ResourceTraceSampleRate);
        Assert.Equal(0.0, new DdAutoResourceTracker(-1.0).ResourceTraceSampleRate);
        Assert.Equal(100.0, new DdAutoResourceTracker(101.0).ResourceTraceSampleRate);
    }
}
