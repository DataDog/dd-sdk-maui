/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

using System;
using Foundation;
using ObjCRuntime;

namespace DatadogSdk.iOS.Binding
{
    // DdTelemetry - SDK-internal telemetry forwarding (errors, debug, configuration)
    [BaseType(typeof(NSObject))]
    interface DdTelemetry
    {
        [Static]
        [Export("errorWithId:message:kind:stack:")]
        void Error(string id, string message, [NullAllowed] string kind, [NullAllowed] string stack);

        [Static]
        [Export("debugWithId:message:")]
        void Debug(string id, string message);

        [Static]
        [Export("reportConfiguration:")]
        void ReportConfiguration(NSDictionary fields);
    }
}
