/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

using DatadogSdk.Maui.Configuration;

namespace DatadogSdk.Maui.AutoTracking
{
    /// <summary>
    /// Resolves the RUM resource kind from an HTTP response Content-Type header.
    /// </summary>
    internal static class ResourceKindResolver
    {
        /// <summary>
        /// Derive the resource kind from a Content-Type media type string.
        /// </summary>
        /// <param name="contentType">The Content-Type header value (e.g., "application/json", "image/png"). May be null.</param>
        /// <returns>The appropriate RumResourceKind.</returns>
        internal static RumResourceKind Resolve(string? contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return RumResourceKind.Other;

            var type = contentType.ToLowerInvariant();

            if (type.StartsWith("image/"))
                return RumResourceKind.Image;

            if (type.Contains("javascript"))
                return RumResourceKind.Js;

            if (type.Contains("css"))
                return RumResourceKind.Css;

            if (type.Contains("font") || type.StartsWith("application/font") || type.StartsWith("application/x-font"))
                return RumResourceKind.Font;

            if (type.StartsWith("video/") || type.StartsWith("audio/"))
                return RumResourceKind.Media;

            if (type.Contains("json") || type.Contains("xml"))
                return RumResourceKind.Native;

            if (type.StartsWith("text/html"))
                return RumResourceKind.Native;

            return RumResourceKind.Other;
        }
    }
}
