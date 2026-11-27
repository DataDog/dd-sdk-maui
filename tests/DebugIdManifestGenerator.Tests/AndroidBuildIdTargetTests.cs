/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.Diagnostics;
using Xunit;

namespace DebugIdManifestGenerator.Tests;

/// <summary>
/// Runs the Android build ID targets (Datadog.Maui.AndroidBuildId.targets) and the mapping upload
/// against a fake `datadog-ci`, with a stand-in for packaging that zips the generated
/// `datadog.buildId` into the published APK, to pin when an ID ships and how the mapping is
/// uploaded with it (RUM-20515).
/// </summary>
public sealed class AndroidBuildIdTargetTests : IDisposable
{
    private static readonly string TargetsFile = FindTargetsFile();
    private static readonly TimeSpan BuildTimeout = TimeSpan.FromMinutes(3);
    private const string Uploaded = "✅ Uploaded symbol 1 file in 0.5 seconds.";
    private static readonly System.Text.RegularExpressions.Regex Uuid =
        new("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$");

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

    private const string OtherBuildId = "11111111-2222-3333-4444-555555555555";

    private sealed record Result(int ExitCode, string Output, string[]? JvmArgs, string[]? FlutterArgs, string Root, string PackageName)
    {
        public string? GeneratedBuildId => ReadId(Path.Combine(Root, "obj", "datadog.buildId"));
        public string? PublishedBuildId => ReadId(Path.Combine(Root, "publish", "datadog.buildId"));
        /// <summary>The build ID in the built APK, null if it has none.</summary>
        public string? BuiltApkBuildId => ReadPackagedId(Path.Combine(Root, "bin", PackageName + "-Signed.apk"), "assets/datadog.buildId");
        /// <summary>How many times the R8 stand-in has run in this test's folder.</summary>
        public int R8Runs => File.Exists(Path.Combine(Root, "obj", "r8-runs.txt")) ? File.ReadAllLines(Path.Combine(Root, "obj", "r8-runs.txt")).Length : 0;

        private static string? ReadId(string path) => File.Exists(path) ? File.ReadAllText(path).Trim() : null;
    }

    private static string? ReadPackagedId(string package, string entryName)
    {
        if (!File.Exists(package))
        {
            return null;
        }
        using var zip = System.IO.Compression.ZipFile.OpenRead(package);
        var entry = zip.GetEntry(entryName);
        if (entry == null)
        {
            return null;
        }
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd().Trim();
    }

    private static void WritePackage(string root, string package, string entryName, string? buildId)
    {
        var content = Path.Combine(root, "pkg-" + Path.GetFileName(package));
        Directory.CreateDirectory(content);
        File.WriteAllText(Path.Combine(content, "classes.dex"), "dex\n");
        if (buildId != null)
        {
            var entry = Path.Combine(content, entryName);
            Directory.CreateDirectory(Path.GetDirectoryName(entry)!);
            File.WriteAllText(entry, buildId + "\n");
        }
        File.Delete(package);
        System.IO.Compression.ZipFile.CreateFromDirectory(content, package);
        Directory.Delete(content, recursive: true);
    }

