/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.Text.Json;
using Datadog.Maui.Tools.DebugIdManifestGenerator;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: DebugIdManifestGenerator <output-manifest-path> [assembly-path ...]");
    return 1;
}

var outputPath = args[0];
var manifest = DebugIdReader.BuildManifest(args.Skip(1));

var fullOutputPath = Path.GetFullPath(outputPath);
Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
File.WriteAllText(fullOutputPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

return 0;
