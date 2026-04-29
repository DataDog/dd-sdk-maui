/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

namespace DatadogSdk.Maui.Configuration
{
    /// <summary>
    /// Privacy level for images in Session Replay recordings.
    /// </summary>
    public enum ImagePrivacy
    {
        /// <summary>All images are replaced with placeholders.</summary>
        MaskAll,
        /// <summary>All images are displayed without masking.</summary>
        MaskNone
    }
}
