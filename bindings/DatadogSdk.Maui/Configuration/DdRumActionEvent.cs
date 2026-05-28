/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

namespace DatadogSdk.Maui.Configuration
{
    /// <summary>
    /// Represents a RUM action event that can be modified or dropped by an ActionEventMapper.
    /// </summary>
    public class DdRumActionEvent
    {
        /// <summary>
        /// Type of action (tap, scroll, swipe, etc.).
        /// </summary>
        public RumActionType Type { get; set; }

        /// <summary>
        /// Action target name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Additional context attributes.
        /// </summary>
        public Dictionary<string, object> Context { get; set; }

        /// <summary>
        /// Timestamp in milliseconds since epoch.
        /// </summary>
        public long TimestampMs { get; set; }

        internal DdRumActionEvent(RumActionType type, string name,
                                   Dictionary<string, object> context, long timestampMs)
        {
            Type = type;
            Name = name;
            Context = context;
            TimestampMs = timestampMs;
        }
    }
}
