/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System;
using Foundation;
using ObjCRuntime;

namespace DatadogSdk.iOS.Binding
{
    // DdRum - RUM functionality
    [BaseType(typeof(NSObject))]
    interface DdRum
    {
        [Static]
        [Export("enableRum:")]
        void EnableRum(NSDictionary configuration);

        [Static]
        [Export("addError:source:stacktrace:context:timestampMs:")]
        void AddError(string message, string source, string stacktrace, NSDictionary context, long timestampMs);

        // Views
        [Static]
        [Export("startView:name:context:timestampMs:")]
        void StartView(string key, string name, NSDictionary context, long timestampMs);

        [Static]
        [Export("stopView:context:timestampMs:")]
        void StopView(string key, NSDictionary context, long timestampMs);

        // Actions
        [Static]
        [Export("startAction:name:context:timestampMs:")]
        void StartAction(string type, string name, NSDictionary context, long timestampMs);

        [Static]
        [Export("stopAction:name:context:timestampMs:")]
        void StopAction(string type, string name, NSDictionary context, long timestampMs);

        [Static]
        [Export("addAction:name:context:timestampMs:")]
        void AddAction(string type, string name, NSDictionary context, long timestampMs);

        // Resources
        [Static]
        [Export("startResource:method:url:context:timestampMs:")]
        void StartResource(string key, string method, string url, NSDictionary context, long timestampMs);

        [Static]
        [Export("stopResource:statusCode:kind:size:context:timestampMs:")]
        void StopResource(string key, int statusCode, string kind, long size, NSDictionary context, long timestampMs);

        // Timing
        [Static]
        [Export("addTiming:")]
        void AddTiming(string name);

        [Static]
        [Export("addViewLoadingTime:")]
        void AddViewLoadingTime(bool overwrite);

        // Session
        [Static]
        [Export("stopSession")]
        void StopSession();

        // View Attributes
        [Static]
        [Export("addViewAttribute:value:")]
        void AddViewAttribute(string key, NSObject value);

        [Static]
        [Export("removeViewAttribute:")]
        void RemoveViewAttribute(string key);

        [Static]
        [Export("addViewAttributes:")]
        void AddViewAttributes(NSDictionary attributes);

        [Static]
        [Export("removeViewAttributes:")]
        void RemoveViewAttributes(NSArray keys);

        // Feature Operations
        [Static]
        [Export("startFeatureOperation:operationKey:context:")]
        void StartFeatureOperation(string name, [NullAllowed] string operationKey, NSDictionary context);

        [Static]
        [Export("succeedFeatureOperation:operationKey:context:")]
        void SucceedFeatureOperation(string name, [NullAllowed] string operationKey, NSDictionary context);

        [Static]
        [Export("failFeatureOperation:operationKey:reason:context:")]
        void FailFeatureOperation(string name, [NullAllowed] string operationKey, string reason, NSDictionary context);
    }
}
