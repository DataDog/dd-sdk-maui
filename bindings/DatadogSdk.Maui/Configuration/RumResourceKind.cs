namespace DatadogSdk.Maui.Configuration
{
    /// <summary>
    /// Type of resource tracked by RUM.
    /// </summary>
    public enum RumResourceKind
    {
        Xhr,
        Native,
        Fetch,
        Document,
        Beacon,
        Image,
        Font,
        Css,
        Media,
        Js,
        Other
    }
}
