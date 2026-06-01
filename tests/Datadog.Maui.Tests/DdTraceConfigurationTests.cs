/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using Datadog.Maui.Configuration;
using Xunit;

namespace Datadog.Maui.Tests;

public class DdTraceConfigurationTests
{
    [Fact]
    public void DdTraceConfiguration_CanBeCreated()
    {
        var config = new DdTraceConfiguration();
        Assert.NotNull(config);
    }

    [Fact]
    public void DdTraceConfiguration_CustomEndpoint_DefaultsToNull()
    {
        var config = new DdTraceConfiguration();
        Assert.Null(config.CustomEndpoint);
    }

    [Fact]
    public void DdTraceConfiguration_CustomEndpoint_CanBeSet()
    {
        var config = new DdTraceConfiguration
        {
            CustomEndpoint = "https://custom-trace-endpoint.example.com"
        };

        Assert.Equal("https://custom-trace-endpoint.example.com", config.CustomEndpoint);
    }

    [Fact]
    public void DdTraceConfiguration_AcceptsNullCustomEndpoint()
    {
        var config = new DdTraceConfiguration
        {
            CustomEndpoint = null
        };

        Assert.Null(config.CustomEndpoint);
    }

    [Fact]
    public void DdTraceConfiguration_CustomEndpoint_CanBeSetWithObjectInitializer()
    {
        var config = new DdTraceConfiguration
        {
            CustomEndpoint = "https://traces.example.com/api/v1/traces"
        };

        Assert.Equal("https://traces.example.com/api/v1/traces", config.CustomEndpoint);
    }

    [Fact]
    public void DdTraceConfiguration_CustomEndpoint_AcceptsEmptyString()
    {
        var config = new DdTraceConfiguration
        {
            CustomEndpoint = string.Empty
        };

        Assert.Equal(string.Empty, config.CustomEndpoint);
    }
}
