/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.Text.Json;
using Datadog.Maui.Tools.DebugIdManifestGenerator;

const string StagePdbsCommand = "--stage-pdbs";

// --stage-pdbs exit codes: 0 = every assembly that had candidates got a matching PDB staged;
// 2 = staging done, but at least one assembly had candidates and none matched (its frames
// won't symbolicate — the targets turn this into a warning); 1 = usage error or failure.
const int StageExitUnmatched = 2;

if (args.Length >= 1 && args[0] == StagePdbsCommand)
{
    return StagePdbs(args[1..]);
}

if (args.Length < 1)
{
    PrintUsage();
    return 1;
}

var outputPath = args[0];
var assemblyPaths = ReadPathArguments(args[1..]);

var manifest = DebugIdReader.BuildManifest(assemblyPaths);

var fullOutputPath = Path.GetFullPath(outputPath);
Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
File.WriteAllText(fullOutputPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

return 0;

static int StagePdbs(string[] stageArgs)
{
    if (stageArgs.Length is not (3 or 4) || !stageArgs[2].StartsWith('@') || (stageArgs.Length == 4 && !stageArgs[3].StartsWith('@')))
    {
        PrintUsage();
        return 1;
    }

    var manifestPath = stageArgs[0];
    var outputDirectory = stageArgs[1];
    var candidatePaths = ReadPathArguments([stageArgs[2]]);
    var onlyAssembliesNamedLike = stageArgs.Length == 4 ? ReadPathArguments([stageArgs[3]]) : null;

    List<PdbStager.Result> results;
    try
    {
        results = PdbStager.Stage(PdbStager.ReadManifest(manifestPath), candidatePaths, outputDirectory, onlyAssembliesNamedLike);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Datadog: staging Portable PDBs failed: {ex.Message}");
        return 1;
    }

    foreach (var result in results)
    {
        Console.WriteLine(result.Outcome == PdbStager.Outcome.Staged
            ? $"Datadog: staged Portable PDB for {result.AssemblyName} from {result.SourcePath}"
            : $"Datadog: not uploading a Portable PDB for {result.AssemblyName} — none of its {result.CandidateCount} candidate(s) matches the debug id of the assembly the app ships");
    }

    return results.Any(result => result.Outcome == PdbStager.Outcome.NoMatchingPdb) ? StageExitUnmatched : 0;
}

// A single "@<file>" argument reads paths (one per line) from a response file instead of the
// command line — needed because a MAUI app's framework + package references can push the
// expanded path list past cmd.exe's ~8,191-character command-line limit.
static IEnumerable<string> ReadPathArguments(string[] pathArgs) =>
    pathArgs.Length == 1 && pathArgs[0].StartsWith('@')
        ? File.ReadAllLines(pathArgs[0][1..]).Where(line => !string.IsNullOrWhiteSpace(line))
        : pathArgs;

static void PrintUsage()
{
    Console.Error.WriteLine("Usage: DebugIdManifestGenerator <output-manifest-path> [assembly-path ...] | <output-manifest-path> @<response-file>");
    Console.Error.WriteLine("       DebugIdManifestGenerator --stage-pdbs <manifest-path> <output-directory> @<candidate-pdbs-response-file> [@<only-assemblies-named-like-response-file>]");
}
