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
        [Export("initializeWithClientToken:environment:service:site:verbosity:")]
        bool Initialize(string clientToken, string environment, string service, string site, string verbosity);
    }
}
