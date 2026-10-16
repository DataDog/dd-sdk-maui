/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using Datadog.Maui.Tools.DebugIdManifestGenerator;
using Xunit;

namespace DebugIdManifestGenerator.Tests;

public sealed class PdbStagerTests : IDisposable
{
    // Two real, distinct assembly/Portable PDB pairs: this test project's own output and the
    // generator tool's (copied next to it by the project reference).
    private static readonly string SelfDll = typeof(PdbStagerTests).Assembly.Location;
    private static readonly string SelfPdb = Path.ChangeExtension(SelfDll, ".pdb");
    private static readonly string SelfName = Path.GetFileNameWithoutExtension(SelfDll);
    private static readonly string GeneratorDll = typeof(PdbStager).Assembly.Location;
    private static readonly string GeneratorPdb = Path.ChangeExtension(GeneratorDll, ".pdb");

    private readonly DirectoryInfo _workDir = Directory.CreateTempSubdirectory();

    private string CandidatesDir => Directory.CreateDirectory(Path.Combine(_workDir.FullName, "candidates")).FullName;

    private string OutputDir => Path.Combine(_workDir.FullName, "staged");

    public void Dispose() => _workDir.Delete(recursive: true);

    private static Dictionary<string, string> ManifestFor(string assemblyName, string dllPath) =>
        new() { [assemblyName] = DebugIdReader.TryReadDebugId(dllPath)! };

    // Places a copy of `sourcePdb` at `{dir}/{fileName}`, so a same-named candidate can carry
    // another assembly's PDB content — the shape of the real bug (bin/ holds a same-named PDB
    // describing a different version of the assembly).
    private static string PlaceCandidate(string dir, string fileName, string sourcePdb)
    {
        var path = Path.Combine(Directory.CreateDirectory(dir).FullName, fileName);
        File.Copy(sourcePdb, path, overwrite: true);
        return path;
    }

    [Fact]
    public void TryReadPdbGuid_MatchesTheGuidPartOfTheAssemblysDebugId()
    {
        var guid = PdbStager.TryReadPdbGuid(SelfPdb);
        var manifestGuid = PdbStager.TryParseManifestGuid(DebugIdReader.TryReadDebugId(SelfDll)!);

        Assert.NotNull(guid);
        Assert.Equal(manifestGuid, guid);
    }

    [Fact]
    public void Stage_CopiesTheMatchingPdbUnderTheManifestName()
    {
        var candidate = PlaceCandidate(CandidatesDir, SelfName + ".pdb", SelfPdb);

        var results = PdbStager.Stage(ManifestFor(SelfName, SelfDll), [candidate], OutputDir);

        var result = Assert.Single(results);
        Assert.Equal(PdbStager.Outcome.Staged, result.Outcome);
        Assert.Equal(candidate, result.SourcePath);
        Assert.Equal(File.ReadAllBytes(SelfPdb), File.ReadAllBytes(Path.Combine(OutputDir, SelfName + ".pdb")));
    }

    [Fact]
    public void Stage_RejectsASameNamedPdbThatDescribesADifferentAssembly()
    {
        // Same file name as the manifest entry, but the content is another assembly's PDB —
        // what bin/example.pdb is on an iOS device build (the pre-link compiler PDB).
        var wrong = PlaceCandidate(CandidatesDir, SelfName + ".pdb", GeneratorPdb);

        var results = PdbStager.Stage(ManifestFor(SelfName, SelfDll), [wrong], OutputDir);

        var result = Assert.Single(results);
        Assert.Equal(PdbStager.Outcome.NoMatchingPdb, result.Outcome);
        Assert.Equal(1, result.CandidateCount);
        Assert.Empty(Directory.EnumerateFiles(OutputDir));
    }

