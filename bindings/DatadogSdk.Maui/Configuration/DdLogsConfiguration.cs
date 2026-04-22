/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

namespace DatadogSdk.Maui.Configuration
{
    /// <summary>
    /// Configuration for the Datadog Logs module.
    /// </summary>
    public class DdLogsConfiguration
    {
        /// <summary>
        /// Custom server endpoint where Logs are sent.
        /// If not provided, logs are sent to the standard Datadog endpoint based on your site configuration.
        /// </summary>
        /// <example>
        /// https://custom-logs-endpoint.example.com/v1/input
        /// </example>
        public string? CustomEndpoint { get; set; }
    }
}
