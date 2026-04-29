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
    // DatadogWrapper - SDK initialization
    [BaseType(typeof(NSObject))]
    interface DatadogWrapper
    {
        [Static]
        [Export("initializeWithClientToken:environment:service:site:verbosity:trackingConsent:batchSize:uploadFrequency:batchProcessingLevel:proxyConfiguration:firstPartyHosts:nativeCrashReportEnabled:additionalConfiguration:")]
        bool Initialize(
            string clientToken,
            string environment,
            [NullAllowed] string service,
            string site,
            string verbosity,
            string trackingConsent,
            [NullAllowed] string batchSize,
            [NullAllowed] string uploadFrequency,
            [NullAllowed] string batchProcessingLevel,
            [NullAllowed] NSDictionary proxyConfiguration,
            [NullAllowed] NSDictionary firstPartyHosts,
            bool nativeCrashReportEnabled,
            [NullAllowed] NSDictionary additionalConfiguration
        );

        [Static]
        [Export("setTrackingConsent:")]
        void SetTrackingConsent(string consent);

        [Static]
        [Export("addAttribute:value:")]
        void AddAttribute(string key, NSObject value);

        [Static]
        [Export("addAttributes:")]
        void AddAttributes(NSDictionary attributes);

        [Static]
        [Export("removeAttribute:")]
        void RemoveAttribute(string key);

        [Static]
        [Export("removeAttributes:")]
        void RemoveAttributes(NSArray keys);

        // User Info
        [Static]
        [Export("setUserInfo:name:email:extraInfo:")]
        void SetUserInfo(string id, [NullAllowed] string name, [NullAllowed] string email, NSDictionary extraInfo);

        [Static]
        [Export("addUserExtraInfo:")]
        void AddUserExtraInfo(NSDictionary extraInfo);

        [Static]
        [Export("clearUserInfo")]
        void ClearUserInfo();

        // Account Info
        [Static]
        [Export("setAccountInfo:name:extraInfo:")]
        void SetAccountInfo(string id, [NullAllowed] string name, NSDictionary extraInfo);

        [Static]
        [Export("addAccountExtraInfo:")]
        void AddAccountExtraInfo(NSDictionary extraInfo);

        [Static]
        [Export("clearAccountInfo")]
        void ClearAccountInfo();

        // Flush
        [Static]
        [Export("flush")]
        void Flush();
    }
}
