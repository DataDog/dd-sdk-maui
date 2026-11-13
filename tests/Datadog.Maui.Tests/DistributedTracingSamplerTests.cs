/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using Datadog.Maui.AutoTracking;
using Xunit;

namespace Datadog.Maui.Tests;

public class DistributedTracingSamplerTests
{
    // ── 100% rate ─────────────────────────────────────────────────────────────

    [Fact]
    public void ShouldSample_100Percent_AlwaysTrue()
    {
        Assert.True(DistributedTracingSampler.ShouldSample(100.0, null, 0UL));
        Assert.True(DistributedTracingSampler.ShouldSample(100.0, null, ulong.MaxValue));
        Assert.True(DistributedTracingSampler.ShouldSample(100.0, "aaaaaaaa-0000-0000-0000-000000000001", 42UL));
    }

    // ── 0% rate ───────────────────────────────────────────────────────────────

    [Fact]
    public void ShouldSample_0Percent_AlwaysFalse()
    {
        Assert.False(DistributedTracingSampler.ShouldSample(0.0, null, 0UL));
        Assert.False(DistributedTracingSampler.ShouldSample(0.0, null, ulong.MaxValue));
        Assert.False(DistributedTracingSampler.ShouldSample(0.0, "aaaaaaaa-0000-0000-0000-000000000001", 42UL));
    }

    // ── Null session ID falls back to trace ID ────────────────────────────────

    [Fact]
    public void ShouldSample_NullSessionId_FallsBackToTraceId()
    {
        const ulong traceId = 0x1234567890abcdefUL;

        var first = DistributedTracingSampler.ShouldSample(50.0, null, traceId);
        var second = DistributedTracingSampler.ShouldSample(50.0, null, traceId);

        // Same trace ID always yields the same result (deterministic).
        Assert.Equal(first, second);
    }

    // ── Same session ID always yields the same decision ───────────────────────

    [Fact]
    public void ShouldSample_WithSessionId_DeterministicPerSession()
    {
        const string sessionId = "aaaabbbb-cccc-dddd-eeee-ffff00001234";

        var first = DistributedTracingSampler.ShouldSample(50.0, sessionId, 0UL);
        var second = DistributedTracingSampler.ShouldSample(50.0, sessionId, 0UL);

        Assert.Equal(first, second);
    }

    // ── Different session IDs can produce different decisions ─────────────────

    [Fact]
    public void ShouldSample_DifferentSessionIds_CanProduceDifferentResults()
    {
        // Generate 100 distinct session IDs and collect the sampling decisions at 50%.
        // With a deterministic Knuth-factor hash spread, we expect at least one true
        // and one false among 100 distinct inputs.
        var results = new HashSet<bool>();

        for (int i = 0; i < 100; i++)
        {
            // Vary the last UUID group (the one the sampler extracts).
            var sessionId = $"aaaaaaaa-0000-0000-0000-{i:x12}";
            results.Add(DistributedTracingSampler.ShouldSample(50.0, sessionId, 0UL));
        }

        Assert.Contains(true, results);
        Assert.Contains(false, results);
    }

    // ── 50% rate — roughly half sampled ──────────────────────────────────────

    [Fact]
    public void ShouldSample_50Percent_ApproximatelyHalfSampled()
    {
        const int iterations = 1000;
        int sampled = 0;

        for (ulong i = 0; i < iterations; i++)
        {
            if (DistributedTracingSampler.ShouldSample(50.0, null, i))
                sampled++;
        }

        // Expect roughly 40-60% sampled for 1000 sequential trace IDs.
        Assert.InRange(sampled, 400, 600);
    }

    // ── Combined sampling rebased on the session ──────────────────────────────
    //
    // Shared vectors with dd-sdk-reactnative and dd-sdk-flutter so all three SDKs make
    // identical decisions. Hash positions (percent of the 64-bit range):
    //   session …0469 = 0.357%, …0015 = 26.490%, …d860b2b9437a = 68.673%, …0053 = 99.938%
    //   trace 15352856648520921629 = 8.204%, 4751997750760398084 = 89.610%

    [Theory]
    // combined 25% keeps low-hash session
    [InlineData("00000000-0000-0000-0000-000000000469", 50.0, 4751997750760398084UL, 50.0, true)]
    // combined 25% drops high-hash session
    [InlineData("00000000-0000-0000-0000-000000000053", 50.0, 15352856648520921629UL, 50.0, false)]
    // combined 25% drops it; raw 50% would keep it
    [InlineData("00000000-0000-0000-0000-000000000015", 50.0, 15352856648520921629UL, 50.0, false)]
    // 100% session leaves trace rate unchanged
    [InlineData("00000000-0000-0000-0000-000000000015", 100.0, 4751997750760398084UL, 50.0, true)]
    // 0% session drops all
    [InlineData("00000000-0000-0000-0000-000000000469", 0.0, 15352856648520921629UL, 100.0, false)]
    // 0% trace drops all
    [InlineData("00000000-0000-0000-0000-000000000469", 100.0, 15352856648520921629UL, 0.0, false)]
    // combined 40%; raw 80% would keep it
    [InlineData("a1b2c3d4-e5f6-7890-abcd-d860b2b9437a", 50.0, 15352856648520921629UL, 80.0, false)]
    public void ShouldSample_WithSessionId_SamplesAtCombinedRate(
        string sessionId, double sessionSampleRate, ulong traceIdLow, double traceSampleRate, bool expected)
    {
        Assert.Equal(expected,
            DistributedTracingSampler.ShouldSample(traceSampleRate, sessionId, traceIdLow, sessionSampleRate));
    }

    [Fact]
    public void ShouldSample_WithSessionId_100PercentTraceRate_StillRebasedOnSession()
    {
        // A 100% trace rate must not short-circuit: a 50% session rate still gives 50%.
        // Session …0053 hashes to 99.938%, above the combined 50% threshold.
        Assert.False(DistributedTracingSampler.ShouldSample(
            100.0, "00000000-0000-0000-0000-000000000053", 0UL, sessionSampleRate: 50.0));
        // Session …0469 hashes to 0.357%, below it.
        Assert.True(DistributedTracingSampler.ShouldSample(
            100.0, "00000000-0000-0000-0000-000000000469", 0UL, sessionSampleRate: 50.0));
    }

    [Fact]
    public void ShouldSample_NullSessionId_IgnoresSessionSampleRate()
    {
        // Without a session, the trace ID decides at the trace rate; the 0% session rate is ignored.
        // Trace 15352856648520921629 hashes to 8.204%, below 50%.
        Assert.True(DistributedTracingSampler.ShouldSample(
            50.0, null, 15352856648520921629UL, sessionSampleRate: 0.0));
    }
}
