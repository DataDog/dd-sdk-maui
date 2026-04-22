/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

using DatadogSdk.Maui.Configuration;
using Xunit;

namespace DatadogSdk.Maui.Tests;

public class DdLogsConfigurationTests
{
    [Fact]
    public void DdLogsConfiguration_CanBeCreated()
    {
        var config = new DdLogsConfiguration();
        Assert.NotNull(config);
    }

    [Fact]
    public void DdLogsConfiguration_CustomEndpoint_DefaultsToNull()
    {
        var config = new DdLogsConfiguration();
        Assert.Null(config.CustomEndpoint);
    }

    [Fact]
    public void DdLogsConfiguration_CustomEndpoint_CanBeSet()
    {
        var config = new DdLogsConfiguration
        {
            CustomEndpoint = "https://custom-logs-endpoint.example.com"
        };

        Assert.Equal("https://custom-logs-endpoint.example.com", config.CustomEndpoint);
    }

    [Fact]
    public void DdLogsConfiguration_AcceptsNullCustomEndpoint()
    {
        var config = new DdLogsConfiguration
        {
            CustomEndpoint = null
        };

        Assert.Null(config.CustomEndpoint);
    }

    [Fact]
    public void DdLogsConfiguration_CustomEndpoint_CanBeSetWithObjectInitializer()
    {
        var config = new DdLogsConfiguration
        {
            CustomEndpoint = "https://logs.example.com/api/v1/logs"
        };

        Assert.Equal("https://logs.example.com/api/v1/logs", config.CustomEndpoint);
    }

    [Fact]
    public void DdLogsConfiguration_CustomEndpoint_AcceptsEmptyString()
    {
        var config = new DdLogsConfiguration
        {
            CustomEndpoint = string.Empty
        };

        Assert.Equal(string.Empty, config.CustomEndpoint);
    }
}
