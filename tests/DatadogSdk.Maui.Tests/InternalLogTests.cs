using System.IO;
using Xunit;

namespace DatadogSdk.Maui.Tests;

[Collection("InternalLog")]
public class InternalLogTests : IDisposable
{
    private readonly StringWriter _consoleOutput;
    private readonly TextWriter _originalOutput;

    public InternalLogTests()
    {
        _originalOutput = Console.Out;
        _consoleOutput = new StringWriter();
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOutput);
        _consoleOutput.Dispose();
        InternalLog.Verbosity = null;
    }

    [Fact]
    public void Log_WhenVerbosityNull_DoesNotOutput()
    {
        InternalLog.Verbosity = null;
        InternalLog.Log("test message", SdkVerbosity.ERROR);
        Assert.Empty(_consoleOutput.ToString());
    }

    [Fact]
    public void Log_WhenLevelMeetsVerbosity_Outputs()
    {
        InternalLog.Verbosity = SdkVerbosity.DEBUG;
        InternalLog.Log("hello", SdkVerbosity.DEBUG);
        Assert.Contains("DATADOG: [DEBUG] hello", _consoleOutput.ToString());
    }

    [Fact]
    public void Log_WhenLevelBelowVerbosity_DoesNotOutput()
    {
        InternalLog.Verbosity = SdkVerbosity.ERROR;
        InternalLog.Log("should not appear", SdkVerbosity.DEBUG);
        Assert.Empty(_consoleOutput.ToString());
    }

    [Fact]
    public void Log_HigherLevelAlwaysPassesLowerVerbosity()
    {
        InternalLog.Verbosity = SdkVerbosity.DEBUG;
        InternalLog.Log("error msg", SdkVerbosity.ERROR);
        Assert.Contains("DATADOG: [ERROR] error msg", _consoleOutput.ToString());
    }

    [Fact]
    public void Log_FormatIncludesPrefix()
    {
        InternalLog.Verbosity = SdkVerbosity.WARN;
        InternalLog.Log("test", SdkVerbosity.WARN);
        var output = _consoleOutput.ToString();
        Assert.StartsWith("DATADOG:", output.Trim());
    }
}
