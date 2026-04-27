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
