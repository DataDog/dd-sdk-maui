namespace DatadogSdk.Maui.Configuration
{
    public class ProxyConfiguration
    {
        public required ProxyType Type { get; set; }
        public required string Address { get; set; }
        public required int Port { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
