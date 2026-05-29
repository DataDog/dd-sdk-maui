/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using DatadogSdk.Maui;

namespace DatadogSdk.Maui.Tests;

internal class MockSessionReplayBridge : DdSessionReplay.ISessionReplayBridge
{
    public int EnableCallCount { get; private set; }
    public double? LastReplaySampleRate { get; private set; }
    public string? LastTextAndInputPrivacy { get; private set; }
    public string? LastImagePrivacy { get; private set; }
    public string? LastTouchPrivacy { get; private set; }
    public string? LastCustomEndpoint { get; private set; }

    public void Enable(double replaySampleRate, string textAndInputPrivacy,
                       string imagePrivacy, string touchPrivacy, string? customEndpoint)
    {
        EnableCallCount++;
        LastReplaySampleRate = replaySampleRate;
        LastTextAndInputPrivacy = textAndInputPrivacy;
        LastImagePrivacy = imagePrivacy;
        LastTouchPrivacy = touchPrivacy;
        LastCustomEndpoint = customEndpoint;
    }
}