    /// <param name="publishing">Whether `_IsPublishing` is set, as `dotnet publish` does.</param>
    /// <param name="hasMappingCommand">Whether the fake datadog-ci has `jvm-mapping-symbols`.</param>
    /// <param name="cliInstalled">Whether datadog-ci is on PATH at all.</param>
    /// <param name="applicationId">The app's ApplicationId, which names its packages; null leaves it unset.</param>
    /// <param name="androidPackage">The package name the Android SDK resolved (from the manifest), which then names the packages instead.</param>
    /// <param name="formats">The packages that packaging produces: "apk", "aab", or both (the Release default).</param>
    /// <param name="perAbi">Whether the APK is named like a per-ABI one.</param>
    /// <param name="packagedBuildId">What packaging puts in the packages; by default the generated ID.</param>
    /// <param name="packageWithoutBuildId">Whether packaging leaves the build ID out of the packages.</param>
    /// <param name="otherAppBuildId">If set, another app's APK, with this build ID, sits in the publish folder.</param>
    /// <param name="fakeR8">Whether a stand-in for R8 writes the mapping file, skipped like R8 while its stamp in obj/ is up to date.</param>
    private Result Publish(
        bool publishing = true,
        string linkTool = "r8",
        bool createMappingFile = true,
        bool embedAssemblies = true,
        string useMonoRuntime = "",
        bool uploadSymbols = true,
        bool hasMappingCommand = true,
        bool cliInstalled = true,
        bool apiKey = true,
        bool mappingFile = true,
        string? customMappingFile = null,
        string? applicationId = "com.example.app",
        string? androidPackage = null,
        string[]? formats = null,
        bool perAbi = false,
        string? packagedBuildId = null,
        bool packageWithoutBuildId = false,
        bool package = true,
        bool publishCopiesPackage = true,
        string? stalePublishedBuildId = null,
        string? otherAppBuildId = null,
        bool failOnError = false,
        bool fakeR8 = false,
        string uploadOutput = Uploaded)
    {
        formats ??= new[] { "apk", "aab" };
        var packageName = androidPackage ?? applicationId ?? "App";
        var root = _workDir.FullName;
        var bin = Directory.CreateDirectory(Path.Combine(root, "bin")).FullName;
        Directory.CreateDirectory(Path.Combine(root, "obj"));
        var publish = Directory.CreateDirectory(Path.Combine(root, "publish")).FullName;
        var mapping = customMappingFile != null ? Path.Combine(root, customMappingFile) : Path.Combine(bin, "mapping.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(mapping)!);
        if (fakeR8)
        {
            // R8's input: older than the stamp R8 leaves, so R8 is up to date after its first run.
            var r8Input = Path.Combine(root, "obj", "classes.zip");
            if (!File.Exists(r8Input))
            {
                File.WriteAllText(r8Input, "classes\n");
                File.SetLastWriteTimeUtc(r8Input, DateTime.UtcNow.AddHours(-1));
            }
        }
        else if (mappingFile)
        {
            File.WriteAllText(mapping, "# R8 mapping\n");
        }
        else
        {
            File.Delete(mapping);
        }
        if (stalePublishedBuildId != null)
        {
            // An APK from an earlier publish, with an earlier build ID.
            WritePackage(root, Path.Combine(publish, packageName + "-Signed.apk"), "assets/datadog.buildId", stalePublishedBuildId);
        }
        if (otherAppBuildId != null)
        {
            WritePackage(root, Path.Combine(publish, "com.other.app-Signed.apk"), "assets/datadog.buildId", otherAppBuildId);
        }

        var fakeBin = Directory.CreateDirectory(Path.Combine(root, "fakebin")).FullName;
        var jvmArgsFile = Path.Combine(root, "jvm-args.txt");
        var flutterArgsFile = Path.Combine(root, "flutter-args.txt");
        File.Delete(jvmArgsFile);
        File.Delete(flutterArgsFile);
        var outputFile = Path.Combine(root, "upload-output.txt");
        File.WriteAllText(outputFile, uploadOutput + "\n");
        var fakeCli = Path.Combine(fakeBin, "datadog-ci");
        File.Delete(fakeCli);
        if (cliInstalled)
        {
            File.WriteAllText(fakeCli,
                "#!/bin/sh\n" +
                "if [ \"$1\" = version ]; then echo v0.0.0; exit 0; fi\n" +
                "if [ \"$1\" = jvm-mapping-symbols ]; then\n" +
                (hasMappingCommand ? "" : "  echo 'Unknown Syntax Error: Command not found'; exit 1\n") +
                "  if [ \"$3\" = --help ]; then exit 0; fi\n" +
                $"  printf '%s\\n' \"$@\" > '{jvmArgsFile}'\n" +
                $"  cat '{outputFile}'; exit 0\n" +
                "fi\n" +
                "if [ \"$1\" = flutter-symbols ]; then\n" +
                $"  printf '%s\\n' \"$@\" > '{flutterArgsFile}'\n" +
                $"  cat '{outputFile}'; exit 0\n" +
                "fi\n" +
                "exit 0\n");
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(fakeCli, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }

        // Stand-in for Android packaging: the listed asset goes into the packages, which are
        // rebuilt from scratch each time (as Android regenerates the app's assets on each build).
        var packageTasks = new System.Text.StringBuilder();
        foreach (var format in formats)
        {
            var assets = format == "aab" ? @"base\assets" : "assets";
            var content = $@"obj\{format}-content";
            packageTasks.Append($"""
                    <RemoveDir Directories="{content}" />
                    <MakeDir Directories="{content}\{assets}" />
                    <Copy SourceFiles="@(AndroidAsset)" DestinationFolder="{content}\{assets}" Condition="'{packagedBuildId}' == '' AND '{packageWithoutBuildId}' != 'True'" />
                    <WriteLinesToFile File="{content}\{assets}\datadog.buildId" Lines="{packagedBuildId}" Overwrite="true" Condition="'{packagedBuildId}' != ''" />
                    <WriteLinesToFile File="{content}\classes.dex" Lines="dex" Overwrite="true" />
                    <ZipDirectory SourceDirectory="{content}" DestinationFile="bin\{packageName}{(perAbi && format == "apk" ? "-arm64-v8a" : "")}-Signed.{format}" Overwrite="true" />
                    <Copy SourceFiles="bin\{packageName}{(perAbi && format == "apk" ? "-arm64-v8a" : "")}-Signed.{format}" DestinationFolder="publish" Condition="'{publishCopiesPackage}' == 'True'" />

                """);
        }

        var project = Path.Combine(root, "app.proj");
        File.WriteAllText(project, $"""
            <Project>
              <PropertyGroup>
                <TargetFramework>net10.0-android</TargetFramework>
                <AndroidApplication>true</AndroidApplication>
                <AssemblyName>App</AssemblyName>
                {(applicationId != null ? $"<ApplicationId>{applicationId}</ApplicationId>" : "")}
                {(androidPackage != null ? $"<_AndroidPackage>{androidPackage}</_AndroidPackage>" : "")}
                <OutputPath>bin\</OutputPath>
                <PublishDir>publish\</PublishDir>
                <IntermediateOutputPath>obj\</IntermediateOutputPath>
                <ApplicationDisplayVersion>1.0.0</ApplicationDisplayVersion>
                <DatadogUploadSymbols>{(uploadSymbols ? "true" : "false")}</DatadogUploadSymbols>
                <DatadogFailOnSymbolUploadError>{(failOnError ? "true" : "false")}</DatadogFailOnSymbolUploadError>
                <_IsPublishing>{(publishing ? "true" : "")}</_IsPublishing>
                <EmbedAssembliesIntoApk>{(embedAssemblies ? "true" : "false")}</EmbedAssembliesIntoApk>
                <UseMonoRuntime>{useMonoRuntime}</UseMonoRuntime>
                <AndroidLinkTool>{linkTool}</AndroidLinkTool>
                <AndroidCreateProguardMappingFile>{(createMappingFile ? "True" : "False")}</AndroidCreateProguardMappingFile>
                {(customMappingFile != null ? $"<AndroidProguardMappingFile>{customMappingFile}</AndroidProguardMappingFile>" : "")}
              </PropertyGroup>
              <Import Project="{TargetsFile}" />
              <Target Name="BeforeBuild" />
              <PropertyGroup Condition="'{fakeR8}' == 'True'">
                <_AndroidStampDirectory>obj\stamp\</_AndroidStampDirectory>
                <AndroidProguardMappingFile>bin\mapping.txt</AndroidProguardMappingFile>
              </PropertyGroup>
              <!-- Stand-in for R8, with the same up-to-date check: its stamp, not the mapping. -->
              <Target Name="_CompileToDalvik" Inputs="obj\classes.zip" Outputs="obj\stamp\_CompileToDalvik.stamp" Condition="'{fakeR8}' == 'True'">
                <WriteLinesToFile File="bin\mapping.txt" Lines="# R8 mapping" Overwrite="true" />
                <WriteLinesToFile File="obj\r8-runs.txt" Lines="ran" />
                <MakeDir Directories="obj\stamp" />
                <Touch Files="obj\stamp\_CompileToDalvik.stamp" AlwaysCreate="true" />
              </Target>
              <Target Name="_TestPackage" Condition="'{package}' == 'True'">
            {packageTasks}
                <!-- Above, publishing copies the packages, unless (like Android's same-size,
                     fixed-timestamp packages) it decides the copies already there are unchanged. -->
              </Target>
            </Project>
            """);

        var startInfo = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var arg in new[] { "msbuild", project, "-nologo", "-v:n", "-t:BeforeBuild;_CompileToDalvik;_TestPackage;DatadogUploadMapping;DatadogSymbolReport" })
        {
            startInfo.ArgumentList.Add(arg);
        }
        foreach (var name in startInfo.Environment.Keys.Where(k => k.StartsWith("DD_", StringComparison.Ordinal) || k.StartsWith("DATADOG_", StringComparison.Ordinal)).ToList())
        {
            startInfo.Environment.Remove(name);
        }
        // Only the fake (or nothing) can be found as datadog-ci.
        startInfo.Environment["PATH"] = fakeBin + Path.PathSeparator + "/usr/bin" + Path.PathSeparator + "/bin";
        if (apiKey)
        {
            startInfo.Environment["DATADOG_API_KEY"] = "fake-key-for-tests";
        }

        using var process = Process.Start(startInfo)!;
        var stdOut = process.StandardOutput.ReadToEndAsync();
        var stdErr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(BuildTimeout))
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail($"msbuild did not finish within {BuildTimeout}");
        }
        process.WaitForExit();
        return new Result(
            process.ExitCode,
            stdOut.Result + stdErr.Result,
            File.Exists(jvmArgsFile) ? File.ReadAllLines(jvmArgsFile) : null,
            File.Exists(flutterArgsFile) ? File.ReadAllLines(flutterArgsFile) : null,
            root,
            packageName);
    }

    [UnixFact]
    public void ShipsABuildIdAndUploadsTheMappingKeyedByIt()
    {
        var result = Publish();

        Assert.Equal(0, result.ExitCode);
        Assert.Matches(Uuid, result.GeneratedBuildId!);
        Assert.Equal(result.GeneratedBuildId, result.BuiltApkBuildId);
        Assert.Equal(result.GeneratedBuildId, result.PublishedBuildId);
        Assert.Equal(new[]
        {
            "jvm-mapping-symbols", "upload", "bin/mapping.txt",
            "--build-id", result.GeneratedBuildId!,
            "--service-name", "App", "--version", "1.0.0",
        }, result.JvmArgs);
        Assert.Null(result.FlutterArgs);
        Assert.Contains($"Android build ID: {result.GeneratedBuildId} — copy at publish/datadog.buildId", result.Output);
        Assert.Contains("Android mapping: uploaded, keyed by build ID", result.Output);
        Assert.DoesNotContain("symbol uploads FAILED", result.Output);
    }

    [UnixFact]
    public void GeneratesANewBuildIdOnEachPublish()
    {
        var first = Publish().GeneratedBuildId;
        var second = Publish().GeneratedBuildId;

        Assert.NotEqual(first, second);
    }

    [UnixTheory]
    [InlineData("notPublishing", "not a dotnet publish")]
    [InlineData("notMono", "the app doesn't use the Mono runtime")]
    [InlineData("noR8", "R8 doesn't produce a mapping file")]
    [InlineData("noMappingFile", "R8 doesn't produce a mapping file")]
    [InlineData("notEmbedded", "the app doesn't embed its assemblies")]
    [InlineData("noApiKey", "DATADOG_API_KEY is not set")]
    [InlineData("oldCli", "the installed datadog-ci has no jvm-mapping-symbols command; upgrade it")]
    [InlineData("noCli", "datadog-ci is not installed")]
    public void ShipsNoBuildIdWhenTheMappingCannotBeUploadedWithIt(string scenario, string reason)
    {
        var result = Publish(
            publishing: scenario != "notPublishing",
            useMonoRuntime: scenario == "notMono" ? "false" : "",
            linkTool: scenario == "noR8" ? "" : "r8",
            createMappingFile: scenario != "noMappingFile",
            embedAssemblies: scenario != "notEmbedded",
            apiKey: scenario != "noApiKey",
            hasMappingCommand: scenario != "oldCli",
            cliInstalled: scenario != "noCli");

        Assert.Null(result.GeneratedBuildId);
        Assert.Null(result.BuiltApkBuildId);
        Assert.Null(result.PublishedBuildId);
        Assert.Null(result.JvmArgs);
        Assert.Contains($"not shipped ({reason}", result.Output);
    }

    [UnixFact]
    public void SaysNoMappingIsUploadedWhenR8WritesNone()
    {
        var result = Publish(createMappingFile: false);

        Assert.Contains("not shipping an Android build ID (R8 doesn't produce a mapping file); there's no mapping file to upload.", result.Output);
    }

    [UnixFact]
    public void SaysOnlyAPublishUploadsWhenTheAppIsNotOnMono()
    {
        var result = Publish(useMonoRuntime: "false");

        Assert.Contains("not shipping an Android build ID (the app doesn't use the Mono runtime); a publish uploads the mapping file by service and version.", result.Output);
    }

    [UnixFact]
    public void UploadsTheMappingByServiceAndVersionWithTheAppsEmptyVariantWithoutABuildId()
    {
        var result = Publish(hasMappingCommand: false);

        Assert.Equal(new[]
        {
            "flutter-symbols", "upload", "--android-mapping",
            "--android-mapping-location", "bin/mapping.txt",
            "--service-name", "App", "--version", "1.0.0", "--flavor", "",
        }, result.FlutterArgs);
        Assert.Contains("the mapping file is uploaded by service and version.", result.Output);
        Assert.Contains("Android mapping: uploaded, keyed by service and version", result.Output);
        Assert.DoesNotContain("Android build ID: FAILED", result.Output);
        Assert.DoesNotContain("symbol uploads FAILED", result.Output);
    }

    [UnixFact]
    public void ShipsNoBuildIdWhenSymbolUploadsAreOff()
    {
        var result = Publish(uploadSymbols: false);

        Assert.Null(result.GeneratedBuildId);
        Assert.Null(result.BuiltApkBuildId);
        Assert.Null(result.JvmArgs);
        Assert.Null(result.FlutterArgs);
    }

    [UnixFact]
    public void RemovesABuildIdLeftByAnEarlierBuildWhenNoneShips()
    {
        var first = Publish();
        Assert.NotNull(first.GeneratedBuildId);
        Assert.NotNull(first.PublishedBuildId);

        var result = Publish(hasMappingCommand: false);

        Assert.Null(result.GeneratedBuildId);
        Assert.Null(result.BuiltApkBuildId);
        // The copy next to the published app can't be mistaken for this build's.
        Assert.Null(result.PublishedBuildId);
    }

    [UnixFact]
    public void ReadsTheBuildIdFromTheAabWhenOnlyAnAabIsPublished()
    {
        var result = Publish(formats: new[] { "aab" });

        Assert.Matches(Uuid, result.GeneratedBuildId!);
        Assert.Equal(result.GeneratedBuildId, ReadPackagedId(Path.Combine(result.Root, "publish", "com.example.app-Signed.aab"), "base/assets/datadog.buildId"));
        Assert.DoesNotContain("Android build ID: FAILED", result.Output);
        Assert.DoesNotContain("symbol uploads FAILED", result.Output);
    }

    [UnixFact]
    public void FailsWhenThePackagedAppCarriesAnotherBuildId()
    {
        var result = Publish(packagedBuildId: OtherBuildId);

        Assert.Contains($"-Signed.apk has build ID {OtherBuildId}, not this build's)", result.Output);
        Assert.Contains("warning : Datadog: one or more symbol uploads FAILED", result.Output);
        // A package that doesn't carry this build's ID isn't this build's: the mapping is
        // uploaded with the generated ID, and the copy kept next to the app is that ID too.
        Assert.Contains(result.GeneratedBuildId!, result.JvmArgs!);
        Assert.Equal(result.GeneratedBuildId, result.PublishedBuildId);
    }

    [UnixFact]
    public void FailsWhenThePackagedAppHasNoBuildId()
    {
        var result = Publish(packageWithoutBuildId: true);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("-Signed.apk has no build ID)", result.Output);
        Assert.Contains("warning : Datadog: one or more symbol uploads FAILED", result.Output);
    }

    [UnixFact]
    public void FailsTheBuildOnABuildIdProblemWithFailOnSymbolUploadError()
    {
        var result = Publish(packageWithoutBuildId: true, failOnError: true);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("error : Datadog: one or more symbol uploads FAILED", result.Output);
    }

    [UnixFact]
    public void ChecksTheAppsOwnPackageWhenAnotherAppsPackageIsPublishedAlongside()
    {
        var result = Publish(otherAppBuildId: OtherBuildId);

        Assert.Matches(Uuid, result.GeneratedBuildId!);
        Assert.DoesNotContain("Android build ID: FAILED", result.Output);
        Assert.DoesNotContain("symbol uploads FAILED", result.Output);
    }

    [UnixTheory]
    [InlineData("com.example.app")]
    [InlineData(null)]
    public void ChecksThePackageNamedAfterThePackageNameTheAndroidSdkResolved(string? applicationId)
    {
        var result = Publish(applicationId: applicationId, androidPackage: "com.from.manifest", otherAppBuildId: OtherBuildId);

        Assert.Matches(Uuid, result.GeneratedBuildId!);
        Assert.Equal(result.GeneratedBuildId, result.BuiltApkBuildId);
        Assert.Contains("com.from.manifest", result.Output);
        Assert.DoesNotContain("Android build ID: FAILED", result.Output);
    }

    [UnixFact]
    public void ChecksAPerAbiApk()
    {
        var result = Publish(formats: new[] { "apk" }, perAbi: true, packagedBuildId: OtherBuildId);

        Assert.Contains($"com.example.app-arm64-v8a-Signed.apk has build ID {OtherBuildId}, not this build's)", result.Output);
    }

    [UnixFact]
    public void FailsWhenAPackageCarriesABuildIdThatThisBuildDidNotShip()
    {
        var result = Publish(hasMappingCommand: false, packagedBuildId: OtherBuildId);

        Assert.Contains($"carries build ID {OtherBuildId} from an earlier build", result.Output);
        Assert.Contains("warning : Datadog: one or more symbol uploads FAILED", result.Output);
    }

    [UnixFact]
    public void KeepsThePublishedCopyOnAPlainBuild()
    {
        var publishedId = Publish().GeneratedBuildId;
        Assert.NotNull(publishedId);

        var result = Publish(publishing: false);

        Assert.Null(result.GeneratedBuildId);
        Assert.Equal(publishedId, result.PublishedBuildId);
    }

    [UnixFact]
    public void RefreshesAPublishedPackageLeftFromAnEarlierPublish()
    {
        var result = Publish(publishCopiesPackage: false, stalePublishedBuildId: OtherBuildId);

        Assert.Matches(Uuid, result.GeneratedBuildId!);
        Assert.Equal(result.GeneratedBuildId, ReadPackagedId(Path.Combine(result.Root, "publish", "com.example.app-Signed.apk"), "assets/datadog.buildId"));
        Assert.Equal(result.GeneratedBuildId, result.PublishedBuildId);
        Assert.DoesNotContain("Android build ID: FAILED", result.Output);
        Assert.Contains("Android mapping: uploaded, keyed by build ID", result.Output);
    }

    [UnixFact]
    public void FailsWhenNoPackagedAppIsFound()
    {
        var result = Publish(package: false);

        Assert.Contains("Android build ID: FAILED (the app's package wasn't found in publish/)", result.Output);
        Assert.Contains("warning : Datadog: one or more symbol uploads FAILED", result.Output);
    }

    [UnixFact]
    public void FailsWhenTheMappingFileIsMissingButABuildIdShips()
    {
        var result = Publish(mappingFile: false);

        Assert.NotNull(result.GeneratedBuildId);
        Assert.Null(result.JvmArgs);
        Assert.Contains($"Android mapping: FAILED (mapping.txt not found at bin/mapping.txt, but the app ships build ID {result.GeneratedBuildId}), keyed by build ID", result.Output);
        Assert.Contains("warning : Datadog: one or more symbol uploads FAILED", result.Output);
    }

    [UnixFact]
    // The Android SDK sets AndroidProguardMappingFile before R8 runs (today always to the build
    // output); the upload follows it rather than assuming that path.
    public void UploadsTheMappingFromWhereTheAndroidSdkHasR8WriteIt()
    {
        var result = Publish(customMappingFile: "maps/custom.txt");

        Assert.Equal("maps/custom.txt", result.JvmArgs![2]);
        Assert.Contains("Android mapping: uploaded, keyed by build ID — maps/custom.txt", result.Output);
        Assert.DoesNotContain("symbol uploads FAILED", result.Output);
    }

    [UnixFact]
    public void RunsR8AgainWhenTheMappingFileItWroteIsGone()
    {
        Assert.Contains("Android mapping: uploaded, keyed by build ID", Publish(fakeR8: true).Output);
        var upToDate = Publish(fakeR8: true);
        Assert.Equal(1, upToDate.R8Runs);
        Assert.DoesNotContain("running R8 again", upToDate.Output);

        // As when bin/ is deleted: R8's outputs in obj/ are still up to date.
        File.Delete(Path.Combine(upToDate.Root, "bin", "mapping.txt"));
        var result = Publish(fakeR8: true);

        Assert.Equal(2, result.R8Runs);
        Assert.Contains("Datadog: bin/mapping.txt is missing; running R8 again to write it.", result.Output);
        Assert.Contains("Android mapping: uploaded, keyed by build ID", result.Output);
        Assert.DoesNotContain("symbol uploads FAILED", result.Output);
    }

    [UnixFact]
    public void DoesNotRunR8AgainWhenNoBuildIdShips()
    {
        var first = Publish(fakeR8: true, hasMappingCommand: false);
        File.Delete(Path.Combine(first.Root, "bin", "mapping.txt"));

        var result = Publish(fakeR8: true, hasMappingCommand: false);

        Assert.Equal(1, result.R8Runs);
        Assert.Contains("Android mapping: not run — bin/mapping.txt", result.Output);
    }

    [UnixFact]
    public void ReportsAFailedMappingUploadWithABuildId()
    {
        var result = Publish(uploadOutput: "❌ Some symbol files may not been uploaded correctly.");

        Assert.Contains("Android mapping: FAILED (the mapping upload failed), keyed by build ID", result.Output);
        Assert.Contains("warning : Datadog: one or more symbol uploads FAILED", result.Output);
    }
}
