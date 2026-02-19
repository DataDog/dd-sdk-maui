namespace DatadogSdk.Maui
{
    public class DdSdkConfiguration
    {
        public required string ClientToken { get; set; }
        public required string Environment { get; set; }
        public required string Service { get; set; }
        public string Site { get; set; } = "us1";
        public SdkVerbosity? Verbosity { get; set; } = null;
    }
}
