/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.Text.Json;
using Datadog.Maui.Configuration;
using Xunit;

namespace Datadog.Maui.Tests;

public class FileBasedConfigurationTests
{
    private static string LoadFixture(string filename)
    {
        string path = Path.Combine("Fixtures", filename);
        return File.ReadAllText(path);
    }

    // --- Full config ---------------------------------------------------------

    [Fact]
    public void ParseJsonConfig_FullConfig_MapsAllFields()
    {
        string json = LoadFixture("full_config.json");
        DdSdkConfiguration config = FileBasedConfiguration.ParseJsonConfig(json);

        Assert.Equal("pub-test-full-config", config.ClientToken);
        Assert.Equal("staging", config.Environment);
        Assert.Equal(DatadogSite.Eu1, config.Site);
        Assert.Equal("my-maui-app", config.Service);
        Assert.Equal(TrackingConsent.Pending, config.TrackingConsent);
        Assert.Equal(SdkVerbosity.DEBUG, config.Verbosity);
        Assert.Equal(BatchSize.Large, config.BatchSize);
        Assert.Equal(UploadFrequency.Frequent, config.UploadFrequency);
        Assert.Equal(BatchProcessingLevel.High, config.BatchProcessingLevel);
        Assert.Equal("2.1.0", config.Version);
        Assert.Equal("-rc1", config.VersionSuffix);
        Assert.NotNull(config.ProxyConfiguration);
        Assert.Equal(ProxyType.Http, config.ProxyConfiguration.Type);
        Assert.Equal("proxy.example.com", config.ProxyConfiguration.Address);
        Assert.Equal(8080, config.ProxyConfiguration.Port);
        Assert.Equal("user", config.ProxyConfiguration.Username);
        Assert.Equal("pass", config.ProxyConfiguration.Password);
        Assert.True(config.NativeCrashReportEnabled);
    }

    [Fact]
    public void ParseJsonConfig_FullConfig_MapsAdditionalConfiguration()
    {
        string json = LoadFixture("full_config.json");
        DdSdkConfiguration config = FileBasedConfiguration.ParseJsonConfig(json);

        Assert.NotNull(config.AdditionalConfiguration);
        Assert.Equal(true, config.AdditionalConfiguration["_dd.needsClearTextHttp"]);
        Assert.Equal("custom_value", config.AdditionalConfiguration["_dd.custom_key"]);
    }

    // --- Minimal config ------------------------------------------------------

    [Fact]
    public void ParseJsonConfig_MinimalConfig_SetsRequiredFields()
    {
        string json = LoadFixture("minimal_config.json");
        DdSdkConfiguration config = FileBasedConfiguration.ParseJsonConfig(json);

        Assert.Equal("pub-test-minimal", config.ClientToken);
        Assert.Equal("prod", config.Environment);
    }

    [Fact]
    public void ParseJsonConfig_MinimalConfig_UsesDefaults()
    {
        string json = LoadFixture("minimal_config.json");
        DdSdkConfiguration config = FileBasedConfiguration.ParseJsonConfig(json);

        Assert.Equal(DatadogSite.Us1, config.Site);
        Assert.Equal(TrackingConsent.Granted, config.TrackingConsent);
        Assert.Null(config.Service);
        Assert.Null(config.Verbosity);
        Assert.Null(config.BatchSize);
        Assert.Null(config.UploadFrequency);
        Assert.Null(config.BatchProcessingLevel);
        Assert.Null(config.Version);
        Assert.Null(config.VersionSuffix);
        Assert.Null(config.AdditionalConfiguration);
        Assert.Null(config.ProxyConfiguration);
        Assert.False(config.NativeCrashReportEnabled);
    }

    // --- Malformed JSON ------------------------------------------------------

    [Fact]
    public void ParseJsonConfig_MalformedJson_ThrowsJsonException()
    {
        string json = LoadFixture("malformed_config.json");
        Assert.ThrowsAny<JsonException>(() => FileBasedConfiguration.ParseJsonConfig(json));
    }

    // --- Missing required fields ---------------------------------------------

