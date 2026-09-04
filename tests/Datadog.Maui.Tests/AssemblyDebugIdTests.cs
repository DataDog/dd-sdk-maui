/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Datadog.Maui.Tests;

public class AssemblyDebugIdTests
{
    [Fact]
    public void TryGetDebugId_ManifestHasEntryForAssemblySimpleName_ReturnsIt()
    {
        var assembly = typeof(AssemblyDebugIdTests).Assembly;
        var simpleName = assembly.GetName().Name!;
        var manifest = new Dictionary<string, string> { [simpleName] = "abc123" };

        var result = AssemblyDebugId.TryGetDebugId(assembly, manifest);

        Assert.Equal("abc123", result);
    }

    [Fact]
    public void TryGetDebugId_ManifestHasNoEntryForAssembly_ReturnsNull()
    {
        var assembly = typeof(AssemblyDebugIdTests).Assembly;
        var manifest = new Dictionary<string, string> { ["SomeOtherAssembly"] = "abc123" };

        var result = AssemblyDebugId.TryGetDebugId(assembly, manifest);

        Assert.Null(result);
    }

    [Fact]
    public void TryGetDebugId_EmptyManifest_ReturnsNull()
    {
        var assembly = typeof(AssemblyDebugIdTests).Assembly;

        var result = AssemblyDebugId.TryGetDebugId(assembly, new Dictionary<string, string>());

        Assert.Null(result);
    }

    [Fact]
    public void TryGetDebugId_LookupIsCaseSensitiveBySimpleName_NotByLocationOrPath()
    {
        // The manifest is keyed by simple name (Path.GetFileNameWithoutExtension), not by any
        // runtime notion of assembly identity — a manifest entry for the wrong case is not the
        // same key and must not match.
        var assembly = typeof(AssemblyDebugIdTests).Assembly;
        var wrongCaseName = assembly.GetName().Name!.ToUpperInvariant();
        var manifest = new Dictionary<string, string> { [wrongCaseName] = "abc123" };

        var result = AssemblyDebugId.TryGetDebugId(assembly, manifest);

        Assert.Null(result);
    }

    [Fact]
    public void TryGetDebugId_NoAppPackageManifestAvailable_ReturnsNullRatherThanThrowing()
    {
        // The single-argument overload loads the manifest via
        // FileSystem.OpenAppPackageFileAsync, which has no real MAUI app package to read from
        // in this test host — this exercises that failure is swallowed (logged, not thrown)
        // and degrades to null, the same as a genuinely missing/never-generated manifest.
        var assembly = typeof(AssemblyDebugIdTests).Assembly;

        var result = AssemblyDebugId.TryGetDebugId(assembly);

        Assert.Null(result);
    }

    [Fact]
    public void TryGetDebugId_SingleArgumentOverload_UsesManifestOverrideForTestsWhenSet()
    {
        var assembly = typeof(AssemblyDebugIdTests).Assembly;
        var simpleName = assembly.GetName().Name!;

        try
        {
            AssemblyDebugId.SetManifestOverrideForTests(new Dictionary<string, string> { [simpleName] = "override123" });

            var result = AssemblyDebugId.TryGetDebugId(assembly);

            Assert.Equal("override123", result);
        }
        finally
        {
            AssemblyDebugId.SetManifestOverrideForTests(null);
        }
    }

    // ── ParseManifest ───────────────────────────────────────────────
    // Exercises the actual JSON-parsing path used by the app-package manifest load
    // (FileSystem.OpenAppPackageFileAsync -> ParseManifest), which none of the tests above
    // reach: they either bypass it via the override seam or hit the always-empty
    // "no app package" fallback.

    [Fact]
    public void ParseManifest_RealJsonWithMultipleEntries_ParsesAllOfThem()
    {
        var json = """{"Foo": "aaaa1111", "Bar": "bbbb2222"}""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var manifest = AssemblyDebugId.ParseManifest(stream);

        Assert.Equal(2, manifest.Count);
        Assert.Equal("aaaa1111", manifest["Foo"]);
        Assert.Equal("bbbb2222", manifest["Bar"]);
    }

    [Fact]
    public void ParseManifest_EmptyJsonObject_ReturnsEmptyManifest()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{}"));

        var manifest = AssemblyDebugId.ParseManifest(stream);

        Assert.Empty(manifest);
    }

    [Fact]
    public void ParseManifest_MalformedJson_ThrowsRatherThanSilentlyReturningEmpty()
    {
        // ParseManifest itself is the seam LoadManifest wraps in try/catch — it's expected to
        // throw on bad input; LoadManifest (not exercised here) is what converts that into a
        // graceful empty-manifest fallback.
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not valid json"));

        Assert.ThrowsAny<JsonException>(() => AssemblyDebugId.ParseManifest(stream));
    }
}
