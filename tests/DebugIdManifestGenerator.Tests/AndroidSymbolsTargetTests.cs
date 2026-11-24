using System.Diagnostics;
using Xunit;

namespace DebugIdManifestGenerator.Tests;

public sealed class AndroidSymbolsTargetTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory();
    private string BuildId => Path.Combine(_root.FullName, "obj", "datadog.buildId");
    private string ExportedId => Path.Combine(_root.FullName, "bin", "datadog-android-symbols", "datadog.buildId");
    private string ExportedMapping => Path.Combine(_root.FullName, "bin", "datadog-android-symbols", "mapping.txt");

    public AndroidSymbolsTargetTests()
    {
        var targets = FindTargets();
        File.WriteAllText(Path.Combine(_root.FullName, "app.proj"), $"""
            <Project>
              <PropertyGroup>
                <TargetFramework>net10.0-android</TargetFramework>
                <AndroidApplication>true</AndroidApplication>
                <AndroidLinkTool>r8</AndroidLinkTool>
                <DatadogUploadSymbols>false</DatadogUploadSymbols>
                <IntermediateOutputPath>obj/</IntermediateOutputPath>
                <OutputPath>bin/</OutputPath>
                <AndroidProguardMappingFile>obj/custom mapping.txt</AndroidProguardMappingFile>
                <TestPublishDependencies Condition="'$(NoBuild)' != 'true'">Build</TestPublishDependencies>
              </PropertyGroup>
              <Import Project="{targets}" />
              <Target Name="_ComputeAndroidAssetsPaths">
                <Error Condition="!Exists('@(AndroidAsset)') And '$(AndroidApplication)' == 'true' And '$(DesignTimeBuild)' != 'true'" Text="Build ID was not registered before assets were resolved" />
                <WriteLinesToFile File="asset-names.txt" Lines="@(AndroidAsset->'%(LogicalName)')" Overwrite="true" />
              </Target>
              <Target Name="_CompileToDalvik" DependsOnTargets="_ComputeAndroidAssetsPaths">
                <WriteLinesToFile File="$(AndroidProguardMappingFile)" Lines="com.example.App -&gt; a:" Overwrite="true" Condition="'$(AndroidLinkTool)' == 'r8' And '$(DesignTimeBuild)' != 'true'" />
              </Target>
              <Target Name="Build" DependsOnTargets="_CompileToDalvik" />
              <Target Name="Publish" DependsOnTargets="$(TestPublishDependencies)" />
            </Project>
            """);
    }

    public void Dispose() => _root.Delete(recursive: true);

    private static string FindTargets()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "bindings", "Datadog.Maui", "build", "Datadog.Maui.AndroidSymbols.targets");
            if (File.Exists(path)) return path;
        }
        throw new FileNotFoundException("Android symbols targets not found");
    }

    private void Run(string target, params string[] properties)
    {
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            WorkingDirectory = _root.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in new[] { "msbuild", "app.proj", "-nologo", "-v:minimal", "-t:" + target }.Concat(properties))
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(TimeSpan.FromSeconds(60)))
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail("MSBuild timed out");
        }
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, stdout.Result + stderr.Result);
    }

    [Fact]
    public void RegistersTheAssetAndExportsTheSameIdWithoutUploadCredentials()
    {
        Run("Build");

        var id = File.ReadAllText(BuildId).Trim();
        Assert.True(Guid.TryParse(id, out _));
        Assert.Equal(File.ReadAllBytes(BuildId), File.ReadAllBytes(ExportedId));
        Assert.DoesNotContain('\uFEFF', System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(BuildId)));
        Assert.Equal("datadog.buildId", File.ReadAllText(Path.Combine(_root.FullName, "asset-names.txt")).Trim());
        Assert.Equal(File.ReadAllText(Path.Combine(_root.FullName, "obj", "custom mapping.txt")), File.ReadAllText(ExportedMapping));
    }

    [Fact]
    public void NoBuildPublishKeepsTheBuiltIdButANewBuildReplacesIt()
    {
        Run("Build");
        var id = File.ReadAllText(ExportedId);

        Run("Publish", "-p:NoBuild=true");
        Assert.Equal(id, File.ReadAllText(ExportedId));

        Run("Build");
        Assert.NotEqual(id, File.ReadAllText(ExportedId));
        Assert.Equal(File.ReadAllText(BuildId), File.ReadAllText(ExportedId));
    }

    [Fact]
    public void SwitchingOffR8RemovesThePreviouslyExportedPair()
    {
        Run("Build");
        Assert.True(File.Exists(ExportedMapping));

        Run("Build", "-p:AndroidLinkTool=none");

        Assert.True(File.Exists(BuildId));
        Assert.False(File.Exists(ExportedId));
        Assert.False(File.Exists(ExportedMapping));
    }

    [Fact]
    public void DesignTimeBuildDoesNotGenerateAnId()
    {
        Run("Build", "-p:DesignTimeBuild=true");

        Assert.False(File.Exists(BuildId));
        Assert.False(File.Exists(ExportedId));
    }
}
