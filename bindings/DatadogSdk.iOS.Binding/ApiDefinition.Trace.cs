using System;
using Foundation;
using ObjCRuntime;

namespace DatadogSdk.iOS.Binding
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
