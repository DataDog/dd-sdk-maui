namespace DatadogSdk.Maui.Configuration
{
    /// <summary>
    /// Configuration for the Datadog Logs module.
    /// </summary>
    public class DdLogsConfiguration
    {
        /// <summary>
        /// Custom server endpoint where Logs are sent.
        /// If not provided, logs are sent to the standard Datadog endpoint based on your site configuration.
        /// </summary>
        /// <example>
        /// https://custom-logs-endpoint.example.com/v1/input
        /// </example>
        public string? CustomEndpoint { get; set; }
    }
}
