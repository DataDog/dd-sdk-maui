/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

namespace DatadogSdk.Maui.Configuration
{
    /// <summary>
    /// Represents user information attached to all Datadog events.
    /// </summary>
    public class DdUserInfo
    {
        /// <summary>
        /// Unique user identifier. Required.
        /// </summary>
        public required string Id { get; set; }

        /// <summary>
        /// User display name. Optional.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// User email address. Optional.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Additional custom attributes for the user. Optional.
        /// </summary>
        public Dictionary<string, object>? ExtraInfo { get; set; }
    }
}
