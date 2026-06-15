/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.Globalization;

namespace Datadog.Maui.AutoTracking
{
    /// <summary>
    /// Deterministic Knuth-factor sampler.
    ///
    /// Uses the last UUID group of the RUM session ID as the sampling seed so that all
    /// requests within the same session are sampled consistently. Falls back to the trace
    /// ID's low bits when the session ID is unavailable.
    /// </summary>
    internal static class DistributedTracingSampler
    {
        // Same constant as RN: 1111111111111111111
        private const ulong KnuthFactor = 1111111111111111111UL;

        /// <summary>
        /// Returns true if the trace should be sampled.
        /// </summary>
        /// <param name="sampleRate">Percentage 0–100 (e.g. 20.0 = 20%).</param>
        /// <param name="sessionId">Current RUM session UUID (nullable). Used as primary seed.</param>
        /// <param name="traceIdLow">Low 64 bits of the trace ID. Used as fallback seed.</param>
        internal static bool ShouldSample(double sampleRate, string? sessionId, ulong traceIdLow)
        {
            if (sampleRate >= 100.0) return true;
            if (sampleRate <= 0.0) return false;

            ulong lowBits = ParseSessionIdLowBits(sessionId) ?? traceIdLow;
            ulong maxSampledId = GetMaxSampledTraceId(sampleRate);

            // ulong multiplication overflows naturally (mod 2^64)
            return unchecked(lowBits * KnuthFactor) < maxSampledId;
        }

        // MAX_TRACE_ID * (sampleRate / 100).
        private static ulong GetMaxSampledTraceId(double sampleRate)
            => (ulong)(ulong.MaxValue * (sampleRate / 100.0));

        // Extracts the low bits from the last UUID group (12 hex chars = 48 bits).
        // UUID format: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
        //                                           ^^^^^^^^^^^^^^^^  ← last group
        private static ulong? ParseSessionIdLowBits(string? sessionId)
        {
            if (sessionId is null) return null;

            var lastDash = sessionId.LastIndexOf('-');
            if (lastDash < 0 || lastDash >= sessionId.Length - 1) return null;

            var lastGroup = sessionId.AsSpan(lastDash + 1);
            if (ulong.TryParse(lastGroup, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var bits))
                return bits;

            return null;
        }
    }
}
