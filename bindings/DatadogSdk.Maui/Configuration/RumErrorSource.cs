namespace DatadogSdk.Maui.Configuration
{
    /// <summary>
    /// Source of an error tracked by RUM.
    /// </summary>
    public enum RumErrorSource
    {
        Network,
        Source,
        Console,
        Logger,
        Agent,
        Webview,
        Report,
        Custom
    }
}
