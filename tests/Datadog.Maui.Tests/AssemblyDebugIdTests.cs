/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.IO;
using System.Reflection;
using System.Reflection.Emit;
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
    /// Independently re-derives the expected debug-directory id by hand-parsing the assembly's
    /// raw PE bytes, deliberately WITHOUT using System.Reflection.PortableExecutable — using
    /// PEReader/ReadCodeViewDebugDirectoryData here would just re-run the exact same library
    /// calls AssemblyDebugId.TryGetDebugId itself makes, so any bug shared by both call sites
    /// (e.g. picking the wrong stamp-like field) would pass silently on both sides.
    /// </summary>
    internal static string ComputeExpectedDebugId(Assembly assembly)
    {
        var data = File.ReadAllBytes(assembly.Location);

        var peOffset = ReadUInt32(data, 0x3C);
        if (data[peOffset] != 'P' || data[peOffset + 1] != 'E' || data[peOffset + 2] != 0 || data[peOffset + 3] != 0)
        {
            throw new InvalidOperationException("Not a valid PE file (missing PE\\0\\0 signature).");
        }

        var coffOffset = peOffset + 4;
        var numberOfSections = ReadUInt16(data, coffOffset + 2);
        var sizeOfOptionalHeader = ReadUInt16(data, coffOffset + 16);
        var optionalHeaderOffset = coffOffset + 20;
        var magic = ReadUInt16(data, optionalHeaderOffset);
        var isPe32Plus = magic == 0x20B;

        var numberOfRvaAndSizesOffset = optionalHeaderOffset + (isPe32Plus ? 108u : 92u);
        var dataDirectoriesOffset = numberOfRvaAndSizesOffset + 4;
        var debugDirectoryRva = ReadUInt32(data, dataDirectoriesOffset + 6 * 8);
        var debugDirectorySize = ReadUInt32(data, dataDirectoriesOffset + 6 * 8 + 4);

        var sectionHeadersOffset = optionalHeaderOffset + sizeOfOptionalHeader;
        var debugDirectoryOffset = RvaToOffset(data, sectionHeadersOffset, numberOfSections, debugDirectoryRva);

        var entryCount = debugDirectorySize / 28;
        for (var i = 0; i < entryCount; i++)
        {
            var entryOffset = debugDirectoryOffset + (uint)i * 28;
            var timeDateStamp = ReadUInt32(data, entryOffset + 4);
            var type = ReadUInt32(data, entryOffset + 12);
            var pointerToRawData = ReadUInt32(data, entryOffset + 24);

            const uint ImageDebugTypeCodeView = 2;
            if (type != ImageDebugTypeCodeView)
            {
                continue;
            }

            if (data[pointerToRawData] != 'R' || data[pointerToRawData + 1] != 'S' ||
                data[pointerToRawData + 2] != 'D' || data[pointerToRawData + 3] != 'S')
            {
                continue;
            }

            var guidBytes = new byte[16];
            Array.Copy(data, pointerToRawData + 4, guidBytes, 0, 16);
            var guid = new Guid(guidBytes);
            return $"{guid:N}{timeDateStamp:x8}";
        }

        throw new InvalidOperationException($"No CodeView/RSDS debug directory entry found for {assembly.FullName}");
    }

    private static uint ReadUInt32(byte[] data, uint offset) =>
        (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));

    private static ushort ReadUInt16(byte[] data, uint offset) =>
        (ushort)(data[offset] | (data[offset + 1] << 8));

    private static uint RvaToOffset(byte[] data, uint sectionHeadersOffset, ushort numberOfSections, uint rva)
    {
        for (var i = 0; i < numberOfSections; i++)
        {
            var sectionOffset = sectionHeadersOffset + (uint)i * 40;
            var virtualSize = ReadUInt32(data, sectionOffset + 8);
            var virtualAddress = ReadUInt32(data, sectionOffset + 12);
            var pointerToRawData = ReadUInt32(data, sectionOffset + 20);

            if (rva >= virtualAddress && rva < virtualAddress + virtualSize)
            {
                return pointerToRawData + (rva - virtualAddress);
            }
        }

        throw new InvalidOperationException($"RVA 0x{rva:x} not found in any section.");
    }
}
