/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using Datadog.Maui.Configuration;
using Xunit;

namespace Datadog.Maui.Tests;

[Collection("InternalLog")]
public class DdSdkConfigurationTests : IDisposable
{
    private readonly MockNativeSdkBridge bridge = new();

    public DdSdkConfigurationTests()
    {
        DdSdk.testBridge = bridge;
    }

    public void Dispose()
    {
        DdSdk.testBridge = null;
        InternalLog.Verbosity = null;
        GC.SuppressFinalize(this);
    }

    // --- Full config ---------------------------------------------------------

    [Fact]
    public void Initialize_PassesAllConfigToNative()
    {
        DdSdkConfiguration config = new()
        {
            ClientToken = "pub-token-123",
            Environment = "staging",
            Service = "my-service",
            Site = DatadogSite.Eu1,
            TrackingConsent = TrackingConsent.Pending,
            Verbosity = SdkVerbosity.DEBUG,
            BatchSize = BatchSize.Large,
            UploadFrequency = UploadFrequency.Frequent,
            BatchProcessingLevel = BatchProcessingLevel.High,
            AdditionalConfiguration = new Dictionary<string, object> { { "_dd.needsClearTextHttp", true } }
        };

        bool result = DdSdk.Initialize(config);

        Assert.True(result);
        Assert.Equal(1, bridge.CallCount);
        Assert.Equal("pub-token-123", bridge.ClientToken);
        Assert.Equal("staging", bridge.Environment);
        Assert.Equal("my-service", bridge.Service);
        Assert.Equal("eu1", bridge.Site);
        Assert.Equal("pending", bridge.TrackingConsent);
        Assert.Equal("debug", bridge.Verbosity);
        Assert.Equal("large", bridge.BatchSize);
        Assert.Equal("frequent", bridge.UploadFrequency);
        Assert.Equal("high", bridge.BatchProcessingLevel);
        Assert.NotNull(bridge.AdditionalConfiguration);
        Assert.Equal(true, bridge.AdditionalConfiguration["_dd.needsClearTextHttp"]);
    }

    // --- Native failure ------------------------------------------------------

    [Fact]
    public void Initialize_ReturnsFalseWhenNativeFails()
    {
        bridge.ReturnValue = false;

        bool result = DdSdk.Initialize(new DdSdkConfiguration
        {
            ClientToken = "pub-token",
            Environment = "test"
        });

        Assert.False(result);
    }

    // --- JSON-based config ---------------------------------------------------

    [Fact]
    public void Initialize_WithConfigFromJson_PassesAllValuesToNative()
    {
        string json = File.ReadAllText(Path.Combine("Fixtures", "full_config.json"));
        DdSdkConfiguration config = FileBasedConfiguration.ParseJsonConfig(json);

        bool result = DdSdk.Initialize(config);

        Assert.True(result);
        Assert.Equal("pub-test-full-config", bridge.ClientToken);
        Assert.Equal("staging", bridge.Environment);
        Assert.Equal("my-maui-app", bridge.Service);
        Assert.Equal("eu1", bridge.Site);
        Assert.Equal("pending", bridge.TrackingConsent);
        Assert.Equal("debug", bridge.Verbosity);
        Assert.Equal("large", bridge.BatchSize);
        Assert.Equal("frequent", bridge.UploadFrequency);
        Assert.Equal("high", bridge.BatchProcessingLevel);
        Assert.NotNull(bridge.AdditionalConfiguration);
        Assert.Equal("2.1.0", bridge.AdditionalConfiguration["_dd.version"]);
        Assert.Equal("-rc1", bridge.AdditionalConfiguration["_dd.version_suffix"]);
        Assert.Equal(true, bridge.AdditionalConfiguration["_dd.needsClearTextHttp"]);
        Assert.Equal("custom_value", bridge.AdditionalConfiguration["_dd.custom_key"]);
        Assert.NotNull(bridge.FirstPartyHosts);
        Assert.Equal(2, bridge.FirstPartyHosts!.Count);
        Assert.Equal("datadog,tracecontext", bridge.FirstPartyHosts["api.example.com"]);
        Assert.Equal("b3", bridge.FirstPartyHosts["cdn.example.com"]);
    }

    // --- Site ----------------------------------------------------------------

    [Theory]
    [InlineData(DatadogSite.Us1, "us1")]
    [InlineData(DatadogSite.Us3, "us3")]
    [InlineData(DatadogSite.Us5, "us5")]
    [InlineData(DatadogSite.Eu1, "eu1")]
    [InlineData(DatadogSite.Ap1, "ap1")]
    [InlineData(DatadogSite.Ap2, "ap2")]
    [InlineData(DatadogSite.Us1Fed, "us1_fed")]
    public void Initialize_WithSite_PassesConvertedValueToNative(DatadogSite site, string expected)
    {
        DdSdk.Initialize(new DdSdkConfiguration
        {
            ClientToken = "pub-token",
            Environment = "test",
            Site = site
        });

        Assert.Equal(expected, bridge.Site);
    }

