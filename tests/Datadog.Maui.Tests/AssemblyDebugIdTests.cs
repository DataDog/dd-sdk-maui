/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using Xunit;

namespace Datadog.Maui.Tests;

public class AssemblyDebugIdTests
{
    [Fact]
    public void TryGetDebugId_RealAssembly_MatchesIndependentlyComputedDebugDirectoryId()
    {
        var assembly = typeof(AssemblyDebugIdTests).Assembly;

        var actual = AssemblyDebugId.TryGetDebugId(assembly);
        var expected = ComputeExpectedDebugId(assembly);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryGetDebugId_IsNotTheModuleVersionId()
    {
        var assembly = typeof(AssemblyDebugIdTests).Assembly;

        var debugId = AssemblyDebugId.TryGetDebugId(assembly);
        var mvid = assembly.ManifestModule.ModuleVersionId.ToString("N");

        Assert.NotNull(debugId);
        Assert.NotEqual(mvid, debugId);
        Assert.Matches("^[0-9a-f]{40}$", debugId);
    }

    [Fact]
    public void TryGetDebugId_AssemblyWithNoLocation_ReturnsNull()
    {
        // A dynamic in-memory assembly always has an empty Location, the same as an
        // AOT/single-file-bundled assembly at runtime — still needs real-device
        // verification, see project memory for RUM-18289.
        var dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("Datadog.Maui.Tests.DynamicFixture"),
            AssemblyBuilderAccess.Run);

        var result = AssemblyDebugId.TryGetDebugId(dynamicAssembly);

        Assert.Null(result);
    }

    /// <summary>
    /// Independently re-derives the expected debug-directory id from the assembly's own PE
    /// bytes, so this doesn't just re-assert whatever AssemblyDebugId happens to compute.
    /// </summary>
    internal static string ComputeExpectedDebugId(Assembly assembly)
    {
        using var stream = File.OpenRead(assembly.Location);
        using var peReader = new PEReader(stream);
        foreach (var entry in peReader.ReadDebugDirectory())
        {
            if (entry.Type == DebugDirectoryEntryType.CodeView)
            {
                var codeView = peReader.ReadCodeViewDebugDirectoryData(entry);
                return $"{codeView.Guid:N}{entry.Stamp:x8}";
            }
        }

        throw new InvalidOperationException($"No CodeView debug directory entry found for {assembly.FullName}");
    }
}
