/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.Net.Http;
using Datadog.Maui.Configuration;

namespace Datadog.Maui.AutoTracking
{
    /// <summary>
    /// Writes distributed tracing headers into an outgoing HttpRequestMessage.
    ///
    /// Supported formats: Datadog, W3C TraceContext, B3 (single), B3 (multi).
    /// </summary>
    internal static class TracingHeaderWriter
    {
        /// <summary>
        /// Writes all headers for each requested TracingHeaderType into the request.
        /// </summary>
        internal static void Write(
            HttpRequestMessage request,
            TracingContext ctx,
            IReadOnlyList<TracingHeaderType> types)
        {
            foreach (var type in types)
            {
                switch (type)
                {
                    case TracingHeaderType.Datadog:
                        WriteDatadog(request, ctx);
                        break;
                    case TracingHeaderType.TraceContext:
                        WriteTraceContext(request, ctx);
                        break;
                    case TracingHeaderType.B3:
                        WriteB3(request, ctx);
                        break;
                    case TracingHeaderType.B3Multi:
                        WriteB3Multi(request, ctx);
                        break;
                }
            }
        }

        // ── Datadog ──────────────────────────────────────────────────────────
        // x-datadog-origin: rum
        // x-datadog-sampling-priority: 1|0
        // x-datadog-trace-id: <low 64 bits decimal>
        // x-datadog-parent-id: <span decimal>
        // x-datadog-tags: _dd.p.tid=<high 64 bits hex>
        private static void WriteDatadog(HttpRequestMessage request, TracingContext ctx)
        {
            var sampled = ctx.IsSampled ? "1" : "0";
            TryAdd(request, "x-datadog-origin", "rum");
            TryAdd(request, "x-datadog-sampling-priority", sampled);
            TryAdd(request, "x-datadog-trace-id", ctx.TraceIdLowDecimal);
            TryAdd(request, "x-datadog-parent-id", ctx.SpanIdDecimal);
            TryAdd(request, "x-datadog-tags", $"_dd.p.tid={ctx.TraceIdHighPaddedHex}");
        }

        // ── W3C Trace Context ─────────────────────────────────────────────────
        // traceparent: 00-<32hex trace>-<16hex span>-01|00
        // tracestate:  dd=s:1|0;o:rum;p:<16hex span>
        private static void WriteTraceContext(HttpRequestMessage request, TracingContext ctx)
        {
            var flags = ctx.IsSampled ? "01" : "00";
            TryAdd(request, "traceparent",
                $"00-{ctx.TraceIdPaddedHex}-{ctx.SpanIdPaddedHex}-{flags}");
            TryAdd(request, "tracestate",
                $"dd=s:{(ctx.IsSampled ? "1" : "0")};o:rum;p:{ctx.SpanIdPaddedHex}");
        }

        // ── B3 single header ──────────────────────────────────────────────────
        // b3: <32hex trace>-<16hex span>-1|0
        private static void WriteB3(HttpRequestMessage request, TracingContext ctx)
        {
            var sampled = ctx.IsSampled ? "1" : "0";
            TryAdd(request, "b3",
                $"{ctx.TraceIdPaddedHex}-{ctx.SpanIdPaddedHex}-{sampled}");
        }

        // ── B3 multi-header ───────────────────────────────────────────────────
        // X-B3-TraceId: <32hex trace>
        // X-B3-SpanId:  <16hex span>
        // X-B3-Sampled: 1|0
        private static void WriteB3Multi(HttpRequestMessage request, TracingContext ctx)
        {
            var sampled = ctx.IsSampled ? "1" : "0";
            TryAdd(request, "X-B3-TraceId", ctx.TraceIdPaddedHex);
            TryAdd(request, "X-B3-SpanId", ctx.SpanIdPaddedHex);
            TryAdd(request, "X-B3-Sampled", sampled);
        }

        private static void TryAdd(HttpRequestMessage request, string header, string value)
        {
            if (!request.Headers.Contains(header))
            {
                request.Headers.TryAddWithoutValidation(header, value);
            }
        }
    }
}
