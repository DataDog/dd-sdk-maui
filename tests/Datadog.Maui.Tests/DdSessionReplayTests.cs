/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using Datadog.Maui;
using Datadog.Maui.Configuration;
using Xunit;

namespace Datadog.Maui.Tests;

public class DdSessionReplayTests : IDisposable
{
    private readonly MockSessionReplayBridge bridge = new();

    public DdSessionReplayTests()
    {
        DdSessionReplay.testBridge = bridge;
    }

    public void Dispose()
    {
        DdSessionReplay.testBridge = null;
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Enable_PassesSampleRateToBridge()
    {
        DdSessionReplay.Enable(new SessionReplayConfiguration
        {
            ReplaySampleRate = 80.0
        });

        Assert.Equal(1, bridge.EnableCallCount);
        Assert.Equal(80.0, bridge.LastReplaySampleRate);
    }

    [Fact]
    public void Enable_DefaultSampleRateIs100()
    {
        DdSessionReplay.Enable(new SessionReplayConfiguration());

        Assert.Equal(100.0, bridge.LastReplaySampleRate);
    }

    [Fact]
    public void Enable_PassesCustomEndpointToBridge()
    {
        DdSessionReplay.Enable(new SessionReplayConfiguration
        {
            CustomEndpoint = "https://sr.example.com"
        });

        Assert.Equal("https://sr.example.com", bridge.LastCustomEndpoint);
    }

    [Fact]
    public void Enable_CustomEndpointDefaultsToNull()
    {
        DdSessionReplay.Enable(new SessionReplayConfiguration());

        Assert.Null(bridge.LastCustomEndpoint);
    }

    // --- Privacy defaults ---

    [Fact]
    public void Enable_DefaultPrivacyLevels()
    {
        DdSessionReplay.Enable(new SessionReplayConfiguration());

        Assert.Equal("mask_all", bridge.LastTextAndInputPrivacy);
        Assert.Equal("mask_all", bridge.LastImagePrivacy);
        Assert.Equal("hide", bridge.LastTouchPrivacy);
    }

    // --- TextAndInputPrivacy ---

    [Theory]
    [InlineData(TextAndInputPrivacy.MaskAll, "mask_all")]
    [InlineData(TextAndInputPrivacy.MaskAllInputs, "mask_all_inputs")]
    [InlineData(TextAndInputPrivacy.MaskSensitiveInputs, "mask_sensitive_inputs")]
    public void Enable_ConvertsTextAndInputPrivacy(TextAndInputPrivacy level, string expected)
    {
        DdSessionReplay.Enable(new SessionReplayConfiguration
        {
            TextAndInputPrivacyLevel = level
        });

        Assert.Equal(expected, bridge.LastTextAndInputPrivacy);
    }

    // --- ImagePrivacy ---

    [Theory]
    [InlineData(ImagePrivacy.MaskAll, "mask_all")]
    [InlineData(ImagePrivacy.MaskNone, "mask_none")]
    public void Enable_ConvertsImagePrivacy(ImagePrivacy level, string expected)
    {
        DdSessionReplay.Enable(new SessionReplayConfiguration
        {
            ImagePrivacyLevel = level
        });

        Assert.Equal(expected, bridge.LastImagePrivacy);
    }

    // --- TouchPrivacy ---

    [Theory]
    [InlineData(TouchPrivacy.Hide, "hide")]
    [InlineData(TouchPrivacy.Show, "show")]
    public void Enable_ConvertsTouchPrivacy(TouchPrivacy level, string expected)
    {
        DdSessionReplay.Enable(new SessionReplayConfiguration
        {
            TouchPrivacyLevel = level
        });

        Assert.Equal(expected, bridge.LastTouchPrivacy);
    }
}
