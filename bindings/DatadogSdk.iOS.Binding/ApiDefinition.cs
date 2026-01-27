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
        [Export("initializeWithClientToken:environment:service:site:")]
        bool Initialize(string clientToken, string environment, string service, string site);

        // Overload without site parameter (uses default)
        [Static]
        [Export("initializeWithClientToken:environment:service:")]
        bool Initialize(string clientToken, string environment, string service);
    }

    // LogsWrapper - Logging functionality
    [BaseType(typeof(NSObject))]
    interface LogsWrapper
    {
        [Static]
        [Export("enableLogs")]
        void EnableLogs();

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
        void LogWithAttributes(string level, string message, NSDictionary<NSString, NSString> attributes);
    }
}
