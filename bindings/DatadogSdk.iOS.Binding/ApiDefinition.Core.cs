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
        [Export("initializeWithClientToken:environment:service:site:verbosity:trackingConsent:batchSize:uploadFrequency:batchProcessingLevel:additionalConfiguration:")]
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
    }
}
