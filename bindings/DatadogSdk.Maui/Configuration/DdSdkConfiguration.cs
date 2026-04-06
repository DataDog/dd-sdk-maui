using System.Collections.Generic;

namespace DatadogSdk.Maui.Configuration
{
    public class DdSdkConfiguration
    {
        // --- Required ---
        public required string ClientToken { get; set; }
        public required string Environment { get; set; }
        public TrackingConsent TrackingConsent { get; set; } = TrackingConsent.Granted;

        // --- Optional ---
        public Dictionary<string, object>? AdditionalConfiguration { get; set; }
        public BatchSize? BatchSize { get; set; }
        public BatchProcessingLevel? BatchProcessingLevel { get; set; }
        public string? Service { get; set; }
        public DatadogSite Site { get; set; } = DatadogSite.Us1;
        public UploadFrequency? UploadFrequency { get; set; }
        public string? Version { get; set; }
        public string? VersionSuffix { get; set; }
        public SdkVerbosity? Verbosity { get; set; }
        public ProxyConfiguration? ProxyConfiguration { get; set; }
        public List<DdFirstPartyHost>? FirstPartyHosts { get; set; }
    }
}
