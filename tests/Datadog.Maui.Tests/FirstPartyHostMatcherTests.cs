/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using Datadog.Maui.AutoTracking;
using Datadog.Maui.Configuration;
using Xunit;

namespace Datadog.Maui.Tests;

public class FirstPartyHostMatcherTests
{
    // ── Exact match ──────────────────────────────────────────────────────────

    [Fact]
    public void GetHeaderTypes_ExactMatch_ReturnsTypes()
    {
        var hosts = new List<FirstPartyHost>
        {
            new() { Match = "api.example.com", HeaderTypes = new List<TracingHeaderType> { TracingHeaderType.Datadog } }
        };
        var matcher = new FirstPartyHostMatcher(hosts);

        var result = matcher.GetHeaderTypes(new Uri("https://api.example.com/path"));

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains(TracingHeaderType.Datadog, result);
    }

    // ── Subdomain match (RN compat) ───────────────────────────────────────────

    [Fact]
    public void GetHeaderTypes_SubdomainMatch_ReturnsTypes()
    {
        var hosts = new List<FirstPartyHost>
        {
            new() { Match = "example.com", HeaderTypes = new List<TracingHeaderType> { TracingHeaderType.TraceContext } }
        };
        var matcher = new FirstPartyHostMatcher(hosts);

        var result = matcher.GetHeaderTypes(new Uri("https://api.example.com/path"));

        Assert.NotNull(result);
        Assert.Contains(TracingHeaderType.TraceContext, result);
    }

    // ── No match ─────────────────────────────────────────────────────────────

    [Fact]
    public void GetHeaderTypes_NoMatch_ReturnsNull()
    {
        var hosts = new List<FirstPartyHost>
        {
            new() { Match = "example.com", HeaderTypes = new List<TracingHeaderType> { TracingHeaderType.Datadog } }
        };
        var matcher = new FirstPartyHostMatcher(hosts);

        var result = matcher.GetHeaderTypes(new Uri("https://other.com/path"));

        Assert.Null(result);
    }

    // ── Multiple header types on one host ─────────────────────────────────────

    [Fact]
    public void GetHeaderTypes_MultipleHeaderTypes_ReturnsAll()
    {
        var hosts = new List<FirstPartyHost>
        {
            new()
            {
                Match = "api.example.com",
                HeaderTypes = new List<TracingHeaderType>
                {
                    TracingHeaderType.Datadog,
                    TracingHeaderType.TraceContext
                }
            }
        };
        var matcher = new FirstPartyHostMatcher(hosts);

        var result = matcher.GetHeaderTypes(new Uri("https://api.example.com/v1/resource"));

        Assert.NotNull(result);
        Assert.Contains(TracingHeaderType.Datadog, result);
        Assert.Contains(TracingHeaderType.TraceContext, result);
    }

    // ── Multiple hosts — correct one matched ──────────────────────────────────

    [Fact]
    public void GetHeaderTypes_MultipleHosts_MatchesCorrectOne()
    {
        var hosts = new List<FirstPartyHost>
        {
            new() { Match = "first.example.com",  HeaderTypes = new List<TracingHeaderType> { TracingHeaderType.B3 } },
            new() { Match = "second.example.com", HeaderTypes = new List<TracingHeaderType> { TracingHeaderType.B3Multi } }
        };
        var matcher = new FirstPartyHostMatcher(hosts);

        var result = matcher.GetHeaderTypes(new Uri("https://second.example.com/data"));

        Assert.NotNull(result);
        Assert.Contains(TracingHeaderType.B3Multi, result);
        Assert.DoesNotContain(TracingHeaderType.B3, result);
    }

    // ── Null hosts ────────────────────────────────────────────────────────────

    [Fact]
    public void GetHeaderTypes_NullHosts_ReturnsNull()
    {
        var matcher = new FirstPartyHostMatcher(null);

        var result = matcher.GetHeaderTypes(new Uri("https://api.example.com/path"));

        Assert.Null(result);
    }

    // ── Empty host list ───────────────────────────────────────────────────────

    [Fact]
    public void GetHeaderTypes_EmptyHosts_ReturnsNull()
    {
        var matcher = new FirstPartyHostMatcher(new List<FirstPartyHost>());

        var result = matcher.GetHeaderTypes(new Uri("https://api.example.com/path"));

        Assert.Null(result);
    }

    // ── Case-insensitive matching ─────────────────────────────────────────────

    [Fact]
    public void GetHeaderTypes_CaseInsensitive_Matches()
    {
        var hosts = new List<FirstPartyHost>
        {
            new() { Match = "example.com", HeaderTypes = new List<TracingHeaderType> { TracingHeaderType.Datadog } }
        };
        var matcher = new FirstPartyHostMatcher(hosts);

        var result = matcher.GetHeaderTypes(new Uri("https://EXAMPLE.COM/path"));

        Assert.NotNull(result);
        Assert.Contains(TracingHeaderType.Datadog, result);
    }
}
