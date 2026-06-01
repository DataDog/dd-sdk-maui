/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

namespace Datadog.Maui.Configuration
{
    /// <summary>
    /// Type of user action tracked by RUM.
    /// </summary>
    public enum RumActionType
    {
        Tap,
        Scroll,
        Swipe,
        Click,
        Back,
        ApplicationStart,
        Custom
    }
}
