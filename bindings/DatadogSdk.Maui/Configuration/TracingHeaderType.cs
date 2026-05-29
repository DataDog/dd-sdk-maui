/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

namespace DatadogSdk.Maui.Configuration
{
    /// <summary>
    /// Type of tracing header to inject for distributed tracing.
    /// </summary>
    public enum TracingHeaderType
    {
        Datadog,
        B3,
        B3Multi,
        TraceContext
    }
}
