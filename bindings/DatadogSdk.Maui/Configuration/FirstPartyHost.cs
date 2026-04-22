namespace DatadogSdk.Maui.Configuration
{
    /// <summary>
    /// Defines a first-party host for distributed tracing.
    /// Requests to matching hosts will have tracing headers injected.
    /// </summary>
    public class FirstPartyHost
    {
        /// <summary>
        /// The host or domain to match (e.g., "api.example.com").
        /// </summary>
        public required string Match { get; set; }

        /// <summary>
        /// The types of tracing headers to inject for requests to this host.
        /// </summary>
        public required List<TracingHeaderType> HeaderTypes { get; set; }
    }
}
