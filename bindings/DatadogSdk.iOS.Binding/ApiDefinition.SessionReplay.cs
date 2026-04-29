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
    // DdSessionReplay - Session Replay functionality
    [BaseType(typeof(NSObject))]
    interface DdSessionReplay
    {
        [Static]
        [Export("enableSessionReplay:textAndInputPrivacy:imagePrivacy:touchPrivacy:customEndpoint:")]
        void EnableSessionReplay(double replaySampleRate, string textAndInputPrivacy,
                                 string imagePrivacy, string touchPrivacy,
                                 [NullAllowed] string customEndpoint);
    }
}
