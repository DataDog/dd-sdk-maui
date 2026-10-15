/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.Diagnostics;
using System.Text.Json;
using Datadog.Maui.Tools.DebugIdManifestGenerator;
using Xunit;

namespace DebugIdManifestGenerator.Tests;

/// <summary>
/// Runs the tool the way the MSBuild targets do (`dotnet exec DebugIdManifestGenerator.dll …`),
/// to pin its command line and exit codes — the targets branch on those exit codes.
/// </summary>
public sealed class ProgramTests : IDisposable
{
    private static readonly string GeneratorDll = typeof(PdbStager).Assembly.Location;
    private static readonly string SelfDll = typeof(ProgramTests).Assembly.Location;
    private static readonly string SelfPdb = Path.ChangeExtension(SelfDll, ".pdb");
    private static readonly string SelfName = Path.GetFileNameWithoutExtension(SelfDll);
    private static readonly string GeneratorPdb = Path.ChangeExtension(GeneratorDll, ".pdb");

    private readonly DirectoryInfo _workDir = Directory.CreateTempSubdirectory();

    public void Dispose() => _workDir.Delete(recursive: true);

    private static (int ExitCode, string StdOut, string StdErr) RunTool(params string[] args)
    {
        var startInfo = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add(GeneratorDll);
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo)!;
        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        return (process.ExitCode, stdOutTask.Result, stdErrTask.Result);
    }

    private string WriteFile(string name, params string[] lines)
    {
        var path = Path.Combine(_workDir.FullName, name);
        File.WriteAllLines(path, lines);
        return path;
    }

    private string WriteManifest(Dictionary<string, string> manifest)
    {
        var path = Path.Combine(_workDir.FullName, "dd_debug_ids.json");
        File.WriteAllText(path, JsonSerializer.Serialize(manifest));
        return path;
    }

    private string PlaceCandidate(string fileName, string sourcePdb)
    {
        var path = Path.Combine(Directory.CreateDirectory(Path.Combine(_workDir.FullName, "candidates")).FullName, fileName);
        File.Copy(sourcePdb, path, overwrite: true);
        return path;
    }

    [Fact]
    public void ManifestMode_ReadsAResponseFileWithBlankLines()
    {
        var manifestPath = Path.Combine(_workDir.FullName, "out", "dd_debug_ids.json");
        var rsp = WriteFile("assemblies.rsp", SelfDll, "", "   ", GeneratorDll);

        var (exitCode, _, _) = RunTool(manifestPath, "@" + rsp);

        Assert.Equal(0, exitCode);
        var manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(manifestPath))!;
        Assert.Equal(DebugIdReader.TryReadDebugId(SelfDll), manifest[SelfName]);
        Assert.Equal(2, manifest.Count);
    }

    [Fact]
    public void ManifestMode_StillAcceptsAssemblyPathsAsArguments()
    {
        var manifestPath = Path.Combine(_workDir.FullName, "dd_debug_ids.json");

        var (exitCode, _, _) = RunTool(manifestPath, SelfDll);

        Assert.Equal(0, exitCode);
        Assert.Contains(SelfName, File.ReadAllText(manifestPath));
    }

    [Fact]
    public void NoArguments_IsAUsageError()
    {
        Assert.Equal(1, RunTool().ExitCode);
    }

    [Fact]
    public void StageMode_ExitsZeroWhenEveryAssemblyWithCandidatesGetsAMatch()
    {
        var manifest = WriteManifest(new() { [SelfName] = DebugIdReader.TryReadDebugId(SelfDll)! });
        var candidates = WriteFile("candidates.rsp", PlaceCandidate(SelfName + ".pdb", SelfPdb));
        var output = Path.Combine(_workDir.FullName, "staged");

        var (exitCode, stdOut, _) = RunTool("--stage-pdbs", manifest, output, "@" + candidates);

        Assert.Equal(0, exitCode);
        Assert.Contains($"staged Portable PDB for {SelfName}", stdOut);
        Assert.True(File.Exists(Path.Combine(output, SelfName + ".pdb")));
    }

    [Fact]
    public void StageMode_ExitsTwoWhenAnAssemblyHasCandidatesButNoneMatches()
    {
        var manifest = WriteManifest(new() { [SelfName] = DebugIdReader.TryReadDebugId(SelfDll)! });
        var candidates = WriteFile("candidates.rsp", PlaceCandidate(SelfName + ".pdb", GeneratorPdb));
        var output = Path.Combine(_workDir.FullName, "staged");

        var (exitCode, stdOut, _) = RunTool("--stage-pdbs", manifest, output, "@" + candidates);

        Assert.Equal(2, exitCode);
        Assert.Contains($"not uploading a Portable PDB for {SelfName}", stdOut);
        Assert.Empty(Directory.EnumerateFiles(output));
    }

    [Fact]
    public void StageMode_HonoursTheRestrictionResponseFile()
    {
        var manifest = WriteManifest(new() { [SelfName] = DebugIdReader.TryReadDebugId(SelfDll)! });
        var candidates = WriteFile("candidates.rsp", PlaceCandidate(SelfName + ".pdb", SelfPdb));
        var restriction = WriteFile("restriction.rsp", Path.Combine(_workDir.FullName, "bin", "Other.pdb"));
        var output = Path.Combine(_workDir.FullName, "staged");

        var (exitCode, _, _) = RunTool("--stage-pdbs", manifest, output, "@" + candidates, "@" + restriction);

        Assert.Equal(0, exitCode);
        Assert.Empty(Directory.EnumerateFiles(output));
    }

    [Fact]
    public void StageMode_ReportsAMissingManifestInOneLineAndExitsOne()
    {
        var candidates = WriteFile("candidates.rsp", PlaceCandidate(SelfName + ".pdb", SelfPdb));

        var (exitCode, _, stdErr) = RunTool("--stage-pdbs", Path.Combine(_workDir.FullName, "missing.json"), Path.Combine(_workDir.FullName, "staged"), "@" + candidates);

        Assert.Equal(1, exitCode);
        Assert.Contains("staging Portable PDBs failed", stdErr);
        Assert.DoesNotContain("   at ", stdErr);
    }

    // Arguments are "|"-separated (attribute arguments can't be string arrays here).
    [Theory]
    [InlineData("--stage-pdbs")]
    [InlineData("--stage-pdbs|manifest.json|out")]
    [InlineData("--stage-pdbs|manifest.json|out|candidates.rsp")]
    [InlineData("--stage-pdbs|manifest.json|out|@candidates.rsp|restriction.rsp")]
    public void StageMode_RejectsMalformedArguments(string args)
    {
        Assert.Equal(1, RunTool(args.Split('|')).ExitCode);
    }
}
