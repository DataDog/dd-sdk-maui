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
    }
}
