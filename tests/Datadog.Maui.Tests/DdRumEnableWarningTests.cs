/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.IO;
using Datadog.Maui;
using Datadog.Maui.Configuration;
using Xunit;

namespace Datadog.Maui.Tests;

/// <summary>
/// Regression guard for the WARN log emitted when DdRum.Enable is called
/// without an Application.Current (e.g. from MauiProgram.CreateMauiApp).
/// Replacing this WARN with a silent skip — as the code did pre-fix — would
/// cause AutomaticViewTracking / AutomaticActionTracking to fail silently,
/// which is the exact failure mode the Hosting extensions exist to prevent.
/// </summary>
[Collection("InternalLog")]
public class DdRumEnableWarningTests : IDisposable
{
    private readonly StringWriter _consoleOutput;
    private readonly TextWriter _originalOutput;

    public DdRumEnableWarningTests()
    {
        _originalOutput = Console.Out;
        _consoleOutput = new StringWriter();
        Console.SetOut(_consoleOutput);
        InternalLog.Verbosity = SdkVerbosity.WARN;
    }

    public void Dispose()
    {
        Console.SetOut(_originalOutput);
        _consoleOutput.Dispose();
        InternalLog.Verbosity = null;
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Enable_AutomaticViewTracking_WithoutApplicationCurrent_LogsWarning()
    {
        // Application.Current is null in this xUnit host — exactly the state
        // that MauiProgram.CreateMauiApp() leaves the runtime in before
        // builder.Build() resolves the App singleton.
        Assert.Null(Application.Current);

        DdRum.Enable(new DdRumConfiguration
        {
            ApplicationId = "test-app",
            AutomaticViewTracking = true,
            AutomaticActionTracking = false,
            AutomaticResourceTracking = false,
        });

        var output = _consoleOutput.ToString();
        Assert.Contains("DATADOG: [WARN]", output);
        Assert.Contains("AutomaticViewTracking=true", output);
        Assert.Contains("Application.Current", output);
    }

    [Fact]
    public void Enable_AutomaticActionTracking_WithoutApplicationCurrent_LogsWarning()
    {
        Assert.Null(Application.Current);

        DdRum.Enable(new DdRumConfiguration
        {
            ApplicationId = "test-app",
            AutomaticViewTracking = false,
            AutomaticActionTracking = true,
            AutomaticResourceTracking = false,
        });

        var output = _consoleOutput.ToString();
        Assert.Contains("DATADOG: [WARN]", output);
        Assert.Contains("AutomaticActionTracking=true", output);
        Assert.Contains("Application.Current", output);
    }

    [Fact]
    public void Enable_AutomaticResourceTracking_WithoutApplicationCurrent_LogsWarning()
    {
        Assert.Null(Application.Current);

        DdRum.Enable(new DdRumConfiguration
        {
            ApplicationId = "test-app",
            AutomaticViewTracking = false,
            AutomaticActionTracking = false,
            AutomaticResourceTracking = true,
        });

        var output = _consoleOutput.ToString();
        Assert.Contains("DATADOG: [WARN]", output);
        Assert.Contains("AutomaticResourceTracking=true", output);
        Assert.Contains("Application.Current", output);
    }

    [Fact]
    public void Enable_AutoTrackingDisabled_DoesNotLogWarning()
    {
        Assert.Null(Application.Current);

        DdRum.Enable(new DdRumConfiguration
        {
            ApplicationId = "test-app",
            AutomaticViewTracking = false,
            AutomaticActionTracking = false,
            AutomaticResourceTracking = false,
        });

        // The WARN we guard against is specifically the auto-tracker one — other
        // unrelated WARNs (e.g. DdRumErrorTracking's "already tracking" from
        // cross-test static state) are not in scope.
        var output = _consoleOutput.ToString();
        Assert.DoesNotContain("AutomaticViewTracking=true", output);
        Assert.DoesNotContain("AutomaticActionTracking=true", output);
        Assert.DoesNotContain("AutomaticResourceTracking=true", output);
    }
}
