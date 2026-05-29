/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

namespace DatadogSdk.Maui.Configuration
{
    /// <summary>
    /// Represents a RUM resource event that can be modified or dropped by a ResourceEventMapper.
    /// </summary>
    public class DdRumResourceEvent
    {
        /// <summary>
        /// Unique resource key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// HTTP method (GET, POST, etc.).
        /// </summary>
        public RumResourceMethod Method { get; set; }

        /// <summary>
        /// Request URL.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// HTTP status code (0 for network failure).
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Resource kind (xhr, image, css, etc.).
        /// </summary>
        public RumResourceKind Kind { get; set; }

        /// <summary>
        /// Response body size in bytes (-1 if unknown).
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Additional context attributes.
        /// </summary>
        public Dictionary<string, object> Context { get; set; }

        internal DdRumResourceEvent(string key, RumResourceMethod method, string url,
                                     int statusCode, RumResourceKind kind, long size,
                                     Dictionary<string, object> context)
        {
            Key = key;
            Method = method;
            Url = url;
            StatusCode = statusCode;
            Kind = kind;
            Size = size;
            Context = context;
        }
    }
}
