/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.Buffers.Binary;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Datadog.Maui.Tools.DebugIdManifestGenerator;
using Xunit;

namespace DebugIdManifestGenerator.Tests;

public class DebugIdReaderTests
{
    // A real, currently-loaded managed assembly with a portable PDB next to it — this test
    // project's own compiled output.
    private static readonly string SelfDll = typeof(DebugIdReaderTests).Assembly.Location;
    private static readonly string SelfPdb = Path.ChangeExtension(SelfDll, ".pdb");

    // A second, distinct real assembly (the generator tool itself), to prove two different
    // assemblies don't collide on the same id.
    private static readonly string GeneratorDll = typeof(DebugIdReader).Assembly.Location;

    [Fact]
    public void TryReadDebugId_ReturnsWellFormedId_ForRealManagedAssembly()
    {
        var id = DebugIdReader.TryReadDebugId(SelfDll);

        Assert.NotNull(id);
        Assert.Matches("^[0-9a-f]{40}$", id);
    }

    [Fact]
    public void TryReadDebugId_MatchesIdIndependentlyReadFromThePdb()
    {
        // Cross-check against a value derived a completely different way: read the Portable
        // PDB's own embedded DebugMetadataHeader.Id (the id the compiler stamped into the PDB
        // itself) instead of the DLL's CodeView debug-directory entry that DebugIdReader reads.
        // Per the Portable PDB spec, and confirmed empirically, this Id's first 16 bytes are the
        // same GUID and its last 4 bytes are the same Stamp (as a little-endian uint32) that the
        // DLL's debug directory carries — so the two independently-read values must agree.
        //
        // This guards against a bug where the DLL-side reading is internally self-consistent but
        // doesn't actually match what's inside the PDB that ships and gets uploaded — the
        // property this whole feature depends on.
        var dllId = DebugIdReader.TryReadDebugId(SelfDll);
        Assert.NotNull(dllId);

        using var pdbStream = File.OpenRead(SelfPdb);
        using var pdbProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
        var pdbReader = pdbProvider.GetMetadataReader();
        var header = pdbReader.DebugMetadataHeader;
        Assert.NotNull(header);

        var idBytes = header!.Id.ToArray();
        var expectedGuid = new Guid(idBytes.Take(16).ToArray());
        var expectedStamp = BinaryPrimitives.ReadUInt32LittleEndian(idBytes.AsSpan(16, 4));
        var expectedId = $"{expectedGuid:N}{expectedStamp:x8}";

        Assert.Equal(expectedId, dllId);
    }

    [Fact]
    public void TryReadDebugId_ReturnsDifferentIds_ForDifferentAssemblies()
    {
        var selfId = DebugIdReader.TryReadDebugId(SelfDll);
        var generatorId = DebugIdReader.TryReadDebugId(GeneratorDll);

        Assert.NotNull(selfId);
        Assert.NotNull(generatorId);
        Assert.NotEqual(selfId, generatorId);
    }

    [Fact]
    public void TryReadDebugId_ReturnsSameId_ForSameFile_AcrossRepeatedReads()
    {
        var first = DebugIdReader.TryReadDebugId(SelfDll);
        var second = DebugIdReader.TryReadDebugId(SelfDll);

        Assert.Equal(first, second);
    }

    [Fact]
    public void TryReadDebugId_ReturnsNull_ForMissingFile()
    {
        var id = DebugIdReader.TryReadDebugId(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dll"));

        Assert.Null(id);
    }

    [Fact]
    public void TryReadDebugId_ReturnsNull_ForNonPortableExecutableFile()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, [0x00, 0x01, 0x02, 0x03]);

            var id = DebugIdReader.TryReadDebugId(path);

            Assert.Null(id);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void BuildManifest_SkipsFilesWithoutDllExtension()
    {
        var nonDllCopy = Path.GetTempFileName();
        try
        {
            File.Copy(SelfDll, nonDllCopy, overwrite: true);

            var manifest = DebugIdReader.BuildManifest([nonDllCopy]);

            Assert.Empty(manifest);
        }
        finally
        {
            File.Delete(nonDllCopy);
        }
    }

    [Fact]
    public void BuildManifest_SkipsMissingFiles()
    {
        var manifest = DebugIdReader.BuildManifest([Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dll")]);

        Assert.Empty(manifest);
    }

    [Fact]
    public void BuildManifest_KeysBySimpleAssemblyName()
    {
        var manifest = DebugIdReader.BuildManifest([SelfDll]);

        var expectedKey = Path.GetFileNameWithoutExtension(SelfDll);
        Assert.True(manifest.ContainsKey(expectedKey));
        Assert.Equal(DebugIdReader.TryReadDebugId(SelfDll), manifest[expectedKey]);
    }

    [Fact]
    public void BuildManifest_LastPathWinsOnDuplicateSimpleName()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var duplicateName = Path.GetFileName(SelfDll);
            var copyPath = Path.Combine(tempDir.FullName, duplicateName);
            File.Copy(GeneratorDll, copyPath, overwrite: true);

            // SelfDll's own id first, then a copy of GeneratorDll's bytes under SelfDll's simple
            // name — the second entry (GeneratorDll's id) should win.
            var manifest = DebugIdReader.BuildManifest([SelfDll, copyPath]);

            var key = Path.GetFileNameWithoutExtension(SelfDll);
            Assert.Equal(DebugIdReader.TryReadDebugId(GeneratorDll), manifest[key]);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void BuildManifest_LeavesOutAnAssemblyWithoutACodeViewEntry()
    {
        // Linkers that rewrite an assembly without symbols (e.g. Android's) drop its CodeView
        // entry. Such an assembly must get no manifest line at all, so it can't be paired with
        // a PDB uploaded for an earlier version of it (RUM-18989).
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var noCodeViewDll = Path.Combine(tempDir.FullName, "NoCodeView.dll");
            WriteAssemblyWithoutDebugDirectory(noCodeViewDll);

            var manifest = DebugIdReader.BuildManifest([noCodeViewDll, SelfDll]);

            Assert.False(manifest.ContainsKey("NoCodeView"));
            Assert.True(manifest.ContainsKey(Path.GetFileNameWithoutExtension(SelfDll)));
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    private static void WriteAssemblyWithoutDebugDirectory(string path)
    {
        var metadata = new MetadataBuilder();
        metadata.AddModule(0, metadata.GetOrAddString(Path.GetFileName(path)), metadata.GetOrAddGuid(Guid.NewGuid()), default, default);
        metadata.AddAssembly(metadata.GetOrAddString(Path.GetFileNameWithoutExtension(path)), new Version(1, 0, 0, 0), default, default, 0, AssemblyHashAlgorithm.None);
        metadata.AddTypeDefinition(default, default, metadata.GetOrAddString("<Module>"), default, MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));

        var peBuilder = new ManagedPEBuilder(PEHeaderBuilder.CreateLibraryHeader(), new MetadataRootBuilder(metadata), new BlobBuilder());
        var blob = new BlobBuilder();
        peBuilder.Serialize(blob);
        File.WriteAllBytes(path, blob.ToArray());
    }

    [Fact]
    public void BuildManifest_SortsKeysOrdinally()
    {
        var manifest = DebugIdReader.BuildManifest([SelfDll, GeneratorDll]);

        var keys = manifest.Keys.ToList();
        var sortedKeys = keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        Assert.Equal(sortedKeys, keys);
    }
}
