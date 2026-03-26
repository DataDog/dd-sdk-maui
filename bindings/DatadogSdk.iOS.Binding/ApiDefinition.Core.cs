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
    }
}
