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
}