    // --- Tracking consent ----------------------------------------------------

    [Theory]
    [InlineData(TrackingConsent.Granted, "granted")]
    [InlineData(TrackingConsent.NotGranted, "not_granted")]
    [InlineData(TrackingConsent.Pending, "pending")]
    public void Initialize_WithExplicitTrackingConsent(TrackingConsent consent, string expected)
    {
        DdSdk.Initialize(new DdSdkConfiguration
        {
            ClientToken = "pub-token",
            Environment = "test",
            TrackingConsent = consent
        });

        Assert.Equal(expected, bridge.TrackingConsent);
    }

    // --- Multiple initializations --------------------------------------------

    [Fact]
    public void Initialize_CalledMultipleTimes()
    {
        DdSdkConfiguration config = new()
        {
            ClientToken = "pub-token",
            Environment = "test"
        };

        bool result = DdSdk.Initialize(config);
        Assert.True(result);
        bridge.ReturnValue = false;
        result = DdSdk.Initialize(config);
        Assert.False(result);
        result = DdSdk.Initialize(config);
        Assert.False(result);
    }

    // --- Version -------------------------------------------------------------

    [Fact]
    public void Initialize_WithVersion_AddsVersionToAdditionalConfig()
    {
        DdSdk.Initialize(new DdSdkConfiguration
        {
            ClientToken = "pub-token",
            Environment = "test",
            Version = "3.0.0"
        });

        Assert.NotNull(bridge.AdditionalConfiguration);
        Assert.Equal("3.0.0", bridge.AdditionalConfiguration["_dd.version"]);
    }

    // --- Version suffix ------------------------------------------------------

    [Fact]
    public void Initialize_WithVersionSuffix_AddsSuffixToAdditionalConfig()
    {
        DdSdk.Initialize(new DdSdkConfiguration
        {
            ClientToken = "pub-token",
            Environment = "test",
            VersionSuffix = "-beta"
        });

        Assert.NotNull(bridge.AdditionalConfiguration);
        Assert.Equal("-beta", bridge.AdditionalConfiguration["_dd.version_suffix"]);
    }

    // --- Version and suffix --------------------------------------------------

    [Fact]
    public void Initialize_WithVersionAndSuffix_AddsBothToAdditionalConfig()
    {
        DdSdk.Initialize(new DdSdkConfiguration
        {
            ClientToken = "pub-token",
            Environment = "test",
            Version = "2.0.0",
            VersionSuffix = "-rc1"
        });

        Assert.NotNull(bridge.AdditionalConfiguration);
        Assert.Equal("2.0.0", bridge.AdditionalConfiguration["_dd.version"]);
        Assert.Equal("-rc1", bridge.AdditionalConfiguration["_dd.version_suffix"]);
    }

    // --- Service name --------------------------------------------------------

    [Fact]
    public void Initialize_WithServiceName_PassesServiceToNative()
    {
        DdSdk.Initialize(new DdSdkConfiguration
        {
            ClientToken = "pub-token",
            Environment = "test",
            Service = "custom-service"
        });

        Assert.Equal("custom-service", bridge.Service);
    }

    [Fact]
    public void Initialize_WithoutServiceName_PassesNullToNative()
    {
        DdSdk.Initialize(new DdSdkConfiguration
        {
            ClientToken = "pub-token",
            Environment = "test"
        });

        Assert.Null(bridge.Service);
    }

    // --- Verbosity -----------------------------------------------------------

    [Theory]
    [InlineData(SdkVerbosity.DEBUG, "debug")]
    [InlineData(SdkVerbosity.INFO, "info")]
    [InlineData(SdkVerbosity.WARN, "warn")]
    [InlineData(SdkVerbosity.ERROR, "error")]
    public void Initialize_WithVerbosity_PassesLowercaseToNative(SdkVerbosity verbosity, string expected)
    {
        DdSdk.Initialize(new DdSdkConfiguration
        {
            ClientToken = "pub-token",
            Environment = "test",
            Verbosity = verbosity
        });

        Assert.Equal(expected, bridge.Verbosity);
    }

    [Fact]
    public void Initialize_WithoutVerbosity_DefaultsToError()
    {
        DdSdk.Initialize(new DdSdkConfiguration
        {
            ClientToken = "pub-token",
            Environment = "test"
        });

        Assert.Equal("error", bridge.Verbosity);
    }

