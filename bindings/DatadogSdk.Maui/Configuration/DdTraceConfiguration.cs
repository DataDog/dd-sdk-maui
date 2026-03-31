namespace DatadogSdk.Maui.Configuration
{
    /// <summary>
    /// Configuration for the Datadog Trace module.
    /// </summary>
    public class DdTraceConfiguration
    {
        /// <summary>
        /// Custom server endpoint where Traces are sent.
        /// If not provided, traces are sent to the standard Datadog endpoint based on your site configuration.
        /// </summary>
        /// <example>
        /// https://custom-trace-endpoint.example.com/v1/input
        /// </example>
        public string? CustomEndpoint { get; set; }
    }
}
