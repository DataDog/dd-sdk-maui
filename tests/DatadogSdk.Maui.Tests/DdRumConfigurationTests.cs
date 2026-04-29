/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

using System.Text.Json;
using DatadogSdk.Maui.Configuration;
using Xunit;

namespace DatadogSdk.Maui.Tests;

public class DdRumConfigurationTests
{
    // ── Defaults ────────────────────────────────────────────────

    [Fact]
    public void DdRumConfiguration_DefaultValues_MatchSpec()
    {
        var config = new DdRumConfiguration { ApplicationId = "app-id" };

        Assert.Equal(100.0, config.SessionSampleRate);
        Assert.Equal(20.0, config.TelemetrySampleRate);
        Assert.Equal(20.0, config.ResourceTraceSampleRate);
        Assert.True(config.TrackFrustrations);
        Assert.False(config.TrackBackgroundEvents);
        Assert.False(config.NativeViewTracking);
        Assert.False(config.NativeInteractionTracking);
        Assert.False(config.TrackMemoryWarnings);
        Assert.Equal(200.0, config.NativeLongTaskThresholdMs);
        Assert.Null(config.InitialResourceThreshold);
        Assert.Null(config.AppHangThreshold);
        Assert.Null(config.TrackNonFatalAnrs);
        Assert.Null(config.TrackWatchdogTerminations);
        Assert.Equal(VitalsUpdateFrequency.Average, config.VitalsUpdateFrequency);
        Assert.Null(config.CustomEndpoint);
        Assert.Null(config.ErrorEventMapper);
        Assert.Null(config.ResourceEventMapper);
        Assert.Null(config.ActionEventMapper);
    }

    // ── Dictionary serialization ────────────────────────────────

    [Fact]
    public void ToDictionary_WithDefaults_ContainsAllRequiredKeys()
    {
        var config = new DdRumConfiguration { ApplicationId = "rum-app-id" };
        var dict = config.ToDictionary();

        Assert.Equal("rum-app-id", dict["applicationId"]);
        Assert.Equal(100.0, dict["sessionSampleRate"]);
        Assert.Equal(20.0, dict["telemetrySampleRate"]);
        Assert.Equal(20.0, dict["resourceTraceSampleRate"]);
        Assert.Equal(true, dict["trackFrustrations"]);
        Assert.Equal(false, dict["trackBackgroundEvents"]);
        Assert.Equal(false, dict["nativeViewTracking"]);
        Assert.Equal(false, dict["nativeInteractionTracking"]);
        Assert.Equal(false, dict["trackMemoryWarnings"]);
        Assert.Equal(200.0, dict["nativeLongTaskThresholdMs"]);
        Assert.Equal("average", dict["vitalsUpdateFrequency"]);
    }

    [Fact]
    public void ToDictionary_WithDefaults_OmitsNullOptionals()
    {
        var config = new DdRumConfiguration { ApplicationId = "app-id" };
        var dict = config.ToDictionary();

        Assert.False(dict.ContainsKey("initialResourceThreshold"));
        Assert.False(dict.ContainsKey("appHangThreshold"));
        Assert.False(dict.ContainsKey("trackNonFatalAnrs"));
        Assert.False(dict.ContainsKey("trackWatchdogTerminations"));
        Assert.False(dict.ContainsKey("customEndpoint"));
    }

