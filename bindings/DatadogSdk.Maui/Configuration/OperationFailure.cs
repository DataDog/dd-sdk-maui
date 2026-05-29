/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

namespace DatadogSdk.Maui.Configuration
{
    /// <summary>
    /// Represents the reason for a failed operation.
    /// </summary>
    public enum OperationFailure
    {
        Error,
        Abandoned,
        Other
    }
}
