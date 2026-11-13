/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Datadog.Maui.Configuration;
using Xunit;

namespace Datadog.Maui.Tests;

/// <summary>
/// DdRum.AddError's Exception overload (dd-sdk-maui#98): handled exceptions are reported with the
/// same stack text and sdk_frames as uncaught ones, as non-crashes.
/// </summary>
[Collection("DdRum")]
public class DdRumAddErrorExceptionTests : IDisposable
{
    private readonly MockRumBridge rumBridge = new();

    public DdRumAddErrorExceptionTests()
    {
        DdRum.testBridge = rumBridge;
    }

    public void Dispose()
    {
        DdRum.testBridge = null;
        DdRum.errorEventMapper = null;
        GC.SuppressFinalize(this);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Exception ThrowAndCatch()
    {
        try
        {
            throw new InvalidOperationException("thrown");
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (Exception Exception, StackTrace Stack) CreateWithoutThrowing() =>
        (new InvalidOperationException("never thrown"), new StackTrace(true));

    private List<Dictionary<string, object>> SdkFrames() =>
        Assert.IsType<List<Dictionary<string, object>>>(rumBridge.LastContext?["_dd.error.sdk_frames"]);

    // The method names in the reported stack text, top first.
    private string[] FrameLines() =>
        rumBridge.LastStacktrace!.Split('\n').Where(line => line.TrimStart().StartsWith("at ", StringComparison.Ordinal)).ToArray();

    [Fact]
    public void ReportsAThrownExceptionAsAHandledErrorWithSdkFrames()
    {
        DdRum.AddError(ThrowAndCatch());

        Assert.Equal(1, rumBridge.AddErrorCallCount);
        Assert.Equal("thrown", rumBridge.LastMessage);
        Assert.Equal(RumErrorSource.Source, rumBridge.LastSource);
        Assert.StartsWith("System.InvalidOperationException: thrown", rumBridge.LastStacktrace);
        Assert.Contains(nameof(ThrowAndCatch), FrameLines()[0]);
        Assert.Equal(false, rumBridge.LastContext?["_dd.error.is_crash"]);
        Assert.Equal("DdRum.AddError", rumBridge.LastContext?["_dd.error.handler"]);
        Assert.All(SdkFrames(), frame =>
        {
            Assert.IsType<int>(frame["line_index"]);
            Assert.IsType<int>(frame["method_token"]);
            Assert.IsType<int>(frame["il_offset"]);
        });
    }

    // The stack text and sdk_frames are the crash path's: same text, same frame data.
    [Fact]
    public void RendersTheSameStackAsTheCrashPath()
    {
        var exception = ThrowAndCatch();
        var expected = DdRumErrorTracking.BuildStackTrace(exception);

        DdRum.AddError(exception);

        Assert.Equal(expected.Text, rumBridge.LastStacktrace);
        Assert.Equal(expected.SdkFrames.Count, SdkFrames().Count);
        Assert.Equal(expected.SdkFrames.Select(f => f["method_token"]), SdkFrames().Select(f => f["method_token"]));
    }

    [Fact]
    public void PassesTheCallersMessageSourceContextTimestampAndFingerprint()
    {
        var context = new Dictionary<string, object> { { "team", "payments" } };

        DdRum.AddError(ThrowAndCatch(), "Checkout failed", RumErrorSource.Network, context, timestampMs: 1234, fingerprint: "checkout");

        Assert.Equal("Checkout failed", rumBridge.LastMessage);
        Assert.Equal(RumErrorSource.Network, rumBridge.LastSource);
        Assert.Equal(1234, rumBridge.LastTimestampMs);
        Assert.Equal("payments", rumBridge.LastContext?["team"]);
        Assert.Equal("checkout", rumBridge.LastContext?["_dd.error.fingerprint"]);
        Assert.False(context.ContainsKey("_dd.error.sdk_frames"), "the caller's dictionary must not be modified");
    }

    // A caller migrating from a hand-built sdk_frames (the #98 workaround) can't desync them from the text.
    [Fact]
    public void ReplacesSdkFramesTheCallerPassedInTheContext()
    {
        var stale = new List<Dictionary<string, object>> { new() { { "line_index", 99 } } };

        DdRum.AddError(ThrowAndCatch(), context: new Dictionary<string, object> { { "_dd.error.sdk_frames", stale } });

        Assert.NotSame(stale, SdkFrames());
        Assert.DoesNotContain(SdkFrames(), frame => (int)frame["line_index"] == 99);
    }

    // An exception that was never thrown has no frames: the caller of AddError is reported instead,
    // not AddError itself.
    [Fact]
    public void ReportsTheCallersStackForAnExceptionThatWasNeverThrown()
    {
        DdRum.AddError(new InvalidOperationException("never thrown"));

        Assert.Contains(nameof(ReportsTheCallersStackForAnExceptionThatWasNeverThrown), FrameLines()[0]);
        Assert.DoesNotContain(FrameLines(), line => line.Contains("Datadog.Maui.DdRum.AddError", StringComparison.Ordinal));
        Assert.NotEmpty(SdkFrames());
    }

    [Fact]
    public void ReportsThePassedStackForAnExceptionThatWasNeverThrown()
    {
        var (exception, stack) = CreateWithoutThrowing();

        DdRum.AddError(exception, stackTrace: stack);

        Assert.Contains(nameof(CreateWithoutThrowing), FrameLines()[0]);
        Assert.NotEmpty(SdkFrames());
    }

    // An explicit stack replaces the exception's own, even when it has one.
    [Fact]
    public void PrefersThePassedStackOverTheExceptionsOwn()
    {
        var (_, stack) = CreateWithoutThrowing();

        DdRum.AddError(ThrowAndCatch(), stackTrace: stack);

        Assert.Contains(nameof(CreateWithoutThrowing), FrameLines()[0]);
        Assert.DoesNotContain(FrameLines(), line => line.Contains(nameof(ThrowAndCatch), StringComparison.Ordinal));
    }

    // The passed stack replaces only the outer exception's frames; inner exceptions keep their own.
    [Fact]
    public void KeepsTheInnerExceptionsOwnFramesWhenAStackIsPassed()
    {
        var (_, stack) = CreateWithoutThrowing();
        var outer = new InvalidOperationException("outer", ThrowAndCatch());

        DdRum.AddError(outer, stackTrace: stack);

        Assert.Contains(" ---> System.InvalidOperationException: thrown", rumBridge.LastStacktrace);
        Assert.Contains(FrameLines(), line => line.Contains(nameof(ThrowAndCatch), StringComparison.Ordinal));
        Assert.Contains(FrameLines(), line => line.Contains(nameof(CreateWithoutThrowing), StringComparison.Ordinal));
    }

    [Fact]
    public void AppliesTheErrorEventMapper()
    {
        DdRum.errorEventMapper = e =>
        {
            e.Message = "[mapped] " + e.Message;
            return e;
        };

        DdRum.AddError(ThrowAndCatch());

        Assert.Equal("[mapped] thrown", rumBridge.LastMessage);
    }

    [Fact]
    public void LetsTheErrorEventMapperDropTheError()
    {
        DdRum.errorEventMapper = _ => null;

        DdRum.AddError(ThrowAndCatch());

        Assert.Equal(0, rumBridge.AddErrorCallCount);
    }

    [Fact]
    public void IgnoresANullException()
    {
        DdRum.AddError((Exception)null!);

        Assert.Equal(0, rumBridge.AddErrorCallCount);
    }

    // The original overload keeps working unchanged next to the new one.
    [Fact]
    public void StillAcceptsAStacktraceString()
    {
        DdRum.AddError("Error", RumErrorSource.Source, "at Foo.Bar()");

        Assert.Equal("at Foo.Bar()", rumBridge.LastStacktrace);
        Assert.Null(rumBridge.LastContext?.GetValueOrDefault("_dd.error.sdk_frames"));
    }

    private sealed class ThrowingMessageException : Exception
    {
        public override string Message => throw new InvalidOperationException("Message getter failed");
    }

    private sealed class NullMessageException : Exception
    {
        public override string Message => null!;
    }

    // Every sdk_frames entry points at an "at …" line of the reported text.
    private void AssertFramesPointAtFrameLines()
    {
        var lines = rumBridge.LastStacktrace!.Split('\n');
        Assert.All(SdkFrames(), frame =>
        {
            var lineIndex = (int)frame["line_index"];
            Assert.InRange(lineIndex, 0, lines.Length - 1);
            Assert.StartsWith("   at ", lines[lineIndex]);
        });
    }

    // The frame skipped by the call-site fallback is only AddError's own if it's never inlined; a
    // normal test run doesn't optimize enough to catch the attribute going missing.
    [Fact]
    public void KeepsTheExceptionOverloadFromBeingInlined()
    {
        var method = typeof(DdRum).GetMethod(nameof(DdRum.AddError), [typeof(Exception), typeof(string), typeof(RumErrorSource),
            typeof(Dictionary<string, object>), typeof(long), typeof(string), typeof(StackTrace)]);

        Assert.NotNull(method);
        Assert.True(method!.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoInlining));
    }

    [Fact]
    public void KeepsTheSdksCrashAndHandlerKeysOverTheCallers()
    {
        var context = new Dictionary<string, object> { { "_dd.error.is_crash", true }, { "_dd.error.handler", "custom" } };

        DdRum.AddError(ThrowAndCatch(), context: context);

        Assert.Equal(false, rumBridge.LastContext?["_dd.error.is_crash"]);
        Assert.Equal("DdRum.AddError", rumBridge.LastContext?["_dd.error.handler"]);
    }

    // With no frame at all, a caller's sdk_frames are still dropped and no empty list is sent.
    [Fact]
    public void SendsNoSdkFramesWhenNoFrameCouldBeBuilt()
    {
        var stale = new List<Dictionary<string, object>> { new() { { "line_index", 0 } } };
        var noFrames = new StackTrace(skipFrames: 10_000);

        DdRum.AddError(new InvalidOperationException("never thrown"), context: new Dictionary<string, object> { { "_dd.error.sdk_frames", stale } }, stackTrace: noFrames);

        Assert.Equal(1, rumBridge.AddErrorCallCount);
        Assert.False(rumBridge.LastContext!.ContainsKey("_dd.error.sdk_frames"));
    }

    [Fact]
    public void AlignsFramesWithTheTextForAThrownException()
    {
        DdRum.AddError(ThrowAndCatch());

        AssertFramesPointAtFrameLines();
    }

    [Fact]
    public void AlignsFramesWithTheTextForTheCallSiteStack()
    {
        DdRum.AddError(new InvalidOperationException("never thrown"));

        AssertFramesPointAtFrameLines();
    }

    [Fact]
    public void AlignsFramesWithTheTextForAPassedStackAndInnerExceptions()
    {
        var (_, stack) = CreateWithoutThrowing();

        DdRum.AddError(new InvalidOperationException("outer", ThrowAndCatch()), stackTrace: stack);

        AssertFramesPointAtFrameLines();
    }

    // Every inner exception of an AggregateException keeps its own frames when a stack is passed.
    [Fact]
    public void KeepsEachAggregatedExceptionsFramesWhenAStackIsPassed()
    {
        var (_, stack) = CreateWithoutThrowing();
        var aggregate = new AggregateException(ThrowAndCatch(), ThrowAndCatch());

        DdRum.AddError(aggregate, stackTrace: stack);

        Assert.Equal(2, FrameLines().Count(line => line.Contains(nameof(ThrowAndCatch), StringComparison.Ordinal)));
        Assert.Contains(FrameLines(), line => line.Contains(nameof(CreateWithoutThrowing), StringComparison.Ordinal));
        AssertFramesPointAtFrameLines();
    }

    // A mapper that throws doesn't lose the error: the original is sent, frames included.
    [Fact]
    public void SendsTheOriginalErrorWhenTheMapperThrows()
    {
        DdRum.errorEventMapper = _ => throw new InvalidOperationException("mapper failed");

        DdRum.AddError(ThrowAndCatch());

        Assert.Equal("thrown", rumBridge.LastMessage);
        Assert.NotEmpty(SdkFrames());
    }

    // Reporting is called from the app's catch blocks, so it never throws back into them.
    [Fact]
    public void DoesNotThrowWhenTheExceptionCannotBeRead()
    {
        var exception = Record.Exception(() => DdRum.AddError(new ThrowingMessageException()));

        Assert.Null(exception);
    }

    [Fact]
    public void FallsBackToTheExceptionTypeWhenItsMessageIsNull()
    {
        DdRum.AddError(new NullMessageException());

        Assert.Equal(typeof(NullMessageException).FullName, rumBridge.LastMessage);
    }

    // An explicit message is kept as given, even when empty.
    [Fact]
    public void KeepsAnExplicitEmptyMessage()
    {
        DdRum.AddError(ThrowAndCatch(), message: "");

        Assert.Equal("", rumBridge.LastMessage);
    }

    [Fact]
    public void AcceptsANullContextAndDefaultsTheSourceToSource()
    {
        DdRum.AddError(ThrowAndCatch(), context: null);

        Assert.Equal(RumErrorSource.Source, rumBridge.LastSource);
        Assert.NotNull(rumBridge.LastContext);
    }

    // A frame of a real method that reports a chosen IL offset, like Mono under LLVM AOT.
    private sealed class FrameWithIlOffset(int ilOffset) : StackFrame(0, false)
    {
        public override MethodBase? GetMethod() => typeof(DdRumAddErrorExceptionTests).GetMethod(nameof(ThrowAndCatch), BindingFlags.NonPublic | BindingFlags.Static);

        public override int GetILOffset() => ilOffset;
    }

    private static StackTrace StackWithIlOffsets(params int[] ilOffsets) =>
        new(ilOffsets.Select(offset => (StackFrame)new FrameWithIlOffset(offset)).ToArray());

    [Fact]
    public void SendsIlOffsetZeroAsUnknown()
    {
        DdRum.AddError(new InvalidOperationException("never thrown"), stackTrace: StackWithIlOffsets(0, 0));

        Assert.All(SdkFrames(), frame => Assert.Equal(-1, frame["il_offset"]));
    }

    [Fact]
    public void KeepsOtherIlOffsets()
    {
        DdRum.AddError(new InvalidOperationException("never thrown"), stackTrace: StackWithIlOffsets(7, -1, 0));

        Assert.Equal(new object[] { 7, -1, -1 }, SdkFrames().Select(frame => frame["il_offset"]).ToArray());
    }

    [Fact]
    public void LeavesTheCrashPathsIlOffsetZeroAlone()
    {
        var rendered = DdRumErrorTracking.BuildStackTrace(new InvalidOperationException("crash"), stackTrace: StackWithIlOffsets(0));

        Assert.Equal(0, Assert.Single(rendered.SdkFrames)["il_offset"]);
    }
}