    [Fact]
    public void Stage_PicksTheMatchingCandidateEvenWhenAMismatchingOneIsListedFirst()
    {
        var wrong = PlaceCandidate(Path.Combine(_workDir.FullName, "bin"), SelfName + ".pdb", GeneratorPdb);
        var right = PlaceCandidate(Path.Combine(_workDir.FullName, "linked"), SelfName + ".pdb", SelfPdb);

        var results = PdbStager.Stage(ManifestFor(SelfName, SelfDll), [wrong, right], OutputDir);

        var result = Assert.Single(results);
        Assert.Equal(PdbStager.Outcome.Staged, result.Outcome);
        Assert.Equal(right, result.SourcePath);
        Assert.Equal(2, result.CandidateCount);
        Assert.Equal(File.ReadAllBytes(SelfPdb), File.ReadAllBytes(Path.Combine(OutputDir, SelfName + ".pdb")));
    }

    [Fact]
    public void Stage_PicksTheFirstMatchingCandidateWhenSeveralMatch()
    {
        var first = PlaceCandidate(Path.Combine(_workDir.FullName, "linked"), SelfName + ".pdb", SelfPdb);
        var second = PlaceCandidate(Path.Combine(_workDir.FullName, "bin"), SelfName + ".pdb", SelfPdb);

        var results = PdbStager.Stage(ManifestFor(SelfName, SelfDll), [first, second], OutputDir);

        Assert.Equal(first, Assert.Single(results).SourcePath);
    }

    [Fact]
    public void Stage_AcceptsAMatchingGuidWhenOnlyTheStampDiffers()
    {
        // iOS IL stripping re-stamps the shipped assembly, so the manifest's stamp differs from
        // the PDB's own stamp even for the right PDB.
        var debugId = DebugIdReader.TryReadDebugId(SelfDll)!;
        var restamped = debugId[..32] + (debugId[32..] == "00000000" ? "11111111" : "00000000");
        var candidate = PlaceCandidate(CandidatesDir, SelfName + ".pdb", SelfPdb);

        var results = PdbStager.Stage(new Dictionary<string, string> { [SelfName] = restamped }, [candidate], OutputDir);

        Assert.Equal(PdbStager.Outcome.Staged, Assert.Single(results).Outcome);
    }

    [Fact]
    public void Stage_SkipsManifestEntriesThatHaveNoCandidate()
    {
        var results = PdbStager.Stage(ManifestFor(SelfName, SelfDll), [], OutputDir);

        Assert.Empty(results);
        Assert.Empty(Directory.EnumerateFiles(OutputDir));
    }

    [Fact]
    public void Stage_IgnoresCandidatesThatAreNotForAnyManifestEntry()
    {
        var unrelated = PlaceCandidate(CandidatesDir, "Unrelated.pdb", GeneratorPdb);

        var results = PdbStager.Stage(ManifestFor(SelfName, SelfDll), [unrelated], OutputDir);

        Assert.Empty(results);
        Assert.Empty(Directory.EnumerateFiles(OutputDir));
    }

    [Fact]
    public void Stage_TreatsAnUnreadablePdbAsNotMatching()
    {
        var garbage = Path.Combine(CandidatesDir, SelfName + ".pdb");
        File.WriteAllBytes(garbage, [0x00, 0x01, 0x02, 0x03]);

        var results = PdbStager.Stage(ManifestFor(SelfName, SelfDll), [garbage], OutputDir);

        Assert.Equal(PdbStager.Outcome.NoMatchingPdb, Assert.Single(results).Outcome);
    }

    [Fact]
    public void Stage_TreatsAMalformedManifestIdAsNotMatching()
    {
        var candidate = PlaceCandidate(CandidatesDir, SelfName + ".pdb", SelfPdb);

        var results = PdbStager.Stage(new Dictionary<string, string> { [SelfName] = "not-a-debug-id" }, [candidate], OutputDir);

        Assert.Equal(PdbStager.Outcome.NoMatchingPdb, Assert.Single(results).Outcome);
        Assert.Empty(Directory.EnumerateFiles(OutputDir));
    }

