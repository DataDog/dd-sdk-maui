/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System;
using Foundation;
using ObjCRuntime;

namespace Datadog.iOS.Binding
{
    // DdTrace - Tracing functionality
    [BaseType(typeof(NSObject))]
    interface DdTrace
    {
        [Static]
        [Export("enableTrace:")]
        void EnableTrace([NullAllowed] string customEndpoint);

        [Static]
        [Export("startSpan:context:timestampMs:")]
        string StartSpan(string operation, NSDictionary context, long timestampMs);

        [Static]
        [Export("finishSpan:context:timestampMs:")]
        void FinishSpan(string spanId, NSDictionary context, long timestampMs);
    }
}
