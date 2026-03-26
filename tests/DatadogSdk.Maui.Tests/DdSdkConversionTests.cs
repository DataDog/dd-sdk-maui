using DatadogSdk.Maui.Configuration;
using Xunit;

namespace DatadogSdk.Maui.Tests;

public class DdSdkConversionTests
{
    // --- DatadogSite conversion ---
    [Theory]
    [InlineData(DatadogSite.Us1, "us1")]
    [InlineData(DatadogSite.Us3, "us3")]
    [InlineData(DatadogSite.Us5, "us5")]
    [InlineData(DatadogSite.Eu1, "eu1")]
    [InlineData(DatadogSite.Ap1, "ap1")]
    [InlineData(DatadogSite.Ap2, "ap2")]
    [InlineData(DatadogSite.Us1Fed, "us1_fed")]
    public void ConvertSite_ReturnsCorrectString(DatadogSite site, string expected)
    {
        Assert.Equal(expected, DdSdk.ConvertSite(site));
    }

    // --- TrackingConsent conversion ---
    [Theory]
    [InlineData(TrackingConsent.Granted, "granted")]
    [InlineData(TrackingConsent.NotGranted, "not_granted")]
    [InlineData(TrackingConsent.Pending, "pending")]
    public void ConvertTrackingConsent_ReturnsCorrectString(TrackingConsent consent, string expected)
    {
        Assert.Equal(expected, DdSdk.ConvertTrackingConsent(consent));
    }

    // --- BuildAdditionalConfiguration ---

    [Fact]
    public void BuildAdditionalConfiguration_AllNull_ReturnsNull()
    {
        Dictionary<string, object>? result = DdSdk.BuildAdditionalConfiguration(null, null, null);
        Assert.Null(result);
    }

    [Fact]
    public void BuildAdditionalConfiguration_VersionOnly_SetsVersionKey()
    {
        Dictionary<string, object>? result = DdSdk.BuildAdditionalConfiguration(null, "1.2.3", null);
        Assert.NotNull(result);
        Assert.Equal("1.2.3", result["_dd.version"]);
        Assert.False(result.ContainsKey("_dd.version_suffix"));
    }

    [Fact]
    public void BuildAdditionalConfiguration_VersionSuffixOnly_SetsSuffixKey()
    {
        Dictionary<string, object>? result = DdSdk.BuildAdditionalConfiguration(null, null, "-beta");
        Assert.NotNull(result);
        Assert.Equal("-beta", result["_dd.version_suffix"]);
        Assert.False(result.ContainsKey("_dd.version"));
    }

    [Fact]
    public void BuildAdditionalConfiguration_VersionAndSuffix_SetsBothKeys()
    {
        Dictionary<string, object>? result = DdSdk.BuildAdditionalConfiguration(null, "2.0.0", "-rc1");
        Assert.NotNull(result);
        Assert.Equal("2.0.0", result["_dd.version"]);
        Assert.Equal("-rc1", result["_dd.version_suffix"]);
    }

    [Fact]
    public void BuildAdditionalConfiguration_ExistingConfigPlusVersion_MergesWithoutLoss()
    {
        Dictionary<string, object> extra = new() { { "custom_key", "custom_value" } };
        Dictionary<string, object>? result = DdSdk.BuildAdditionalConfiguration(extra, "3.0.0", null);
        Assert.NotNull(result);
        Assert.Equal("3.0.0", result["_dd.version"]);
        Assert.Equal("custom_value", result["custom_key"]);
    }

    [Fact]
    public void BuildAdditionalConfiguration_DoesNotMutateSourceDictionary()
    {
        Dictionary<string, object> original = new() { { "key", "value" } };
        DdSdk.BuildAdditionalConfiguration(original, "1.0.0", null);
        Assert.False(original.ContainsKey("_dd.version"));
    }

    [Fact]
    public void BuildAdditionalConfiguration_InternalKeyPassedViaAdditionalConfig_FlowsThrough()
    {
        Dictionary<string, object> extra = new() { { "_dd.needsClearTextHttp", true } };
        Dictionary<string, object>? result = DdSdk.BuildAdditionalConfiguration(extra, null, null);
        Assert.NotNull(result);
        Assert.Equal(true, result["_dd.needsClearTextHttp"]);
    }
}
