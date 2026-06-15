/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.Globalization;
using System.Security.Cryptography;

namespace Datadog.Maui.AutoTracking
{
    /// <summary>
    /// Holds a 128-bit trace ID, 64-bit span ID, and sampling decision for a single outgoing request.
    /// </summary>
    internal readonly struct TracingContext
    {
        // 128-bit trace ID split into two 64-bit halves.
        // High: Unix timestamp (seconds) in bits 127-96, zeros in bits 95-64.
        // Low:  64 random bits.
        public ulong TraceIdHigh { get; init; }
        public ulong TraceIdLow { get; init; }

        // 64-bit random span ID.
        public ulong SpanId { get; init; }

        // Sampling decision set by DistributedTracingSampler.
        public bool IsSampled { get; init; }

        // ── Formatters (aligned with RN TracingIdFormat) ─────────────────────

        // x-datadog-trace-id: low 64 bits as decimal  (TracingIdFormat.lowDecimal)
        public string TraceIdLowDecimal
            => TraceIdLow.ToString(CultureInfo.InvariantCulture);

        // x-datadog-tags _dd.p.tid: high 64 bits as 16-char hex  (TracingIdFormat.paddedHighHex)
        public string TraceIdHighPaddedHex
            => TraceIdHigh.ToString("x16", CultureInfo.InvariantCulture);

        // traceparent, b3, _dd.trace_id: full 128 bits as 32-char hex  (TracingIdFormat.paddedHex)
        public string TraceIdPaddedHex
            => TraceIdHigh.ToString("x16", CultureInfo.InvariantCulture)
             + TraceIdLow.ToString("x16", CultureInfo.InvariantCulture);

        // x-datadog-parent-id, _dd.span_id: span as decimal  (TracingIdFormat.decimal)
        public string SpanIdDecimal
            => SpanId.ToString(CultureInfo.InvariantCulture);

        // traceparent, tracestate, b3: span as 16-char hex  (TracingIdFormat.paddedHex for span)
        public string SpanIdPaddedHex
            => SpanId.ToString("x16", CultureInfo.InvariantCulture);

        // ── Factory ──────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a new TracingContext with cryptographically random IDs.
        /// IsSampled is false by default; the caller should use DistributedTracingSampler to set it.
        /// </summary>
        public static TracingContext Generate()
        {
            Span<byte> bytes = stackalloc byte[16]; // 8 for TraceIdLow + 8 for SpanId
            ulong traceIdLow, spanId;

            // W3C traceparent requires parent-id to be non-zero; re-draw on the astronomically
            // rare chance that RandomNumberGenerator produces all-zero bytes.
            do
            {
                RandomNumberGenerator.Fill(bytes);
                traceIdLow = BitConverter.ToUInt64(bytes[..8]);
                spanId = BitConverter.ToUInt64(bytes[8..]);
            } while (traceIdLow == 0 || spanId == 0);

            // High: Unix timestamp (seconds) masked to 32 bits, shifted into the upper half.
            // Lower 32 bits of TraceIdHigh are zero.
            // Cast to uint truncates to 32 bits naturally — no explicit bitmask needed.
            ulong unixSeconds = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            ulong traceIdHigh = unixSeconds << 32;

            return new TracingContext
            {
                TraceIdHigh = traceIdHigh,
                TraceIdLow = traceIdLow,
                SpanId = spanId,
                IsSampled = false,
            };
        }

        /// <summary>Returns a copy with IsSampled set to the given value.</summary>
        public TracingContext WithSampled(bool sampled)
            => new TracingContext
            {
                TraceIdHigh = TraceIdHigh,
                TraceIdLow = TraceIdLow,
                SpanId = SpanId,
                IsSampled = sampled,
            };
    }
}
