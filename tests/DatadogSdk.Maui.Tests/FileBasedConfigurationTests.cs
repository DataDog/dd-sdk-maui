using System.Text.Json;
using DatadogSdk.Maui.Configuration;
using Xunit;

namespace DatadogSdk.Maui.Tests;

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

}
