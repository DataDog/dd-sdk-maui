/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

namespace DatadogSdk.Maui.Configuration
{
    /// <summary>
    /// Defines a first-party host for distributed tracing.
    /// Requests to matching hosts will have tracing headers injected.
    /// </summary>
    public class FirstPartyHost
    {
        /// <summary>
        /// The host or domain to match (e.g., "api.example.com").
        /// </summary>
        public required string Match { get; set; }

        /// <summary>
        /// The types of tracing headers to inject for requests to this host.
        /// </summary>
        public required List<TracingHeaderType> HeaderTypes { get; set; }
    }
}
