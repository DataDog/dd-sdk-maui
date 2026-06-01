/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

namespace Datadog.Maui.Configuration
{
    /// <summary>
    /// Privacy level for touch interactions in Session Replay recordings.
    /// </summary>
    public enum TouchPrivacy
    {
        /// <summary>Touch interactions are hidden.</summary>
        Hide,
        /// <summary>Touch interactions are visible.</summary>
        Show
    }
}
