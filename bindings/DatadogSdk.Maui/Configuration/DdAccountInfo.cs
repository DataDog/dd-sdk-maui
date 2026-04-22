/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

namespace DatadogSdk.Maui.Configuration
{
    /// <summary>
    /// Represents account information attached to all Datadog events.
    /// </summary>
    public class DdAccountInfo
    {
        /// <summary>
        /// Unique account identifier. Required.
        /// </summary>
        public required string Id { get; set; }

        /// <summary>
        /// Account display name. Optional.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Additional custom attributes for the account. Optional.
        /// </summary>
        public Dictionary<string, object>? ExtraInfo { get; set; }
    }
}
