using System;
using Foundation;
using ObjCRuntime;

namespace DatadogSdk.iOS.Binding
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
