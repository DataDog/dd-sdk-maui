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

    // --- BatchSize ToString ---
    [Theory]
    [InlineData(BatchSize.Small, "small")]
    [InlineData(BatchSize.Medium, "medium")]
    [InlineData(BatchSize.Large, "large")]
    public void BatchSize_ToLowerInvariant_ReturnsCorrectString(BatchSize size, string expected)
    {
        Assert.Equal(expected, size.ToString().ToLowerInvariant());
    }

    // --- UploadFrequency ToString ---
    [Theory]
    [InlineData(UploadFrequency.Frequent, "frequent")]
    [InlineData(UploadFrequency.Average, "average")]
    [InlineData(UploadFrequency.Rare, "rare")]
    public void UploadFrequency_ToLowerInvariant_ReturnsCorrectString(UploadFrequency freq, string expected)
    {
        Assert.Equal(expected, freq.ToString().ToLowerInvariant());
    }

    // --- BatchProcessingLevel ToString ---
    [Theory]
    [InlineData(BatchProcessingLevel.Low, "low")]
    [InlineData(BatchProcessingLevel.Medium, "medium")]
    [InlineData(BatchProcessingLevel.High, "high")]
    public void BatchProcessingLevel_ToLowerInvariant_ReturnsCorrectString(BatchProcessingLevel level, string expected)
    {
        Assert.Equal(expected, level.ToString().ToLowerInvariant());
    }

    // --- SdkVerbosity ToString ---
    [Theory]
    [InlineData(SdkVerbosity.DEBUG, "debug")]
    [InlineData(SdkVerbosity.INFO, "info")]
    [InlineData(SdkVerbosity.WARN, "warn")]
    [InlineData(SdkVerbosity.ERROR, "error")]
    public void SdkVerbosity_ToLowerInvariant_ReturnsCorrectString(SdkVerbosity verbosity, string expected)
    {
        Assert.Equal(expected, verbosity.ToString().ToLowerInvariant());
    }
}