    [Fact]
    public void ParseJsonConfig_MissingClientToken_ThrowsArgumentException()
    {
        string json = """{ "Environment": "prod" }""";
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => FileBasedConfiguration.ParseJsonConfig(json));
        Assert.Contains("ClientToken", ex.Message);
    }

    [Fact]
    public void ParseJsonConfig_MissingEnvironment_ThrowsArgumentException()
    {
        string json = """{ "ClientToken": "pub-xxx" }""";
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => FileBasedConfiguration.ParseJsonConfig(json));
        Assert.Contains("Environment", ex.Message);
    }

    [Fact]
    public void ParseJsonConfig_EmptyClientToken_ThrowsArgumentException()
    {
        string json = """{ "ClientToken": "", "Environment": "prod" }""";
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => FileBasedConfiguration.ParseJsonConfig(json));
        Assert.Contains("ClientToken", ex.Message);
    }

    [Fact]
    public void ParseJsonConfig_WhitespaceEnvironment_ThrowsArgumentException()
    {
        string json = """{ "ClientToken": "pub-xxx", "Environment": "   " }""";
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => FileBasedConfiguration.ParseJsonConfig(json));
        Assert.Contains("Environment", ex.Message);
    }

    // --- Invalid enum values -------------------------------------------------

    [Fact]
    public void ParseJsonConfig_InvalidSite_ThrowsArgumentException()
    {
        string json = """{ "ClientToken": "pub-xxx", "Environment": "prod", "Site": "InvalidSite" }""";
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => FileBasedConfiguration.ParseJsonConfig(json));
        Assert.Contains("Site", ex.Message);
    }

    [Fact]
    public void ParseJsonConfig_InvalidVerbosity_ThrowsArgumentException()
    {
        string json = """{ "ClientToken": "pub-xxx", "Environment": "prod", "Verbosity": "LOUD" }""";
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => FileBasedConfiguration.ParseJsonConfig(json));
        Assert.Contains("Verbosity", ex.Message);
    }

    // --- Proxy configuration -------------------------------------------------

    [Fact]
    public void ParseJsonConfig_WithProxyConfiguration_ParsesAllFields()
    {
        string json = """
        {
            "ClientToken": "pub-xxx",
            "Environment": "prod",
            "ProxyConfiguration": {
                "Type": "Http",
                "Address": "proxy.example.com",
                "Port": 8080,
                "Username": "user",
                "Password": "pass"
            }
        }
        """;
        DdSdkConfiguration config = FileBasedConfiguration.ParseJsonConfig(json);

        Assert.NotNull(config.ProxyConfiguration);
        Assert.Equal(ProxyType.Http, config.ProxyConfiguration.Type);
        Assert.Equal("proxy.example.com", config.ProxyConfiguration.Address);
        Assert.Equal(8080, config.ProxyConfiguration.Port);
        Assert.Equal("user", config.ProxyConfiguration.Username);
        Assert.Equal("pass", config.ProxyConfiguration.Password);
    }

    [Fact]
    public void ParseJsonConfig_WithProxyNoAuth_ParsesWithoutCredentials()
    {
        string json = """
        {
            "ClientToken": "pub-xxx",
            "Environment": "prod",
            "ProxyConfiguration": {
                "Type": "Socks",
                "Address": "socks.example.com",
                "Port": 1080
            }
        }
        """;
        DdSdkConfiguration config = FileBasedConfiguration.ParseJsonConfig(json);

        Assert.NotNull(config.ProxyConfiguration);
        Assert.Equal(ProxyType.Socks, config.ProxyConfiguration.Type);
        Assert.Equal("socks.example.com", config.ProxyConfiguration.Address);
        Assert.Equal(1080, config.ProxyConfiguration.Port);
        Assert.Null(config.ProxyConfiguration.Username);
        Assert.Null(config.ProxyConfiguration.Password);
    }

    [Fact]
    public void ParseJsonConfig_WithoutProxy_LeavesProxyNull()
    {
        string json = """{ "ClientToken": "pub-xxx", "Environment": "prod" }""";
        DdSdkConfiguration config = FileBasedConfiguration.ParseJsonConfig(json);

        Assert.Null(config.ProxyConfiguration);
    }

    [Fact]
    public void ParseJsonConfig_WithInvalidProxyType_ThrowsArgumentException()
    {
        string json = """
        {
            "ClientToken": "pub-xxx",
            "Environment": "prod",
            "ProxyConfiguration": {
                "Type": "FTP",
                "Address": "proxy.example.com",
                "Port": 8080
            }
        }
        """;
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => FileBasedConfiguration.ParseJsonConfig(json));
        Assert.Contains("Type", ex.Message);
    }

    [Fact]
    public void ParseJsonConfig_WithProxyMissingAddress_ThrowsArgumentException()
    {
        string json = """
        {
            "ClientToken": "pub-xxx",
            "Environment": "prod",
            "ProxyConfiguration": {
                "Type": "Http",
                "Port": 8080
            }
        }
        """;
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => FileBasedConfiguration.ParseJsonConfig(json));
        Assert.Contains("Address", ex.Message);
    }

    [Fact]
    public void ParseJsonConfig_WithProxyMissingPort_ThrowsArgumentException()
    {
        string json = """
        {
            "ClientToken": "pub-xxx",
            "Environment": "prod",
            "ProxyConfiguration": {
                "Type": "Http",
                "Address": "proxy.example.com"
            }
        }
        """;
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => FileBasedConfiguration.ParseJsonConfig(json));
        Assert.Contains("Port", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    [InlineData(70000)]
    public void ParseJsonConfig_WithProxyPortOutOfRange_ThrowsArgumentException(int port)
    {
        string json = $$"""
        {
            "ClientToken": "pub-xxx",
            "Environment": "prod",
            "ProxyConfiguration": {
                "Type": "Http",
                "Address": "proxy.example.com",
                "Port": {{port}}
            }
        }
        """;
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => FileBasedConfiguration.ParseJsonConfig(json));
        Assert.Contains("Port", exception.Message);
    }

}
