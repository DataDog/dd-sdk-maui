/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using DatadogSdk.Maui.AutoTracking;
using DatadogSdk.Maui.Configuration;
using Xunit;

namespace DatadogSdk.Maui.Tests;

public class ResourceKindResolverTests
{
    [Theory]
    [InlineData("image/png", RumResourceKind.Image)]
    [InlineData("image/jpeg", RumResourceKind.Image)]
    [InlineData("image/svg+xml", RumResourceKind.Image)]
    [InlineData("application/javascript", RumResourceKind.Js)]
    [InlineData("text/javascript", RumResourceKind.Js)]
    [InlineData("text/css", RumResourceKind.Css)]
    [InlineData("font/woff2", RumResourceKind.Font)]
    [InlineData("application/font-woff", RumResourceKind.Font)]
    [InlineData("application/x-font-ttf", RumResourceKind.Font)]
    [InlineData("video/mp4", RumResourceKind.Media)]
    [InlineData("audio/mpeg", RumResourceKind.Media)]
    [InlineData("application/json", RumResourceKind.Native)]
    [InlineData("application/xml", RumResourceKind.Native)]
    [InlineData("text/xml", RumResourceKind.Native)]
    [InlineData("text/html", RumResourceKind.Native)]
    [InlineData("application/octet-stream", RumResourceKind.Other)]
    [InlineData("text/plain", RumResourceKind.Other)]
    [InlineData(null, RumResourceKind.Other)]
    [InlineData("", RumResourceKind.Other)]
    public void Resolve_MapsContentTypeCorrectly(string? contentType, RumResourceKind expected)
    {
        Assert.Equal(expected, ResourceKindResolver.Resolve(contentType));
    }

    [Fact]
    public void Resolve_IsCaseInsensitive()
    {
        Assert.Equal(RumResourceKind.Image, ResourceKindResolver.Resolve("IMAGE/PNG"));
        Assert.Equal(RumResourceKind.Js, ResourceKindResolver.Resolve("Application/JavaScript"));
    }
}
