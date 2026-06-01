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
    // DdLogs - Logging functionality
    [BaseType(typeof(NSObject))]
    interface DdLogs
    {
        [Static]
        [Export("enableLogs:")]
        void EnableLogs([NullAllowed] string customEndpoint);

        [Static]
        [Export("logDebug:")]
        void LogDebug(string message);

        [Static]
        [Export("logInfo:")]
        void LogInfo(string message);

        [Static]
        [Export("logWarn:")]
        void LogWarn(string message);

        [Static]
        [Export("logError:")]
        void LogError(string message);

        [Static]
        [Export("logWithAttributesWithLevel:message:attributes:")]
        void LogWithAttributes(string level, string message, NSDictionary attributes);
    }
}