    // --- First-party hosts ---------------------------------------------------

    [Fact]
    public void Initialize_WithFirstPartyHosts_PassesDictionaryToNative()
    {
        DdSdk.Initialize(new DdSdkConfiguration
        {
            ClientToken = "pub-token",
            Environment = "test",
            FirstPartyHosts = new List<FirstPartyHost>
            {
                new() { Match = "api.example.com", HeaderTypes = new List<TracingHeaderType> { TracingHeaderType.Datadog, TracingHeaderType.TraceContext } },
                new() { Match = "cdn.example.com", HeaderTypes = new List<TracingHeaderType> { TracingHeaderType.B3 } }
            }
        });

        Assert.NotNull(bridge.FirstPartyHosts);
        Assert.Equal(2, bridge.FirstPartyHosts!.Count);
        Assert.Equal("datadog,tracecontext", bridge.FirstPartyHosts["api.example.com"]);
        Assert.Equal("b3", bridge.FirstPartyHosts["cdn.example.com"]);
    }

    [Fact]
    public void Initialize_WithoutFirstPartyHosts_PassesNullToNative()
    {
        DdSdk.Initialize(new DdSdkConfiguration
        {
            ClientToken = "pub-token",
            Environment = "test"
        });

        Assert.Null(bridge.FirstPartyHosts);
    }

    [Fact]
    public void Initialize_WithEmptyFirstPartyHosts_PassesNullToNative()
    {
        DdSdk.Initialize(new DdSdkConfiguration
        {
            ClientToken = "pub-token",
            Environment = "test",
            FirstPartyHosts = new List<FirstPartyHost>()
        });

        Assert.Null(bridge.FirstPartyHosts);
    }

    // --- NativeCrashReportEnabled --------------------------------------------

    [Fact]
    public void Initialize_WithNativeCrashReportEnabled_PassesTrueToNative()
    {
        DdSdk.Initialize(new DdSdkConfiguration
        {
            ClientToken = "pub-token",
            Environment = "test",
            NativeCrashReportEnabled = true
        });

        Assert.True(bridge.NativeCrashReportEnabled);
    }

    [Fact]
    public void Initialize_WithoutNativeCrashReportEnabled_DefaultsFalse()
    {
        DdSdk.Initialize(new DdSdkConfiguration
        {
            ClientToken = "pub-token",
            Environment = "test"
        });

        Assert.False(bridge.NativeCrashReportEnabled);
    }

    // --- SetTrackingConsent --------------------------------------------------

    [Theory]
    [InlineData(TrackingConsent.Granted, "granted")]
    [InlineData(TrackingConsent.NotGranted, "not_granted")]
    [InlineData(TrackingConsent.Pending, "pending")]
    public void SetTrackingConsent_PassesConvertedValueToNative(TrackingConsent consent, string expected)
    {
        DdSdk.SetTrackingConsent(consent);

        Assert.Equal(1, bridge.SetTrackingConsentCallCount);
        Assert.Equal(expected, bridge.LastSetTrackingConsent);
    }

    [Fact]
    public void SetTrackingConsent_CanBeCalledMultipleTimes()
    {
        DdSdk.SetTrackingConsent(TrackingConsent.Pending);
        DdSdk.SetTrackingConsent(TrackingConsent.Granted);

        Assert.Equal(2, bridge.SetTrackingConsentCallCount);
        Assert.Equal("granted", bridge.LastSetTrackingConsent);
    }

    // --- Proxy configuration -------------------------------------------------

    [Fact]
    public void Initialize_WithProxyConfiguration_PassesProxyDictToNative()
    {
        DdSdk.Initialize(new DdSdkConfiguration
        {
            ClientToken = "pub-token",
            Environment = "test",
            ProxyConfiguration = new ProxyConfiguration
            {
                Type = ProxyType.Http,
                Address = "1.2.3.4",
                Port = 8080,
                Username = "user",
                Password = "pass"
            }
        });

        Assert.NotNull(bridge.ProxyConfiguration);
        Assert.Equal("http", bridge.ProxyConfiguration["type"]);
        Assert.Equal("1.2.3.4", bridge.ProxyConfiguration["address"]);
        Assert.Equal(8080, bridge.ProxyConfiguration["port"]);
        Assert.Equal("user", bridge.ProxyConfiguration["username"]);
        Assert.Equal("pass", bridge.ProxyConfiguration["password"]);
    }

    [Fact]
    public void Initialize_WithoutProxyConfiguration_PassesNullProxyToNative()
    {
        DdSdk.Initialize(new DdSdkConfiguration
        {
            ClientToken = "pub-token",
            Environment = "test"
        });

        Assert.Null(bridge.ProxyConfiguration);
    }
}
