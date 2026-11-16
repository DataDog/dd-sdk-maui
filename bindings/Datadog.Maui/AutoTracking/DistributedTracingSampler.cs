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
    /// requests within the same session are sampled consistently. With a session, the
    /// decision is rebased on the session: traces are sampled at
    /// <c>sessionSampleRate × traceSampleRate</c>, as the iOS and Android SDKs do, so traced
    /// sessions are always sampled-in RUM sessions. Falls back to the trace ID's low bits at
    /// the trace rate when the session ID is unavailable.
    /// </summary>
    internal static class DistributedTracingSampler
    {
        // Same constant as RN: 1111111111111111111
        private const ulong KnuthFactor = 1111111111111111111UL;

        /// <summary>
        /// Returns true if the trace should be sampled.
        /// </summary>
        /// <param name="traceSampleRate">Percentage 0–100 (e.g. 20.0 = 20%).</param>
        /// <param name="sessionId">Current RUM session UUID (nullable). Used as primary seed.</param>
        /// <param name="traceIdLow">Low 64 bits of the trace ID. Used as fallback seed.</param>
        /// <param name="sessionSampleRate">
        /// RUM session sample rate, percentage 0–100. The decision is rebased on it when a
        /// session ID is available; ignored otherwise.
        /// </param>
        internal static bool ShouldSample(double traceSampleRate, string? sessionId, ulong traceIdLow,
            double sessionSampleRate = 100.0)
        {
            if (traceSampleRate <= 0.0) return false;

            var sessionLowBits = ParseSessionIdLowBits(sessionId);
            if (sessionLowBits.HasValue)
            {
                var combinedSampleRate = ClampRate(sessionSampleRate) * (ClampRate(traceSampleRate) / 100.0);
                if (combinedSampleRate >= 100.0) return true;

                return IsSampled(sessionLowBits.Value, GetMaxSampledTraceId(combinedSampleRate));
            }

            if (traceSampleRate >= 100.0) return true;

            return IsSampled(traceIdLow, GetMaxSampledTraceId(traceSampleRate));
        }

        // ulong multiplication overflows naturally (mod 2^64)
        private static bool IsSampled(ulong lowBits, ulong maxSampledId)
            => unchecked(lowBits * KnuthFactor) < maxSampledId;

        private static double ClampRate(double rate) => Math.Clamp(rate, 0.0, 100.0);

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
