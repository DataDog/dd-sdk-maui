/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.Reflection.PortableExecutable;

namespace Datadog.Maui.Tools.DebugIdManifestGenerator;

internal static class DebugIdReader
{
    // Returns "{guid:N}{stamp:x8}" — the same CodeView GUID + debug-directory Stamp pairing
    // upload-time tooling reads off a compiled DLL/PDB pair — or null if it can't be resolved
    // (not a managed PE, stripped, or unreadable).
    internal static string? TryReadDebugId(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var peReader = new PEReader(stream);

            foreach (var entry in peReader.ReadDebugDirectory())
            {
                if (entry.Type == DebugDirectoryEntryType.CodeView)
                {
                    var codeView = peReader.ReadCodeViewDebugDirectoryData(entry);
                    return $"{codeView.Guid:N}{entry.Stamp:x8}";
                }
            }
        }
        catch
        {
            // Skip silently — same graceful-degradation policy as the runtime lookup this
            // manifest replaces: a missing id just means that frame won't carry an assembly_id.
        }

        return null;
    }

    internal static SortedDictionary<string, string> BuildManifest(IEnumerable<string> assemblyPaths)
    {
        var manifest = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (var path in assemblyPaths)
        {
            if (!path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
            {
                continue;
            }

            var debugId = TryReadDebugId(path);
            if (debugId is null)
            {
                continue;
            }

            // Last one wins on a duplicate simple name — mirrors how the runtime lookup this
            // manifest replaces resolves by assembly name too.
            manifest[Path.GetFileNameWithoutExtension(path)] = debugId;
        }

        return manifest;
    }
}
