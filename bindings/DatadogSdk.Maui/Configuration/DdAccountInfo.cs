namespace DatadogSdk.Maui.Configuration
{
    /// <summary>
    /// Represents account information attached to all Datadog events.
    /// </summary>
    public class DdAccountInfo
    {
        /// <summary>
        /// Unique account identifier. Required.
        /// </summary>
        public required string Id { get; set; }

        /// <summary>
        /// Account display name. Optional.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Additional custom attributes for the account. Optional.
        /// </summary>
        public Dictionary<string, object>? ExtraInfo { get; set; }
    }
}
