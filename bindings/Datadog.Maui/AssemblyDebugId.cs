/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System;
using System.IO;
using System.Reflection;
using System.Reflection.PortableExecutable;

namespace Datadog.Maui
{
    /// <summary>
    /// Reads the native PE debug-directory id (CodeView GUID + debug-directory Stamp) of an
    /// assembly — the same identifier upload-time tooling reads off the compiled DLL/PDB pair.
    /// This is intentionally NOT Module.ModuleVersionId (the MVID): the MVID identifies the
    /// managed metadata, not the PE/PDB pairing, and doesn't match what build-time tooling uses
    /// to correlate a binary with its PDB.
    /// </summary>
    internal static class AssemblyDebugId
    {
        /// <summary>
        /// Returns "{guid:N}{stamp:x8}", or null if the id can't be resolved.
        /// Resolution requires a real file at Assembly.Location, so this returns null for
        /// assemblies loaded from single-file/AOT bundles where Location is empty — known gap,
        /// see RUM-18289 project memory for the still-needed real-device verification.
        /// </summary>
        internal static string? TryGetDebugId(Assembly assembly)
        {
            var location = assembly.Location;
            if (string.IsNullOrEmpty(location) || !File.Exists(location))
            {
                return null;
            }

            try
            {
                using var stream = File.OpenRead(location);
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
            catch (Exception ex)
            {
                InternalLog.Log(
                    $"AssemblyDebugId: Failed to read debug directory for assembly '{assembly.FullName}': {ex.Message}",
                    SdkVerbosity.DEBUG);
            }

            return null;
        }
    }
}
