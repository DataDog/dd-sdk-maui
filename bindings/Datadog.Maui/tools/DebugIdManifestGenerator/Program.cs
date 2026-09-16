/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.Text.Json;
using Datadog.Maui.Tools.DebugIdManifestGenerator;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: DebugIdManifestGenerator <output-manifest-path> [assembly-path ...] | <output-manifest-path> @<response-file>");
    return 1;
}

var outputPath = args[0];

// A single "@<file>" argument reads assembly paths (one per line) from a response file
// instead of the command line — needed because a MAUI app's framework + package references
// can push the expanded path list past cmd.exe's ~8,191-character command-line limit.
var assemblyPaths = args.Length == 2 && args[1].StartsWith('@')
    ? File.ReadAllLines(args[1][1..]).Where(line => !string.IsNullOrWhiteSpace(line))
    : args.Skip(1);

var manifest = DebugIdReader.BuildManifest(assemblyPaths);

var fullOutputPath = Path.GetFullPath(outputPath);
Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
File.WriteAllText(fullOutputPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

return 0;