    [Fact]
    public void ToDictionary_WithAllOptionals_IncludesAllKeys()
    {
        var config = new DdRumConfiguration
        {
            ApplicationId = "app-id",
            SessionSampleRate = 75.0,
            TelemetrySampleRate = 10.0,
            ResourceTraceSampleRate = 50.0,
            TrackFrustrations = false,
            TrackBackgroundEvents = true,
            NativeViewTracking = true,
            NativeInteractionTracking = true,
            TrackMemoryWarnings = true,
            NativeLongTaskThresholdMs = 500.0,
            InitialResourceThreshold = 0.5,
            AppHangThreshold = 2.0,
            TrackNonFatalAnrs = true,
            TrackWatchdogTerminations = true,
            VitalsUpdateFrequency = VitalsUpdateFrequency.Frequent,
            CustomEndpoint = "https://rum.example.com"
        };

        var dict = config.ToDictionary();

        Assert.Equal(75.0, dict["sessionSampleRate"]);
        Assert.Equal(10.0, dict["telemetrySampleRate"]);
        Assert.Equal(50.0, dict["resourceTraceSampleRate"]);
        Assert.Equal(false, dict["trackFrustrations"]);
        Assert.Equal(true, dict["trackBackgroundEvents"]);
        Assert.Equal(true, dict["nativeViewTracking"]);
        Assert.Equal(true, dict["nativeInteractionTracking"]);
        Assert.Equal(true, dict["trackMemoryWarnings"]);
        Assert.Equal(500.0, dict["nativeLongTaskThresholdMs"]);
        Assert.Equal(0.5, dict["initialResourceThreshold"]);
        Assert.Equal(2.0, dict["appHangThreshold"]);
        Assert.Equal(true, dict["trackNonFatalAnrs"]);
        Assert.Equal(true, dict["trackWatchdogTerminations"]);
        Assert.Equal("frequent", dict["vitalsUpdateFrequency"]);
        Assert.Equal("https://rum.example.com", dict["customEndpoint"]);
    }

    // ── Enum conversions ────────────────────────────────────────

    [Theory]
    [InlineData(VitalsUpdateFrequency.Never, "never")]
    [InlineData(VitalsUpdateFrequency.Rare, "rare")]
    [InlineData(VitalsUpdateFrequency.Average, "average")]
    [InlineData(VitalsUpdateFrequency.Frequent, "frequent")]
    public void ConvertVitalsUpdateFrequency_MapsCorrectly(VitalsUpdateFrequency input, string expected)
    {
        Assert.Equal(expected, DdRumConfiguration.ConvertVitalsUpdateFrequency(input));
    }

    [Theory]
    [InlineData(TracingHeaderType.Datadog, "datadog")]
    [InlineData(TracingHeaderType.B3, "b3")]
    [InlineData(TracingHeaderType.B3Multi, "b3multi")]
    [InlineData(TracingHeaderType.TraceContext, "tracecontext")]
    public void ConvertTracingHeaderType_MapsCorrectly(TracingHeaderType input, string expected)
    {
        Assert.Equal(expected, DdRumConfiguration.ConvertTracingHeaderType(input));
    }

    // ── FirstPartyHosts serialization ───────────────────────────

    [Fact]
    public void SerializeFirstPartyHosts_ProducesCorrectJson()
    {
        var hosts = new List<FirstPartyHost>
        {
            new() { Match = "api.example.com", HeaderTypes = new List<TracingHeaderType> { TracingHeaderType.Datadog, TracingHeaderType.TraceContext } },
            new() { Match = "cdn.example.com", HeaderTypes = new List<TracingHeaderType> { TracingHeaderType.B3 } }
        };

        var json = DdRumConfiguration.SerializeFirstPartyHosts(hosts);
        var parsed = JsonSerializer.Deserialize<JsonElement>(json);

        Assert.Equal(JsonValueKind.Array, parsed.ValueKind);
        Assert.Equal(2, parsed.GetArrayLength());

        var first = parsed[0];
        Assert.Equal("api.example.com", first.GetProperty("match").GetString());
        var headerTypes = first.GetProperty("headerTypes");
        Assert.Equal(2, headerTypes.GetArrayLength());
        Assert.Equal("datadog", headerTypes[0].GetString());
        Assert.Equal("tracecontext", headerTypes[1].GetString());

        var second = parsed[1];
        Assert.Equal("cdn.example.com", second.GetProperty("match").GetString());
        Assert.Equal("b3", second.GetProperty("headerTypes")[0].GetString());
    }

}
