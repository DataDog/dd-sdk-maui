namespace DatadogSdk.Maui.Tests;

internal class MockNativeSdkBridge : DdSdk.INativeBridge
{
    public int CallCount { get; private set; }
    public string? ClientToken { get; private set; }
    public string? Environment { get; private set; }
    public string? Service { get; private set; }
    public string? Site { get; private set; }
    public string? Verbosity { get; private set; }
    public string? TrackingConsent { get; private set; }
    public string? BatchSize { get; private set; }
    public string? UploadFrequency { get; private set; }
    public string? BatchProcessingLevel { get; private set; }
    public Dictionary<string, object>? AdditionalConfiguration { get; private set; }
    public bool ReturnValue { get; set; } = true;
    public int SetTrackingConsentCallCount { get; private set; }
    public string? LastSetTrackingConsent { get; private set; }

    public bool Initialize(
        string clientToken, string environment, string? service,
        string site, string verbosity, string trackingConsent,
        string? batchSize, string? uploadFrequency, string? batchProcessingLevel,
        Dictionary<string, object>? additionalConfiguration)
    {
        CallCount++;
        ClientToken = clientToken;
        Environment = environment;
        Service = service;
        Site = site;
        Verbosity = verbosity;
        TrackingConsent = trackingConsent;
        BatchSize = batchSize;
        UploadFrequency = uploadFrequency;
        BatchProcessingLevel = batchProcessingLevel;
        AdditionalConfiguration = additionalConfiguration;
        return ReturnValue;
    }

    public void SetTrackingConsent(string consent)
    {
        SetTrackingConsentCallCount++;
        LastSetTrackingConsent = consent;
    }
}
