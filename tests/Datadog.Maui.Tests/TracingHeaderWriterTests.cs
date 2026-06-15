/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.Net.Http;
using Datadog.Maui.AutoTracking;
using Datadog.Maui.Configuration;
using Xunit;

namespace Datadog.Maui.Tests;

public class TracingHeaderWriterTests
{
    // Fixed TracingContext used across all tests.
    //
    // TraceIdHigh = 0x6745d8a000000000  →  TraceIdHighPaddedHex = "6745d8a000000000"
    // TraceIdLow  = 0xabcdef1234567890  →  TraceIdLowDecimal    = "12379813812177893520"
    // SpanId      = 0x0123456789abcdef  →  SpanIdDecimal        = "81985529216486895"
    //                                       SpanIdPaddedHex      = "0123456789abcdef"
    // TraceIdPaddedHex = "6745d8a000000000abcdef1234567890"
    private static readonly TracingContext SampledCtx = new TracingContext
    {
        TraceIdHigh = 0x6745d8a000000000UL,
        TraceIdLow = 0xabcdef1234567890UL,
        SpanId = 0x0123456789abcdefUL,
        IsSampled = true,
    };

    private static readonly TracingContext UnsampledCtx = SampledCtx.WithSampled(false);

    private static HttpRequestMessage NewRequest()
        => new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/data");

    private static string? Header(HttpRequestMessage req, string name)
    {
        req.Headers.TryGetValues(name, out var values);
        return values?.FirstOrDefault();
    }

    // ── Datadog format ────────────────────────────────────────────────────────

    [Fact]
    public void Write_Datadog_SetsAllHeaders()
    {
        var req = NewRequest();
        TracingHeaderWriter.Write(req, SampledCtx, new[] { TracingHeaderType.Datadog });

        Assert.Equal("rum", Header(req, "x-datadog-origin"));
        Assert.Equal("1", Header(req, "x-datadog-sampling-priority"));
        Assert.Equal("12379813812177893520", Header(req, "x-datadog-trace-id"));
        Assert.Equal("81985529216486895", Header(req, "x-datadog-parent-id"));
        Assert.Equal("_dd.p.tid=6745d8a000000000", Header(req, "x-datadog-tags"));
    }

    [Fact]
    public void Write_Datadog_Unsampled_Priority0()
    {
        var req = NewRequest();
        TracingHeaderWriter.Write(req, UnsampledCtx, new[] { TracingHeaderType.Datadog });

        Assert.Equal("0", Header(req, "x-datadog-sampling-priority"));
    }

    // ── W3C Trace Context ─────────────────────────────────────────────────────

    [Fact]
    public void Write_TraceContext_SetsTraceparentAndTracestate()
    {
        var req = NewRequest();
        TracingHeaderWriter.Write(req, SampledCtx, new[] { TracingHeaderType.TraceContext });

        Assert.Equal(
            "00-6745d8a000000000abcdef1234567890-0123456789abcdef-01",
            Header(req, "traceparent"));
        Assert.Equal(
            "dd=s:1;o:rum;p:0123456789abcdef",
            Header(req, "tracestate"));
    }

    [Fact]
    public void Write_TraceContext_Unsampled_Flags00()
    {
        var req = NewRequest();
        TracingHeaderWriter.Write(req, UnsampledCtx, new[] { TracingHeaderType.TraceContext });

        Assert.Equal(
            "00-6745d8a000000000abcdef1234567890-0123456789abcdef-00",
            Header(req, "traceparent"));
        Assert.Equal(
            "dd=s:0;o:rum;p:0123456789abcdef",
            Header(req, "tracestate"));
    }

    // ── B3 single header ──────────────────────────────────────────────────────

    [Fact]
    public void Write_B3_SetsSingleHeader()
    {
        var req = NewRequest();
        TracingHeaderWriter.Write(req, SampledCtx, new[] { TracingHeaderType.B3 });

        Assert.Equal(
            "6745d8a000000000abcdef1234567890-0123456789abcdef-1",
            Header(req, "b3"));
    }

    // ── B3 multi-header ───────────────────────────────────────────────────────

    [Fact]
    public void Write_B3Multi_SetsThreeHeaders()
    {
        var req = NewRequest();
        TracingHeaderWriter.Write(req, SampledCtx, new[] { TracingHeaderType.B3Multi });

        Assert.Equal("6745d8a000000000abcdef1234567890", Header(req, "X-B3-TraceId"));
        Assert.Equal("0123456789abcdef", Header(req, "X-B3-SpanId"));
        Assert.Equal("1", Header(req, "X-B3-Sampled"));
    }

    // ── Multiple types ────────────────────────────────────────────────────────

    [Fact]
    public void Write_MultipleTypes_SetsAllFormats()
    {
        var req = NewRequest();
        TracingHeaderWriter.Write(req, SampledCtx, new[] { TracingHeaderType.Datadog, TracingHeaderType.TraceContext });

        // Datadog headers present
        Assert.Equal("rum", Header(req, "x-datadog-origin"));
        Assert.Equal("1", Header(req, "x-datadog-sampling-priority"));
        Assert.Equal("12379813812177893520", Header(req, "x-datadog-trace-id"));
        Assert.Equal("81985529216486895", Header(req, "x-datadog-parent-id"));
        Assert.Equal("_dd.p.tid=6745d8a000000000", Header(req, "x-datadog-tags"));

        // TraceContext headers present
        Assert.Equal(
            "00-6745d8a000000000abcdef1234567890-0123456789abcdef-01",
            Header(req, "traceparent"));
        Assert.Equal(
            "dd=s:1;o:rum;p:0123456789abcdef",
            Header(req, "tracestate"));
    }

    // ── Empty type list ───────────────────────────────────────────────────────

    [Fact]
    public void Write_EmptyTypes_SetsNoHeaders()
    {
        var req = NewRequest();
        TracingHeaderWriter.Write(req, SampledCtx, Array.Empty<TracingHeaderType>());

        Assert.False(req.Headers.Contains("x-datadog-origin"));
        Assert.False(req.Headers.Contains("traceparent"));
        Assert.False(req.Headers.Contains("b3"));
        Assert.False(req.Headers.Contains("X-B3-TraceId"));
    }
}
