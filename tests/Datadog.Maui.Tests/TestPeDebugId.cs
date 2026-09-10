/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.IO;
using System.Reflection;

namespace Datadog.Maui.Tests;

/// <summary>
/// Independently derives an assembly's PE debug-directory id by hand-parsing its raw bytes,
/// deliberately WITHOUT using System.Reflection.PortableExecutable — gives tests a source of
/// real, non-arbitrary debug ids to populate a test manifest with, without depending on
/// whatever library calls the production build-time manifest generator happens to use.
/// </summary>
internal static class TestPeDebugId
{
    internal static string Compute(Assembly assembly)
    {
        var data = File.ReadAllBytes(assembly.Location);

        var peOffset = ReadUInt32(data, 0x3C);
        var peOffsetInt = checked((int)peOffset);
        if (data[peOffsetInt] != 'P' || data[peOffsetInt + 1] != 'E' || data[peOffsetInt + 2] != 0 || data[peOffsetInt + 3] != 0)
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

            var pointerToRawDataInt = checked((int)pointerToRawData);
            if (data[pointerToRawDataInt] != 'R' || data[pointerToRawDataInt + 1] != 'S' ||
                data[pointerToRawDataInt + 2] != 'D' || data[pointerToRawDataInt + 3] != 'S')
            {
                continue;
            }

            var guidBytes = new byte[16];
            Array.Copy(data, pointerToRawDataInt + 4, guidBytes, 0, 16);
            var guid = new Guid(guidBytes);
            return $"{guid:N}{timeDateStamp:x8}";
        }

        throw new InvalidOperationException($"No CodeView/RSDS debug directory entry found for {assembly.FullName}");
    }

    private static uint ReadUInt32(byte[] data, uint offset)
    {
        var i = checked((int)offset);
        return (uint)(data[i] | (data[i + 1] << 8) | (data[i + 2] << 16) | (data[i + 3] << 24));
    }

    private static ushort ReadUInt16(byte[] data, uint offset)
    {
        var i = checked((int)offset);
        return (ushort)(data[i] | (data[i + 1] << 8));
    }

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
