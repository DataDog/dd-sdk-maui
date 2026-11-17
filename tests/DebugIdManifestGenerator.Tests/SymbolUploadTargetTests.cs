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

/// <summary>A <see cref="FactAttribute"/> skipped on Windows, where the fake datadog-ci (a shell script) can't run.</summary>
public sealed class UnixFactAttribute : FactAttribute
{
    public UnixFactAttribute()
    {
        if (OperatingSystem.IsWindows())
        {
            Skip = "Uses a shell-script fake of datadog-ci.";
        }
    }
}

/// <summary>A <see cref="TheoryAttribute"/> skipped on Windows, where the fake datadog-ci (a shell script) can't run.</summary>
public sealed class UnixTheoryAttribute : TheoryAttribute
{
    public UnixTheoryAttribute()
    {
        if (OperatingSystem.IsWindows())
        {
            Skip = "Uses a shell-script fake of datadog-ci.";
        }
    }
}

/// <summary>
/// Runs the real upload targets from Datadog.Maui.targets (DatadogUploadDsym,
/// DatadogUploadMapping, DatadogUploadPortablePdb, DatadogSymbolReport) against a fake
/// `datadog-ci` that records its arguments and prints canned output, to pin how the commands
/// are built and how their output is reported (dd-sdk-maui#97: datadog-ci exits 0 even when
/// it uploads nothing).
/// </summary>
public sealed class SymbolUploadTargetTests : IDisposable
{
    private static readonly string SelfDll = typeof(SymbolUploadTargetTests).Assembly.Location;
    private static readonly string SelfPdb = Path.ChangeExtension(SelfDll, ".pdb");
    private static readonly string SelfName = Path.GetFileNameWithoutExtension(SelfDll);
    private static readonly string GeneratorDll = typeof(PdbStager).Assembly.Location;
    private static readonly string GeneratorPdb = Path.ChangeExtension(GeneratorDll, ".pdb");
    private static readonly string TargetsFile = FindTargetsFile();

    // Placeholder the fake datadog-ci replaces with the number of .pdb files it finds under the
    // folder it's given, searched recursively like the real one (**/*.pdb).
    private const string FoundCount = "__FOUND_COUNT__";

    private const string AllUploaded = "✅ Uploaded symbol 1 file in 0.629 seconds.";
    private const string MappingUploaded = "Mapping upload finished\n✅ Uploaded symbol 1 file in 0.5 seconds.";
    private const string DsymUploaded = "✅ Uploaded 1 dSYM in 0.7 seconds.";

    // What the real datadog-ci prints when the build doesn't run inside a git repository.
    private const string GitWarning =
        "⚠️ An error occurred while invoking git: Error: fatal: not a git repository (or any of the parent directories): .git\n" +
        "Make sure the command is running within your git repository to fully leverage Datadog's git integration.\n" +
        "To ignore this warning use the --disable-git flag.";

    // An MSBuild run that hangs (e.g. waiting on a lock) fails the test instead of blocking the suite.
    private static readonly TimeSpan BuildTimeout = TimeSpan.FromMinutes(3);

    private readonly DirectoryInfo _workDir = Directory.CreateTempSubdirectory();

    public void Dispose() => _workDir.Delete(recursive: true);

