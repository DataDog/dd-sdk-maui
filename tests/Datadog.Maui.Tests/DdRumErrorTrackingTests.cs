/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Datadog.Maui.Configuration;
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
        AssemblyDebugId.SetManifestOverrideForTests(null);
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

        // AppDomain.CurrentDomain.UnhandledException can't be raised directly without
        // crashing the test process, so invoke the private handler itself via reflection
        // with a real UnhandledExceptionEventArgs, mirroring what the CLR would do.
        // Actually thrown (not just `new`'d) so it carries real stack frames for BuildSdkFrames.
        var exception = CatchException(() => throw new InvalidOperationException("boom"));
        var args = new UnhandledExceptionEventArgs(exception, isTerminating: false);
        var handler = typeof(DdRumErrorTracking).GetMethod("OnUnhandledException", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(handler);

        handler!.Invoke(null, [null, args]);

        Assert.Equal(1, rumBridge.AddErrorCallCount);
        Assert.Equal(RumErrorSource.Source, rumBridge.LastSource);
        Assert.Equal("boom", rumBridge.LastMessage);
        Assert.Equal("AppDomain.UnhandledException", rumBridge.LastContext?["_dd.error.handler"]);
        Assert.Equal(false, rumBridge.LastContext?["_dd.error.is_crash"]);

        // Guards the ReportError -> AddError wiring itself: BuildSdkFrames is unit-tested
        // directly elsewhere, but nothing else exercises the real handler path to confirm
        // its output actually reaches the reported context under the expected key.
        var sdkFrames = Assert.IsType<List<Dictionary<string, object>>>(rumBridge.LastContext?["_dd.error.sdk_frames"]);
        Assert.NotEmpty(sdkFrames);
        Assert.All(sdkFrames, frame =>
        {
            Assert.IsType<int>(frame["line_index"]);
            Assert.IsType<int>(frame["method_token"]);
            Assert.IsType<int>(frame["il_offset"]);
        });
    }

    [Fact]
    public void OnUnobservedTaskException_ReportsErrorWithCorrectSource()
    {
        DdRumErrorTracking.StartTracking();

        // TaskScheduler.UnobservedTaskException can't be raised directly without a real
        // unobserved faulted Task and a GC pass, so invoke the private handler itself via
        // reflection with a real UnobservedTaskExceptionEventArgs, mirroring the
        // OnUnhandledException test above — both handlers must go through the same
        // unwrap-then-report path (see DdRumErrorTracking.HandleException), so this test
        // exists specifically to catch that symmetry regressing.
        var exception = new InvalidOperationException("boom");
        var faultedTask = Task.FromException(exception);
        var args = new UnobservedTaskExceptionEventArgs(faultedTask.Exception!);
        var handler = typeof(DdRumErrorTracking).GetMethod("OnUnobservedTaskException", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(handler);

        handler!.Invoke(null, [null, args]);

        Assert.Equal(1, rumBridge.AddErrorCallCount);
        Assert.Equal(RumErrorSource.Source, rumBridge.LastSource);
        Assert.Equal("boom", rumBridge.LastMessage);
        Assert.Equal("TaskScheduler.UnobservedTaskException", rumBridge.LastContext?["_dd.error.handler"]);
        Assert.Equal(false, rumBridge.LastContext?["_dd.error.is_crash"]);
    }

    // ── ReportRaisedCrash (AndroidEnvironment.UnhandledExceptionRaiser) ──
    // The event subscription itself only compiles for -android; these cover what it calls.

    [Fact]
    public void ReportRaisedCrash_ReportsFatalErrorFromRaiser()
    {
        DdRumErrorTracking.StartTracking();
        var exception = CatchException(() => throw new InvalidOperationException("boom"));

        DdRumErrorTracking.ReportRaisedCrash(exception);

        Assert.Equal(1, rumBridge.AddErrorCallCount);
        Assert.Equal("boom", rumBridge.LastMessage);
        Assert.Equal("AndroidEnvironment.UnhandledExceptionRaiser", rumBridge.LastContext?["_dd.error.handler"]);
        Assert.Equal(true, rumBridge.LastContext?["_dd.error.is_crash"]);
    }

    [Fact]
    public void OnUnhandledException_SameExceptionAlreadyRaised_IsNotReportedAgain()
    {
        DdRumErrorTracking.StartTracking();
        var exception = CatchException(() => throw new InvalidOperationException("boom"));
        DdRumErrorTracking.ReportRaisedCrash(exception);

        InvokeOnUnhandledException(exception);

        Assert.Equal(1, rumBridge.AddErrorCallCount);
    }

    [Fact]
    public void OnUnhandledException_DifferentExceptionAfterRaise_IsReported()
    {
        DdRumErrorTracking.StartTracking();
        DdRumErrorTracking.ReportRaisedCrash(CatchException(() => throw new InvalidOperationException("first")));

        InvokeOnUnhandledException(CatchException(() => throw new InvalidOperationException("second")));

        Assert.Equal(2, rumBridge.AddErrorCallCount);
        Assert.Equal("second", rumBridge.LastMessage);
    }

    // ── IsSkippedPlatformCrash ─────────────────────────────────────
    // The Java.Lang.Throwable type check only compiles for -android, so these tests pass
    // isPlatformThrowable directly and exercise the decision around it.

    [Fact]
    public void IsSkippedPlatformCrash_PureJavaCrashWithNativeReportingOff_IsSkipped()
    {
        // Never thrown in managed code, like a crash on a Java thread: no managed frames.
        var exception = new Exception("java crash");

        Assert.True(DdRumErrorTracking.IsSkippedPlatformCrash(exception, isPlatformThrowable: true, nativeCrashReportEnabled: false));
    }

    [Fact]
    public void IsSkippedPlatformCrash_PureJavaCrashWithNativeReportingOn_IsReported()
    {
        var exception = new Exception("java crash");

        Assert.False(DdRumErrorTracking.IsSkippedPlatformCrash(exception, isPlatformThrowable: true, nativeCrashReportEnabled: true));
    }

    [Fact]
    public void IsSkippedPlatformCrash_JavaExceptionThrownThroughManagedCode_IsReported()
    {
        // A Java exception that escaped a C# call is thrown through managed frames.
        var exception = CatchException(() => throw new Exception("java exception via C#"));

        Assert.False(DdRumErrorTracking.IsSkippedPlatformCrash(exception, isPlatformThrowable: true, nativeCrashReportEnabled: false));
    }

    [Fact]
    public void IsSkippedPlatformCrash_ThrowableWrappingManagedException_IsReported()
    {
        // A JavaProxyThrowable shell around a C# exception.
        var exception = new Exception("proxy", new InvalidOperationException("boom"));

        Assert.False(DdRumErrorTracking.IsSkippedPlatformCrash(exception, isPlatformThrowable: true, nativeCrashReportEnabled: false));
    }

    [Fact]
    public void IsSkippedPlatformCrash_ManagedException_IsReported()
    {
        var exception = new InvalidOperationException("boom");

        Assert.False(DdRumErrorTracking.IsSkippedPlatformCrash(exception, isPlatformThrowable: false, nativeCrashReportEnabled: false));
    }

    // ── UnwrapJavaException ────────────────────────────────────────
    // TODO: The #if ANDROID branch (Java.Lang.Throwable → InnerException) only compiles for
    // the -android target framework. Still needs verification on a real Android run/device.

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

    // ── BuildStackTrace ─────────────────────────────────────────────

    [Fact]
    public void BuildStackTrace_NullException_ReturnsFallbackWithoutFrames()
    {
        var result = DdRumErrorTracking.BuildStackTrace(null);

        Assert.Equal("No stacktrace available", result.Text);
        Assert.Empty(result.SdkFrames);
    }

    [Fact]
    public void BuildStackTrace_SdkFramesPointToTheirManagedLines()
    {
        var exception = CatchException(() => throw new InvalidOperationException("boom"));

        var result = DdRumErrorTracking.BuildStackTrace(exception);
        var lines = result.Text.Split('\n');

        AssertSdkFramesPointToManagedLines(result, lines);
    }

    [Fact]
    public void BuildStackTrace_MultilineExceptionMessageKeepsFrameIndexesAligned()
    {
        var exception = CatchException(() => throw new InvalidOperationException("first line\nsecond line"));

        var result = DdRumErrorTracking.BuildStackTrace(exception);
        var lines = result.Text.Split('\n');

        Assert.Equal("System.InvalidOperationException: first line", lines[0]);
        Assert.Equal("second line", lines[1]);
        AssertSdkFramesPointToManagedLines(result, lines);
        Assert.All(result.SdkFrames, frame => Assert.True(Assert.IsType<int>(frame["line_index"]) > 1));
    }

    [Fact]
    public async Task BuildStackTrace_AsyncExceptionKeepsFrameIndexesAligned()
    {
        var exception = await CatchExceptionAsync(ThrowAfterAwait);

        var result = DdRumErrorTracking.BuildStackTrace(exception);
        var lines = result.Text.Split('\n');

        Assert.Contains(lines, line => line.Contains(nameof(ThrowAfterAwait), StringComparison.Ordinal));
        AssertSdkFramesPointToManagedLines(result, lines);
    }

    [Fact]
    public void BuildStackTrace_AggregateExceptionKeepsAllInnerFrameIndexesAligned()
    {
        var first = CatchException(ThrowFirstAggregateInner);
        var second = CatchException(ThrowSecondAggregateInner);
        var exception = CatchException(() => throw new AggregateException("aggregate failure", first, second));

        var result = DdRumErrorTracking.BuildStackTrace(exception);
        var lines = result.Text.Split('\n');

        Assert.Contains(lines, line => line.Contains("first inner failure", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("second inner failure", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains(nameof(ThrowFirstAggregateInner), StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains(nameof(ThrowSecondAggregateInner), StringComparison.Ordinal));
        Assert.Equal(2, lines.Count(line => line.Contains("End of inner exception stack trace", StringComparison.Ordinal)));
        AssertSdkFramesPointToManagedLines(result, lines);
    }

    [Fact]
    public void BuildStackTrace_MetadataExtractionFailurePreservesRenderedManagedLines()
    {
        var exception = CatchException(() => throw new InvalidOperationException("boom"));

        var result = DdRumErrorTracking.BuildStackTrace(
            exception,
            javaStackTrace: null,
            (_, _, _) => throw new InvalidOperationException("metadata unavailable"));

        Assert.Contains(result.Text.Split('\n'), line => line.StartsWith("   at ", StringComparison.Ordinal));
        Assert.Empty(result.SdkFrames);
    }

    [Fact]
    public void BuildStackTrace_JavaTailDoesNotAffectManagedLineIndexes()
    {
        var exception = CatchException(() => throw new InvalidOperationException("boom"));
        string[] javaStackTrace =
        [
            "java.lang.IllegalStateException: Java failure",
            "   at example.JavaThrower.level3(JavaThrower.java:14)",
            "   at example.JavaThrower.level2(JavaThrower.java:10)",
        ];

        var result = DdRumErrorTracking.BuildStackTrace(exception, javaStackTrace);
        var lines = result.Text.Split('\n');
        var javaStartIndex = lines.Length - javaStackTrace.Length;

        Assert.True(javaStartIndex > 0);
        Assert.Equal(javaStackTrace, lines.Skip(javaStartIndex));
        Assert.All(result.SdkFrames, frame =>
        {
            var lineIndex = Assert.IsType<int>(frame["line_index"]);
            Assert.True(lineIndex < javaStartIndex);
        });
    }

    [Fact]
    public void BuildStackTrace_InnerExceptionFramesHaveStableLineIndexes()
    {
        var exception = CatchException(ThrowWithInnerException);

        var result = DdRumErrorTracking.BuildStackTrace(exception);
        var lines = result.Text.Split('\n');

        Assert.Contains(lines, line => line.Contains("inner failure", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("outer failure", StringComparison.Ordinal));
        Assert.All(result.SdkFrames, frame =>
        {
            var lineIndex = Assert.IsType<int>(frame["line_index"]);
            Assert.StartsWith("   at ", lines[lineIndex]);
        });
    }

    [Fact]
    public void BuildStackTrace_ExceptionDispatchInfoRethrowPreservesBoundary()
    {
        var exception = CatchException(RethrowWithExceptionDispatchInfo);

        var result = DdRumErrorTracking.BuildStackTrace(exception);
        var lines = result.Text.Split('\n');
        var boundaryIndex = Array.FindIndex(
            lines,
            line => line.Contains("End of stack trace from previous location", StringComparison.Ordinal));

        Assert.True(boundaryIndex > 0);
        Assert.Contains(lines.Take(boundaryIndex), line => line.Contains(nameof(ThrowBeforeDispatch), StringComparison.Ordinal));
        Assert.Contains(lines.Skip(boundaryIndex + 1), line => line.Contains(nameof(RethrowWithExceptionDispatchInfo), StringComparison.Ordinal));
        Assert.All(result.SdkFrames, frame =>
        {
            var lineIndex = Assert.IsType<int>(frame["line_index"]);
            Assert.StartsWith("   at ", lines[lineIndex]);
        });
    }

    [Fact]
    public void BuildStackTrace_RethrowBoundaryBeforeJavaTailKeepsSectionsAndIndexesAligned()
    {
        var exception = CatchException(RethrowWithExceptionDispatchInfo);
        string[] javaStackTrace =
        [
            "java.lang.IllegalStateException: Java failure",
            "   at example.JavaThrower.level3(JavaThrower.java:14)",
            "   at example.JavaThrower.level2(JavaThrower.java:10)",
        ];

        var result = DdRumErrorTracking.BuildStackTrace(exception, javaStackTrace);
        var lines = result.Text.Split('\n');
        var boundaryIndex = Array.FindIndex(
            lines,
            line => line.Contains("End of stack trace from previous location", StringComparison.Ordinal));
        var javaStartIndex = lines.Length - javaStackTrace.Length;

        Assert.True(boundaryIndex > 0);
        Assert.True(boundaryIndex < javaStartIndex);
        Assert.Equal(javaStackTrace, lines.Skip(javaStartIndex));
        AssertSdkFramesPointToManagedLines(result, lines);
        Assert.All(result.SdkFrames, frame => Assert.True(Assert.IsType<int>(frame["line_index"]) < javaStartIndex));
    }

    [Fact]
    public void BuildSdkFrames_AssemblyId_MatchesItsManifestEntryAndIsNotTheMvid()
    {
        var exception = CatchException(() => throw new InvalidOperationException("boom"));
        var thisAssembly = typeof(DdRumErrorTrackingTests).Assembly;
        var expectedId = TestPeDebugId.Compute(thisAssembly);
        var mvid = thisAssembly.ManifestModule.ModuleVersionId.ToString("N");
        AssemblyDebugId.SetManifestOverrideForTests(
            new Dictionary<string, string> { [thisAssembly.GetName().Name!] = expectedId });

        var frames = DdRumErrorTracking.BuildStackTrace(exception).SdkFrames;

        Assert.Contains(frames, f => Equals(f.GetValueOrDefault("assembly_id"), expectedId));
        Assert.DoesNotContain(frames, f => Equals(f.GetValueOrDefault("assembly_id"), mvid));
    }

    [Fact]
    public void BuildSdkFrames_MultiAssemblyStack_EachFrameReportsItsOwnDeclaringAssemblyFromTheManifest()
    {
        var thisAssembly = typeof(DdRumErrorTrackingTests).Assembly;
        var otherAssembly = typeof(CrossAssemblyThrower).Assembly;
        var expectedThisId = TestPeDebugId.Compute(thisAssembly);
        var expectedOtherId = TestPeDebugId.Compute(otherAssembly);
        Assert.NotEqual(expectedThisId, expectedOtherId);
        AssemblyDebugId.SetManifestOverrideForTests(new Dictionary<string, string>
        {
            [thisAssembly.GetName().Name!] = expectedThisId,
            [otherAssembly.GetName().Name!] = expectedOtherId,
        });

        var exception = CatchException(InvokeCrossAssemblyThrower);
        var frames = DdRumErrorTracking.BuildStackTrace(exception).SdkFrames;

        Assert.Contains(frames, f => Equals(f.GetValueOrDefault("assembly_id"), expectedThisId));
        Assert.Contains(frames, f => Equals(f.GetValueOrDefault("assembly_id"), expectedOtherId));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InvokeCrossAssemblyThrower() => CrossAssemblyThrower.ThrowFromThisAssembly();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowWithInnerException()
    {
        try
        {
            throw new InvalidOperationException("inner failure");
        }
        catch (Exception inner)
        {
            throw new ApplicationException("outer failure", inner);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RethrowWithExceptionDispatchInfo()
    {
        try
        {
            ThrowBeforeDispatch();
        }
        catch (Exception exception)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowBeforeDispatch() => throw new InvalidOperationException("dispatched failure");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowFirstAggregateInner() => throw new InvalidOperationException("first inner failure");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowSecondAggregateInner() => throw new ArgumentException("second inner failure");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task ThrowAfterAwait()
    {
        await Task.Yield();
        throw new InvalidOperationException("async failure");
    }

    private static async Task<Exception> CatchExceptionAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            return ex;
        }

        throw new InvalidOperationException("Expected action to throw.");
    }

    private static void AssertSdkFramesPointToManagedLines(
        DdRumErrorTracking.RenderedStackTrace result,
        string[] lines)
    {
        Assert.NotEmpty(result.SdkFrames);
        Assert.All(result.SdkFrames, frame =>
        {
            var lineIndex = Assert.IsType<int>(frame["line_index"]);
            Assert.InRange(lineIndex, 0, lines.Length - 1);
            Assert.StartsWith("   at ", lines[lineIndex]);
            Assert.IsType<int>(frame["method_token"]);
            Assert.IsType<int>(frame["il_offset"]);
        });
    }

    private static void InvokeOnUnhandledException(Exception exception)
    {
        var handler = typeof(DdRumErrorTracking).GetMethod("OnUnhandledException", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(handler);
        handler!.Invoke(null, [null, new UnhandledExceptionEventArgs(exception, isTerminating: true)]);
    }

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