    [Fact]
    public void Stage_IgnoresMissingAndNonPdbCandidates()
    {
        var missing = Path.Combine(CandidatesDir, SelfName + ".pdb");
        var notAPdb = PlaceCandidate(CandidatesDir, SelfName + ".txt", SelfPdb);

        var results = PdbStager.Stage(ManifestFor(SelfName, SelfDll), [missing, notAPdb], OutputDir);

        Assert.Empty(results);
    }

    [Fact]
    public void Stage_MatchesCandidateFileNamesCaseInsensitively()
    {
        var candidate = PlaceCandidate(CandidatesDir, SelfName.ToUpperInvariant() + ".PDB", SelfPdb);

        var results = PdbStager.Stage(ManifestFor(SelfName, SelfDll), [candidate], OutputDir);

        Assert.Equal(PdbStager.Outcome.Staged, Assert.Single(results).Outcome);
        Assert.Equal([SelfName + ".pdb"], Directory.EnumerateFiles(OutputDir).Select(Path.GetFileName));
    }

    [Fact]
    public void Stage_RemovesPdbsLeftInTheOutputDirectoryByAPreviousRun()
    {
        Directory.CreateDirectory(OutputDir);
        File.WriteAllBytes(Path.Combine(OutputDir, "Stale.pdb"), [0x00]);

        PdbStager.Stage(ManifestFor(SelfName, SelfDll), [], OutputDir);

        Assert.Empty(Directory.EnumerateFiles(OutputDir));
    }

    [Fact]
    public void Stage_OnlyStagesAssembliesNamedLikeTheRestrictionFiles()
    {
        // The upload target restricts staging to the assemblies whose PDB is in bin/, so the
        // linker's PDBs for framework assemblies aren't uploaded as a side effect.
        var generatorName = Path.GetFileNameWithoutExtension(GeneratorDll);
        var manifest = ManifestFor(SelfName, SelfDll);
        manifest[generatorName] = DebugIdReader.TryReadDebugId(GeneratorDll)!;
        var selfCandidate = PlaceCandidate(Path.Combine(_workDir.FullName, "linked"), SelfName + ".pdb", SelfPdb);
        var generatorCandidate = PlaceCandidate(Path.Combine(_workDir.FullName, "linked"), generatorName + ".pdb", GeneratorPdb);
        var selfInBin = Path.Combine(_workDir.FullName, "bin", SelfName + ".pdb");

        var results = PdbStager.Stage(manifest, [selfCandidate, generatorCandidate], OutputDir, [selfInBin]);

        var result = Assert.Single(results);
        Assert.Equal(SelfName, result.AssemblyName);
        Assert.Equal(PdbStager.Outcome.Staged, result.Outcome);
        Assert.Equal([SelfName + ".pdb"], Directory.EnumerateFiles(OutputDir).Select(Path.GetFileName));
    }

    [Fact]
    public void Stage_StagesNothingWhenTheRestrictionListIsEmpty()
    {
        var candidate = PlaceCandidate(CandidatesDir, SelfName + ".pdb", SelfPdb);

        var results = PdbStager.Stage(ManifestFor(SelfName, SelfDll), [candidate], OutputDir, []);

        Assert.Empty(results);
        Assert.Empty(Directory.EnumerateFiles(OutputDir));
    }

    [Fact]
    public void ReadManifest_ReadsTheGeneratorsOwnOutputFormat()
    {
        var manifestPath = Path.Combine(_workDir.FullName, "dd_debug_ids.json");
        var expected = DebugIdReader.BuildManifest([SelfDll, GeneratorDll]);
        File.WriteAllText(manifestPath, System.Text.Json.JsonSerializer.Serialize(expected));

        var manifest = PdbStager.ReadManifest(manifestPath);

        Assert.Equal(expected.OrderBy(e => e.Key), manifest.OrderBy(e => e.Key));
    }
}
