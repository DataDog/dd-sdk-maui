/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using Datadog.Maui.AutoTracking;
using Xunit;

namespace Datadog.Maui.Tests;

public class TracingContextTests
{
    // ── Generate() ───────────────────────────────────────────────────────────────

    [Fact]
    public void Generate_NeverReturnsZeroTraceIdLow()
    {
        for (int i = 0; i < 1_000; i++)
        {
            var ctx = TracingContext.Generate();
            Assert.NotEqual(0UL, ctx.TraceIdLow);
        }
    }

    [Fact]
    public void Generate_NeverReturnsZeroSpanId()
    {
        for (int i = 0; i < 1_000; i++)
        {
            var ctx = TracingContext.Generate();
            Assert.NotEqual(0UL, ctx.SpanId);
        }
    }

    [Fact]
    public void Generate_TraceIdHighEncodesSecs()
    {
        var before = (ulong)(uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var ctx = TracingContext.Generate();
        var after = (ulong)(uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var high = ctx.TraceIdHigh >> 32;
        Assert.InRange(high, before, after);
        Assert.Equal(0UL, ctx.TraceIdHigh & 0x0000_0000_FFFF_FFFFUL);
    }

    [Fact]
    public void Generate_IsSampledDefaultsFalse()
    {
        Assert.False(TracingContext.Generate().IsSampled);
    }

    // ── WithSampled() ────────────────────────────────────────────────────────────

    [Fact]
    public void WithSampled_PreservesIdsAndFlipsSampledFlag()
    {
        var original = TracingContext.Generate();

        var sampled = original.WithSampled(true);
        Assert.Equal(original.TraceIdHigh, sampled.TraceIdHigh);
        Assert.Equal(original.TraceIdLow, sampled.TraceIdLow);
        Assert.Equal(original.SpanId, sampled.SpanId);
        Assert.True(sampled.IsSampled);

        var unsampled = original.WithSampled(false);
        Assert.False(unsampled.IsSampled);
    }

    // ── Formatters ───────────────────────────────────────────────────────────────

    [Fact]
    public void Formatters_ProduceExpectedStrings()
    {
        var ctx = new TracingContext
        {
            TraceIdHigh = 0x6745d8a000000000UL,
            TraceIdLow = 0xabcdef1234567890UL,
            SpanId = 0x0123456789abcdefUL,
            IsSampled = true,
        };

        Assert.Equal("12379813812177893520", ctx.TraceIdLowDecimal);
        Assert.Equal("6745d8a000000000", ctx.TraceIdHighPaddedHex);
        Assert.Equal("6745d8a000000000abcdef1234567890", ctx.TraceIdPaddedHex);
        Assert.Equal("81985529216486895", ctx.SpanIdDecimal);
        Assert.Equal("0123456789abcdef", ctx.SpanIdPaddedHex);
    }
}
