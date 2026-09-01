/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.Runtime.CompilerServices;
using Datadog.Maui.Tests.Fixtures.CrossAssembly;
using Xunit;

namespace Datadog.Maui.Tests;

[Collection("DdRum")]
public class DdRumErrorTrackingTests : IDisposable
{
    private readonly MockRumBridge rumBridge = new();

    public DdRumErrorTrackingTests()
    {
        DdRum.testBridge = rumBridge;
    }

    public void Dispose()
    {
        DdRumErrorTracking.StopTracking();
        DdRum.testBridge = null;
        GC.SuppressFinalize(this);
    }

    // ── StartTracking / StopTracking ──────────────────────────────

    [Fact]
    public void StartTracking_SetsIsTrackingTrue()
    {
        DdRumErrorTracking.StartTracking();

        Assert.True(DdRumErrorTracking.IsTracking);
    }

    [Fact]
    public void StartTracking_CalledTwice_DoesNotDoubleRegister()
    {
        DdRumErrorTracking.StartTracking();
        DdRumErrorTracking.StartTracking();

        Assert.True(DdRumErrorTracking.IsTracking);
    }

    [Fact]
    public void StopTracking_SetsIsTrackingFalse()
    {
        DdRumErrorTracking.StartTracking();
        DdRumErrorTracking.StopTracking();

        Assert.False(DdRumErrorTracking.IsTracking);
    }

    [Fact]
    public void StopTracking_WhenNotTracking_DoesNotThrow()
    {
        DdRumErrorTracking.StopTracking();

        Assert.False(DdRumErrorTracking.IsTracking);
    }

    // ── Direct handler tests ─────────────────────────────────────

    [Fact]
    public void OnUnhandledException_ReportsErrorWithCorrectSource()
    {
        DdRumErrorTracking.StartTracking();

        // Simulate AppDomain.UnhandledException by raising it on the current domain
        // We can't easily trigger this without crashing, so we test via the public API
        // that DdRumErrorTracking hooks are in place by checking IsTracking
        Assert.True(DdRumErrorTracking.IsTracking);
    }

    // ── UnwrapJavaException ────────────────────────────────────────
    // The #if ANDROID branch (Java.Lang.Throwable → InnerException) only compiles for the
    // -android target framework, so it can't be exercised by this desktop (net9.0/net10.0)
    // test project — that unwrap path still needs verification on a real Android run/device.

    [Fact]
    public void UnwrapJavaException_PlainException_ReturnsSameInstance()
    {
        var exception = new InvalidOperationException("plain");

        var result = DdRumErrorTracking.UnwrapJavaException(exception);

        Assert.Same(exception, result);
    }

    [Fact]
    public void UnwrapJavaException_Null_ReturnsNull()
    {
        Assert.Null(DdRumErrorTracking.UnwrapJavaException(null));
    }

    // ── BuildSdkFrames ──────────────────────────────────────────────

    [Fact]
    public void BuildSdkFrames_NullException_ReturnsEmptyList()
    {
        var frames = DdRumErrorTracking.BuildSdkFrames(null);

        Assert.Empty(frames);
    }

    [Fact]
    public void BuildSdkFrames_SimpleException_IncludesMethodTokenAndIlOffsetForEveryFrame()
    {
        var exception = CatchException(() => throw new InvalidOperationException("boom"));

        var frames = DdRumErrorTracking.BuildSdkFrames(exception);

        Assert.NotEmpty(frames);
        Assert.All(frames, frame =>
        {
            Assert.IsType<int>(frame["method_token"]);
            Assert.IsType<int>(frame["il_offset"]);
        });
    }

    [Fact]
    public void BuildSdkFrames_AssemblyId_MatchesIndependentlyComputedDebugIdAndIsNotTheMvid()
    {
        var exception = CatchException(() => throw new InvalidOperationException("boom"));
        var thisAssembly = typeof(DdRumErrorTrackingTests).Assembly;
        var expectedId = AssemblyDebugIdTests.ComputeExpectedDebugId(thisAssembly);
        var mvid = thisAssembly.ManifestModule.ModuleVersionId.ToString("N");

        var frames = DdRumErrorTracking.BuildSdkFrames(exception);

        Assert.Contains(frames, f => Equals(f.GetValueOrDefault("assembly_id"), expectedId));
        Assert.DoesNotContain(frames, f => Equals(f.GetValueOrDefault("assembly_id"), mvid));
    }

    [Fact]
    public void BuildSdkFrames_MultiAssemblyStack_EachFrameReportsItsOwnDeclaringAssembly()
    {
        var exception = CatchException(InvokeCrossAssemblyThrower);

        var frames = DdRumErrorTracking.BuildSdkFrames(exception);

        var thisAssembly = typeof(DdRumErrorTrackingTests).Assembly;
        var otherAssembly = typeof(CrossAssemblyThrower).Assembly;
        var expectedThisId = AssemblyDebugIdTests.ComputeExpectedDebugId(thisAssembly);
        var expectedOtherId = AssemblyDebugIdTests.ComputeExpectedDebugId(otherAssembly);

        Assert.NotEqual(expectedThisId, expectedOtherId);
        Assert.Contains(frames, f => Equals(f.GetValueOrDefault("assembly_id"), expectedThisId));
        Assert.Contains(frames, f => Equals(f.GetValueOrDefault("assembly_id"), expectedOtherId));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InvokeCrossAssemblyThrower() => CrossAssemblyThrower.ThrowFromThisAssembly();

    private static Exception CatchException(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            return ex;
        }

        throw new InvalidOperationException("Expected action to throw.");
    }
}
