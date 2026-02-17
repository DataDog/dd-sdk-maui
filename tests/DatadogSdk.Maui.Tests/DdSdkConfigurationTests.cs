using DatadogSdk.Maui.Configuration;
using Xunit;

namespace DatadogSdk.Maui.Tests;

public class DdSdkConfigurationTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var config = new DdSdkConfiguration
        {
            ClientToken = "test-token",
            Environment = "test"
        };

        Assert.Equal(TrackingConsent.Granted, config.TrackingConsent);
        Assert.Equal(DatadogSite.Us1, config.Site);
        Assert.Null(config.Service);
        Assert.Null(config.Verbosity);
        Assert.Null(config.BatchSize);
        Assert.Null(config.UploadFrequency);
        Assert.Null(config.BatchProcessingLevel);
        Assert.Null(config.Version);
        Assert.Null(config.VersionSuffix);
        Assert.Null(config.AdditionalConfiguration);
    }

    [Fact]
    public void FullConfiguration_AllFieldsSet()
    {
        var config = new DdSdkConfiguration
        {
            ClientToken = "pub123",
            Environment = "production",
            TrackingConsent = TrackingConsent.Pending,
            Service = "my-service",
            Site = DatadogSite.Eu1,
            Verbosity = SdkVerbosity.DEBUG,
            BatchSize = BatchSize.Large,
            UploadFrequency = UploadFrequency.Frequent,
            BatchProcessingLevel = BatchProcessingLevel.High,
            Version = "1.2.3",
            VersionSuffix = "-beta",
            AdditionalConfiguration = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 42 }
            }
        };

        Assert.Equal("pub123", config.ClientToken);
        Assert.Equal("production", config.Environment);
        Assert.Equal(TrackingConsent.Pending, config.TrackingConsent);
        Assert.Equal("my-service", config.Service);
        Assert.Equal(DatadogSite.Eu1, config.Site);
        Assert.Equal(SdkVerbosity.DEBUG, config.Verbosity);
        Assert.Equal(BatchSize.Large, config.BatchSize);
        Assert.Equal(UploadFrequency.Frequent, config.UploadFrequency);
        Assert.Equal(BatchProcessingLevel.High, config.BatchProcessingLevel);
        Assert.Equal("1.2.3", config.Version);
        Assert.Equal("-beta", config.VersionSuffix);
        Assert.Equal(2, config.AdditionalConfiguration!.Count);
    }
}
