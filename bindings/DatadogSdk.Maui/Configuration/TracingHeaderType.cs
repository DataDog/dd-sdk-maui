namespace DatadogSdk.Maui.Configuration
{
    /// <summary>
    /// Type of tracing header to inject for distributed tracing.
    /// </summary>
    public enum TracingHeaderType
    {
        Datadog,
        B3,
        B3Multi,
        TraceContext
    }
}
