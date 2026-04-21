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
    public List<(string Key, object Value)> AddAttributeCalls { get; } = new();
    public List<Dictionary<string, object>> AddAttributesCalls { get; } = new();
    public List<string> RemoveAttributeCalls { get; } = new();
    public List<List<string>> RemoveAttributesCalls { get; } = new();

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

    public void AddAttribute(string key, object value)
    {
        AddAttributeCalls.Add((key, value));
    }

    public void AddAttributes(Dictionary<string, object> attributes)
    {
        AddAttributesCalls.Add(attributes);
    }

    public void RemoveAttribute(string key)
    {
        RemoveAttributeCalls.Add(key);
    }

    public void RemoveAttributes(List<string> keys)
    {
        RemoveAttributesCalls.Add(keys);
    }
}
