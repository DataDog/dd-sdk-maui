/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

namespace DatadogSdk.Maui.Configuration
{
    /// <summary>
    /// Type of resource tracked by RUM.
    /// </summary>
    public enum RumResourceKind
    {
        Xhr,
        Native,
        Fetch,
        Document,
        Beacon,
        Image,
        Font,
        Css,
        Media,
        Js,
        Other
    }
}
