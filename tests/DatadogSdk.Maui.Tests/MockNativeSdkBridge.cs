/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

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
    public bool NativeCrashReportEnabled { get; private set; }
    public Dictionary<string, object>? AdditionalConfiguration { get; private set; }
    public Dictionary<string, object>? ProxyConfiguration { get; private set; }
    public Dictionary<string, object>? FirstPartyHosts { get; private set; }
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
        Dictionary<string, object>? proxyConfiguration,
        Dictionary<string, object>? firstPartyHosts,
        bool nativeCrashReportEnabled,
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
        NativeCrashReportEnabled = nativeCrashReportEnabled;
        AdditionalConfiguration = additionalConfiguration;
        ProxyConfiguration = proxyConfiguration;
        FirstPartyHosts = firstPartyHosts;
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

    // User Info
    public List<(string Id, string? Name, string? Email, Dictionary<string, object> ExtraInfo)> SetUserInfoCalls { get; } = new();
    public List<Dictionary<string, object>> AddUserExtraInfoCalls { get; } = new();
    public int ClearUserInfoCallCount { get; private set; }

    public void SetUserInfo(string id, string? name, string? email, Dictionary<string, object> extraInfo)
    {
        SetUserInfoCalls.Add((id, name, email, extraInfo));
    }

    public void AddUserExtraInfo(Dictionary<string, object> extraInfo)
    {
        AddUserExtraInfoCalls.Add(extraInfo);
    }

    public void ClearUserInfo()
    {
        ClearUserInfoCallCount++;
    }

    // Account Info
    public List<(string Id, string? Name, Dictionary<string, object> ExtraInfo)> SetAccountInfoCalls { get; } = new();
    public List<Dictionary<string, object>> AddAccountExtraInfoCalls { get; } = new();
    public int ClearAccountInfoCallCount { get; private set; }

    public void SetAccountInfo(string id, string? name, Dictionary<string, object> extraInfo)
    {
        SetAccountInfoCalls.Add((id, name, extraInfo));
    }

    public void AddAccountExtraInfo(Dictionary<string, object> extraInfo)
    {
        AddAccountExtraInfoCalls.Add(extraInfo);
    }

    public void ClearAccountInfo()
    {
        ClearAccountInfoCallCount++;
    }
}
