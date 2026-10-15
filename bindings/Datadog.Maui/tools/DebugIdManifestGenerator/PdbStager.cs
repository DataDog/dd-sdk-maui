/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.Reflection.Metadata;
using System.Text.Json;

namespace Datadog.Maui.Tools.DebugIdManifestGenerator;

/// <summary>
/// Picks, for each assembly in the debug-id manifest, the Portable PDB that actually describes
/// the assembly the app ships, and copies it into a staging directory for upload.
///
/// Needed because the build can leave several same-named PDBs on disk that describe different
/// versions of an assembly. On iOS device builds the managed static registrar (run inside the
/// linker) rewrites the app's own assembly — new methods, shifted MethodDef tokens, new CodeView
/// GUID — and writes the matching PDB under obj/…/linked/, while bin/ still holds the compiler's
/// PDB for the pre-link assembly. Uploading the bin/ copy under the manifest's id makes every
/// frame resolve one method off (RUM-18989).
///
/// A candidate matches when its Portable PDB id GUID equals the manifest entry's CodeView GUID.
/// Only the GUID is compared: iOS IL stripping re-stamps every shipped assembly with the build
/// time, so the manifest's stamp legitimately differs from the PDB's even for the right file.
/// </summary>
internal static class PdbStager
{
    internal enum Outcome
    {
        Staged,
        NoMatchingPdb,
    }

    internal sealed record Result(string AssemblyName, Outcome Outcome, string? SourcePath, int CandidateCount);

    internal static IReadOnlyDictionary<string, string> ReadManifest(string manifestPath)
    {
        using var stream = File.OpenRead(manifestPath);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? new Dictionary<string, string>();
    }

    // Returns the GUID part of a Portable PDB's id (its #Pdb stream id: 16-byte GUID followed by
    // the 4-byte stamp), or null if the file isn't a readable Portable PDB (e.g. a Windows PDB).
    internal static Guid? TryReadPdbGuid(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var provider = MetadataReaderProvider.FromPortablePdbStream(stream);
            var header = provider.GetMetadataReader().DebugMetadataHeader;
            if (header is null || header.Id.Length < 16)
            {
                return null;
            }

            return new Guid(header.Id.AsSpan().Slice(0, 16));
        }
        catch
        {
            return null;
        }
    }

    // The manifest value is "{guid:N}{stamp:x8}" (see DebugIdReader.TryReadDebugId).
    internal static Guid? TryParseManifestGuid(string debugId)
    {
        if (debugId.Length != 40)
        {
            return null;
        }

        return Guid.TryParseExact(debugId[..32], "N", out var guid) ? guid : null;
    }

    /// <summary>
    /// Copies, for each manifest entry, the first candidate PDB with the same simple name whose
    /// GUID matches the entry, to "{outputDirectory}/{assemblyName}.pdb". Candidates are tried
    /// in the order given, so callers list the likeliest location first. Any *.pdb already in
    /// the output directory is removed first, so a previous build's staging can't leak into
    /// this upload.
    ///
    /// When <paramref name="onlyAssembliesNamedLike"/> is given, only assemblies whose simple
    /// name matches one of those files' names are staged. The upload target passes the PDBs
    /// found in the build output (bin/, outside the .app), so the set of uploaded assemblies
    /// stays what the previous whole-bin/ upload sent (the app's own, its project references,
    /// and any package PDBs copied there), and the linker's PDBs for other framework
    /// assemblies (e.g. Microsoft.iOS.pdb, ~21 MB) aren't uploaded as a side effect.
    /// </summary>
    internal static List<Result> Stage(
        IReadOnlyDictionary<string, string> manifest,
        IEnumerable<string> candidatePdbPaths,
        string outputDirectory,
        IEnumerable<string>? onlyAssembliesNamedLike = null)
    {
        var allowedNames = onlyAssembliesNamedLike?
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidatesByName = candidatePdbPaths
            .Where(path => path.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.Ordinal)
            .GroupBy(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase)
            .Where(group => allowedNames is null || allowedNames.Contains(group.Key))
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        Directory.CreateDirectory(outputDirectory);
        foreach (var stale in Directory.EnumerateFiles(outputDirectory, "*.pdb"))
        {
            File.Delete(stale);
        }

        var results = new List<Result>();
        foreach (var (assemblyName, debugId) in manifest.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            if (!candidatesByName.TryGetValue(assemblyName, out var candidates))
            {
                continue;
            }

            var expectedGuid = TryParseManifestGuid(debugId);
            var match = expectedGuid is null
                ? null
                : candidates.FirstOrDefault(path => TryReadPdbGuid(path) == expectedGuid);

            if (match is null)
            {
                results.Add(new Result(assemblyName, Outcome.NoMatchingPdb, null, candidates.Count));
                continue;
            }

            File.Copy(match, Path.Combine(outputDirectory, assemblyName + ".pdb"), overwrite: true);
            results.Add(new Result(assemblyName, Outcome.Staged, match, candidates.Count));
        }

        return results;
    }
}
