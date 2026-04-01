namespace DatadogSdk.Maui.Configuration
{
    /// <summary>
    /// Represents a RUM error event that can be modified or dropped by an ErrorEventMapper.
    /// </summary>
    public class DdRumErrorEvent
    {
        /// <summary>
        /// Error message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Error source (e.g., "source", "network", "console", "webview", "custom").
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Error stacktrace string.
        /// </summary>
        public string Stacktrace { get; set; }

        /// <summary>
        /// Additional context attributes.
        /// </summary>
        public Dictionary<string, object> Context { get; set; }

        /// <summary>
        /// Timestamp in milliseconds since epoch.
        /// </summary>
        public long TimestampMs { get; set; }

        internal DdRumErrorEvent(string message, string source, string stacktrace,
                                  Dictionary<string, object> context, long timestampMs)
        {
            Message = message;
            Source = source;
            Stacktrace = stacktrace;
            Context = context;
            TimestampMs = timestampMs;
        }
    }
}