    private static string FindTargetsFile()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "bindings", "Datadog.Maui", "build", "Datadog.Maui.targets");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }
        throw new FileNotFoundException("Datadog.Maui.targets not found above " + AppContext.BaseDirectory);
    }

    private sealed record Result(int ExitCode, string Output, string[] PpdbCliArgs);

    // Lays out a minimal Android publish (mapping.txt and a PDB in bin/, the matching PDB in
    // obj/ and its debug-id manifest) — or, with `ios`, an iOS one (the .app.dSYM next to the
    // publish folder instead of mapping.txt) — then runs the targets with the fake datadog-ci
    // first in PATH. Each `…Output` is what the matching datadog-ci command prints.
    private Result Publish(
        string ppdbOutput,
        int ppdbExitCode = 0,
        string mappingOutput = MappingUploaded,
        int mappingExitCode = 0,
        string dsymOutput = DsymUploaded,
        int dsymExitCode = 0,
        bool ios = false,
        bool failOnError = false,
        Action<string>? beforeBuild = null)
    {
        var root = _workDir.FullName;
        var bin = Directory.CreateDirectory(Path.Combine(root, "bin")).FullName;
        var obj = Directory.CreateDirectory(Path.Combine(root, "obj")).FullName;
        File.Copy(SelfPdb, Path.Combine(bin, SelfName + ".pdb"), overwrite: true);
        if (ios)
        {
            Directory.CreateDirectory(Path.Combine(root, "publish", "App.app.dSYM"));
        }
        else
        {
            File.WriteAllText(Path.Combine(bin, "mapping.txt"), "# R8 mapping\n");
        }
        File.Copy(SelfPdb, Path.Combine(obj, SelfName + ".pdb"), overwrite: true);
        File.WriteAllText(Path.Combine(obj, "dd_debug_ids.json"),
            JsonSerializer.Serialize(new Dictionary<string, string> { [SelfName] = DebugIdReader.TryReadDebugId(SelfDll)! }));
        beforeBuild?.Invoke(root);

        var fakeBin = Directory.CreateDirectory(Path.Combine(root, "fakebin")).FullName;
        var argsFile = Path.Combine(root, "ppdb-args.txt");
        var ppdbOutputFile = Path.Combine(root, "ppdb-output.txt");
        var mappingOutputFile = Path.Combine(root, "mapping-output.txt");
        var dsymOutputFile = Path.Combine(root, "dsym-output.txt");
        File.WriteAllText(ppdbOutputFile, ppdbOutput + "\n");
        File.WriteAllText(mappingOutputFile, mappingOutput + "\n");
        File.WriteAllText(dsymOutputFile, dsymOutput + "\n");
        var fakeCli = Path.Combine(fakeBin, "datadog-ci");
        File.WriteAllText(fakeCli,
            "#!/bin/sh\n" +
            "if [ \"$1\" = ppdb-symbols ]; then\n" +
            $"  printf '%s\\n' \"$@\" > '{argsFile}'\n" +
            "  found=$(find \"$3\" -name '*.pdb' 2>/dev/null | wc -l | tr -d ' ')\n" +
            $"  sed \"s/{FoundCount}/$found/\" '{ppdbOutputFile}'\n" +
            $"  exit {ppdbExitCode}\n" +
            "fi\n" +
            "if [ \"$1\" = dsyms ]; then\n" +
            $"  cat '{dsymOutputFile}'\n" +
            $"  exit {dsymExitCode}\n" +
            "fi\n" +
            $"cat '{mappingOutputFile}'\n" +
            $"exit {mappingExitCode}\n");
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(fakeCli, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        // Backslashes, as MSBuild produces them on Windows.
        var project = Path.Combine(root, "app.proj");
        File.WriteAllText(project, $"""
            <Project>
              <PropertyGroup>
                <TargetFramework>{(ios ? "net10.0-ios" : "net10.0-android")}</TargetFramework>
                <AssemblyName>App</AssemblyName>
                <OutputPath>bin\</OutputPath>
                <PublishDir>publish\App\</PublishDir>
                <IntermediateOutputPath>obj\</IntermediateOutputPath>
                <ApplicationDisplayVersion>1.0.0</ApplicationDisplayVersion>
                <DatadogUploadSymbols>true</DatadogUploadSymbols>
                <DatadogFailOnSymbolUploadError>{(failOnError ? "true" : "false")}</DatadogFailOnSymbolUploadError>
              </PropertyGroup>
              <Import Project="{TargetsFile}" />
              <PropertyGroup>
                <_DdDebugIdManifestGeneratorDll>{GeneratorDll}</_DdDebugIdManifestGeneratorDll>
              </PropertyGroup>
            </Project>
            """);

        var startInfo = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        var targets = ios ? "DatadogUploadDsym;DatadogUploadPortablePdb;DatadogSymbolReport" : "DatadogUploadMapping;DatadogUploadPortablePdb;DatadogSymbolReport";
        foreach (var arg in new[] { "msbuild", project, "-nologo", "-v:n", "-t:" + targets })
        {
            startInfo.ArgumentList.Add(arg);
        }
        // Keep the developer's own Datadog settings out of the build.
        foreach (var name in startInfo.Environment.Keys.Where(k => k.StartsWith("DD_", StringComparison.Ordinal) || k.StartsWith("DATADOG_", StringComparison.Ordinal)).ToList())
        {
            startInfo.Environment.Remove(name);
        }
        startInfo.Environment["PATH"] = fakeBin + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");
        startInfo.Environment["DATADOG_API_KEY"] = "fake-key-for-tests";

        using var process = Process.Start(startInfo)!;
        var stdOut = process.StandardOutput.ReadToEndAsync();
        var stdErr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(BuildTimeout))
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail($"msbuild did not finish within {BuildTimeout}");
        }
        process.WaitForExit();
        var cliArgs = File.Exists(argsFile) ? File.ReadAllLines(argsFile) : Array.Empty<string>();
        return new Result(process.ExitCode, stdOut.Result + stdErr.Result, cliArgs);
    }

    // Only pins the shape of the command: on macOS and Linux, Exec already turns backslashes
    // into forward slashes, so this can't tell whether the targets convert them for Windows.
    [UnixFact]
    public void PassesTheStagingFolderAndManifestToPpdbSymbolsUpload()
    {
        var result = Publish(AllUploaded);

        Assert.Equal(new[] { "ppdb-symbols", "upload", "obj/dd_ppdb_upload", "--debug-id-manifest", "obj/dd_debug_ids.json" }, result.PpdbCliArgs);
    }

    [UnixFact]
    public void ReportsUploadedWhenEveryStagedPdbWasUploaded()
    {
        var result = Publish(AllUploaded);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Portable PDB: uploaded (1 of 1)", result.Output);
        Assert.DoesNotContain("symbol uploads FAILED", result.Output);
    }

    // Each datadog-ci summary that exits 0 without uploading every staged PDB.
    [UnixTheory]
    [InlineData("⚠️ No actions were taken. Did you specify the correct path?", "FAILED (datadog-ci found none of the 1 staged Portable PDB(s) in obj/dd_ppdb_upload (path not matched))")]
    [InlineData("⚠️  Some symbol files have been skipped.", "FAILED (datadog-ci skipped some Portable PDBs)")]
    [InlineData("❌ Some symbol files may not been uploaded correctly.", "FAILED (some Portable PDB uploads failed)")]
    public void WarnsWhenDatadogCiExitsZeroWithoutUploadingEveryPdb(string cliSummary, string expectedStatus)
    {
        var result = Publish(cliSummary);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Portable PDB: " + expectedStatus, result.Output);
        Assert.Contains("warning : Datadog: one or more symbol uploads FAILED", result.Output);
    }

    // datadog-ci searches the folder recursively but the targets count only the PDBs they staged,
    // so a stray PDB in a subfolder makes the counts differ.
    [UnixFact]
    public void WarnsWhenDatadogCiUploadedADifferentNumberOfPdbsThanWereStaged()
    {
        var result = Publish($"✅ Uploaded symbol {FoundCount} files in 0.893 seconds.", beforeBuild: root =>
        {
            var stray = Directory.CreateDirectory(Path.Combine(root, "obj", "dd_ppdb_upload", "old")).FullName;
            File.Copy(GeneratorPdb, Path.Combine(stray, Path.GetFileName(GeneratorPdb)));
        });

        Assert.Contains("Portable PDB: FAILED (datadog-ci uploaded 2 of 1 staged Portable PDBs)", result.Output);
        Assert.Contains("warning : Datadog: one or more symbol uploads FAILED", result.Output);
    }

    [UnixFact]
    public void FailsTheBuildWhenAskedAndNothingWasUploaded()
    {
        var result = Publish("⚠️ No actions were taken. Did you specify the correct path?", failOnError: true);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("error : Datadog: one or more symbol uploads FAILED", result.Output);
    }

    [UnixFact]
    public void WarnsWithoutFailingWhenTheSummaryIsNotRecognized()
    {
        var result = Publish("Some future summary wording.", failOnError: true);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Portable PDB: upload not verified (datadog-ci summary not recognized", result.Output);
        Assert.Contains("warning : Datadog: could not confirm these symbol uploads: Portable PDB.", result.Output);
        Assert.DoesNotContain("symbol uploads FAILED", result.Output);
    }

    [UnixFact]
    public void StillReportsANonZeroExitCodeAsFailed()
    {
        var result = Publish("❌ Error: Invalid API key", ppdbExitCode: 1);

        Assert.Contains("Portable PDB: FAILED (exit 1)", result.Output);
        Assert.Contains("warning : Datadog: one or more symbol uploads FAILED", result.Output);
    }

    // datadog-ci's git warning has the shape of an MSBuild error line; it must not turn a
    // successful upload into a failure (Exec would otherwise report exit -1).
    [UnixFact]
    public void IgnoresDatadogCiGitWarningsWhenTheUploadsSucceeded()
    {
        var result = Publish(GitWarning + "\n" + AllUploaded, mappingOutput: GitWarning + "\n" + MappingUploaded, failOnError: true);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Android mapping: uploaded", result.Output);
        Assert.Contains("Portable PDB: uploaded (1 of 1)", result.Output);
        Assert.DoesNotContain("symbol uploads FAILED", result.Output);
    }

    // Same shape for the metrics-flush warning printed next to failed uploads: the summary, not
    // exit -1, must explain the failure.
    [UnixFact]
    public void ReportsFailedUploadsEvenWhenDatadogCiAlsoPrintsAMetricsWarning()
    {
        var result = Publish("WARN: Error: Could not flush metrics to api.example.invalid: Error: getaddrinfo ENOTFOUND api.example.invalid\n❌ Some symbol files may not been uploaded correctly.");

        Assert.Contains("Portable PDB: FAILED (some Portable PDB uploads failed)", result.Output);
    }

    [UnixFact]
    public void ReportsTheMappingAsUploadedOnlyWhenDatadogCiSaysSo()
    {
        var result = Publish(AllUploaded);

        Assert.Contains("Android mapping: uploaded —", result.Output);
        Assert.DoesNotContain("symbol uploads FAILED", result.Output);
    }

    // What `flutter-symbols upload` prints, exiting 0, when the mapping upload failed after retries.
    [UnixFact]
    public void WarnsWhenTheMappingUploadFailedButDatadogCiExitedZero()
    {
        var result = Publish(AllUploaded, mappingOutput: "❌ Failed upload for [mapping.txt]: Request failed with status code 500\nMapping upload failed\n❌ Some symbol files may not been uploaded correctly.");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Android mapping: FAILED (the mapping upload failed)", result.Output);
        Assert.Contains("warning : Datadog: one or more symbol uploads FAILED", result.Output);
    }

    [UnixFact]
    public void StillReportsANonZeroMappingExitCodeAsFailed()
    {
        var result = Publish(AllUploaded, mappingOutput: "❌ Error: Invalid API key", mappingExitCode: 1);

        Assert.Contains("Android mapping: FAILED (exit 1)", result.Output);
    }

    [UnixFact]
    public void ReportsTheDsymAsUploadedOnlyWhenDatadogCiSaysSo()
    {
        var result = Publish(AllUploaded, ios: true, failOnError: true);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("iOS dSYM: uploaded (1 dSYM(s))", result.Output);
        Assert.Contains("Portable PDB: uploaded (1 of 1)", result.Output);
    }

    // The `dsyms upload` summaries that exit 0 without uploading every dSYM.
    [UnixTheory]
    [InlineData("❌ Failed upload dSYM for [App.app.dSYM]: Request failed with status code 500\n❌ Some dSYMS have not been uploaded correctly.", "FAILED (some dSYM uploads failed)")]
    [InlineData("⚠️  Some dSYMs have been skipped.", "FAILED (datadog-ci skipped some dSYMs)")]
    [InlineData("⚠️ No dSYMs detected. Did you specify the correct directory?", "FAILED (datadog-ci found no dSYM in")]
    public void WarnsWhenTheDsymUploadDidNotHappenButDatadogCiExitedZero(string dsymSummary, string expectedStatus)
    {
        var result = Publish(AllUploaded, dsymOutput: dsymSummary, ios: true);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("iOS dSYM: " + expectedStatus, result.Output);
        Assert.Contains("warning : Datadog: one or more symbol uploads FAILED", result.Output);
    }

    [UnixFact]
    public void IgnoresDatadogCiGitWarningsWhenTheDsymUploadSucceeded()
    {
        var result = Publish(AllUploaded, dsymOutput: GitWarning + "\n" + DsymUploaded, ios: true, failOnError: true);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("iOS dSYM: uploaded (1 dSYM(s))", result.Output);
    }

    [UnixFact]
    public void NamesEveryUploadItCouldNotConfirm()
    {
        var result = Publish("Some future summary wording.", dsymOutput: "Another future wording.", ios: true);

        Assert.Contains("iOS dSYM: upload not verified", result.Output);
        Assert.Contains("warning : Datadog: could not confirm these symbol uploads: iOS dSYM, Portable PDB.", result.Output);
        Assert.DoesNotContain("symbol uploads FAILED", result.Output);
    }

    [UnixFact]
    public void StillReportsANonZeroDsymExitCodeAsFailed()
    {
        var result = Publish(AllUploaded, dsymOutput: "❌ Error: Invalid API key", dsymExitCode: 1, ios: true);

        Assert.Contains("iOS dSYM: FAILED (exit 1)", result.Output);
    }

    // The `flutter-symbols upload` outcomes other than a failed upload: nothing taken, skipped,
    // and an output that has only the per-file failure line.
    [UnixTheory]
    [InlineData("⚠️ No actions were taken. Did you specify the correct path?", "FAILED (datadog-ci uploaded nothing)")]
    [InlineData("⚠️  Some symbol files have been skipped.", "FAILED (datadog-ci skipped the mapping)")]
    [InlineData("Mapping upload failed", "FAILED (the mapping upload failed)")]
    public void WarnsWhenTheMappingWasNotUploadedButDatadogCiExitedZero(string mappingSummary, string expectedStatus)
    {
        var result = Publish(AllUploaded, mappingOutput: mappingSummary);

        Assert.Contains("Android mapping: " + expectedStatus, result.Output);
        Assert.Contains("warning : Datadog: one or more symbol uploads FAILED", result.Output);
    }

    [UnixFact]
    public void ReportsTheMappingAsUnverifiedWhenTheSummaryIsNotRecognized()
    {
        var result = Publish(AllUploaded, mappingOutput: "Some future summary wording.", failOnError: true);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Android mapping: upload not verified", result.Output);
        Assert.Contains("warning : Datadog: could not confirm these symbol uploads: Android mapping.", result.Output);
    }

    [UnixFact]
    public void CountsSeveralUploadedDsyms()
    {
        var result = Publish(AllUploaded, dsymOutput: "✅ Uploaded 2 dSYMs in 1.2 seconds.", ios: true);

        Assert.Contains("iOS dSYM: uploaded (2 dSYM(s))", result.Output);
        Assert.DoesNotContain("could not confirm", result.Output);
    }

    // A non-zero exit is reported from the exit code alone, whatever the output says.
    [UnixFact]
    public void ReadsTheSummaryOnlyWhenDatadogCiExitedZero()
    {
        var result = Publish("✅ Uploaded symbol 1 file in 0.1 seconds.", ppdbExitCode: 1, mappingOutput: "Some future summary wording.", mappingExitCode: 1);

        Assert.Contains("Android mapping: FAILED (exit 1)", result.Output);
        Assert.Contains("Portable PDB: FAILED (exit 1)", result.Output);
        Assert.DoesNotContain("could not confirm", result.Output);
    }
}
