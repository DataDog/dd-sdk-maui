using DatadogSdk.Maui;
using DatadogSdk.Maui.Configuration;
using Xunit;

namespace DatadogSdk.Maui.Tests;

[Collection("DdRum")]
public class DdRumTrackingTests : IDisposable
{
    private readonly MockRumBridge bridge = new();

    public DdRumTrackingTests()
    {
        DdRum.testBridge = bridge;
    }

    public void Dispose()
    {
        DdRum.testBridge = null;
        GC.SuppressFinalize(this);
    }

    // --- Views ---------------------------------------------------------------

    [Fact]
    public void StartView_PassesAllParametersToBridge()
    {
        var ctx = new Dictionary<string, object> { { "screen", "home" } };
        DdRum.StartView("home-key", "Home", ctx, 1234567890L);

        Assert.Single(bridge.StartViewCalls);
        Assert.Equal("home-key", bridge.StartViewCalls[0].Key);
        Assert.Equal("Home", bridge.StartViewCalls[0].Name);
        Assert.Equal("home", bridge.StartViewCalls[0].Context["screen"]);
        Assert.Equal(1234567890L, bridge.StartViewCalls[0].TimestampMs);
    }

    [Fact]
    public void StartView_DefaultsContextToEmptyDict()
    {
        DdRum.StartView("key", "Name");
        Assert.Empty(bridge.StartViewCalls[0].Context);
    }

    [Fact]
    public void StopView_PassesAllParametersToBridge()
    {
        DdRum.StopView("home-key", new Dictionary<string, object> { { "duration", 500 } }, 1234567890L);

        Assert.Single(bridge.StopViewCalls);
        Assert.Equal("home-key", bridge.StopViewCalls[0].Key);
        Assert.Equal(500, bridge.StopViewCalls[0].Context["duration"]);
    }

    // --- Actions -------------------------------------------------------------

    [Fact]
    public void StartAction_PassesAllParametersToBridge()
    {
        DdRum.StartAction(RumActionType.Tap, "Login Button", new Dictionary<string, object> { { "target", "login" } }, 100L);

        Assert.Single(bridge.StartActionCalls);
        Assert.Equal(RumActionType.Tap, bridge.StartActionCalls[0].Type);
        Assert.Equal("Login Button", bridge.StartActionCalls[0].Name);
    }

    [Fact]
    public void StopAction_PassesAllParametersToBridge()
    {
        DdRum.StopAction(RumActionType.Tap, "Login Button");

        Assert.Single(bridge.StopActionCalls);
        Assert.Equal(RumActionType.Tap, bridge.StopActionCalls[0].Type);
        Assert.Equal("Login Button", bridge.StopActionCalls[0].Name);
    }

    [Fact]
    public void AddAction_PassesAllParametersToBridge()
    {
        DdRum.AddAction(RumActionType.Custom, "Purchase", new Dictionary<string, object> { { "item", "shoes" } });

        Assert.Single(bridge.AddActionCalls);
        Assert.Equal(RumActionType.Custom, bridge.AddActionCalls[0].Type);
        Assert.Equal("Purchase", bridge.AddActionCalls[0].Name);
        Assert.Equal("shoes", bridge.AddActionCalls[0].Context["item"]);
    }

    // --- Resources -----------------------------------------------------------

    [Fact]
    public void StartResource_PassesAllParametersToBridge()
    {
        DdRum.StartResource("res-1", RumResourceMethod.Get, "https://api.example.com/data", new Dictionary<string, object> { { "api", "v2" } }, 100L);

        Assert.Single(bridge.StartResourceCalls);
        Assert.Equal("res-1", bridge.StartResourceCalls[0].Key);
        Assert.Equal(RumResourceMethod.Get, bridge.StartResourceCalls[0].Method);
        Assert.Equal("https://api.example.com/data", bridge.StartResourceCalls[0].Url);
        Assert.Equal("v2", bridge.StartResourceCalls[0].Context["api"]);
    }

    [Fact]
    public void StopResource_PassesAllParametersToBridge()
    {
        DdRum.StopResource("res-1", 200, RumResourceKind.Xhr, 1024);

        Assert.Single(bridge.StopResourceCalls);
        Assert.Equal("res-1", bridge.StopResourceCalls[0].Key);
        Assert.Equal(200, bridge.StopResourceCalls[0].StatusCode);
        Assert.Equal(RumResourceKind.Xhr, bridge.StopResourceCalls[0].Kind);
        Assert.Equal(1024L, bridge.StopResourceCalls[0].Size);
    }

    [Fact]
    public void StopResource_DefaultsSizeToNegativeOne()
    {
        DdRum.StopResource("res-1", 200, RumResourceKind.Xhr);

        Assert.Equal(-1L, bridge.StopResourceCalls[0].Size);
    }

    // --- Timing --------------------------------------------------------------

    [Fact]
    public void AddTiming_PassesNameToBridge()
    {
        DdRum.AddTiming("time_to_interactive");

        Assert.Single(bridge.AddTimingCalls);
        Assert.Equal("time_to_interactive", bridge.AddTimingCalls[0]);
    }

    [Fact]
    public void AddViewLoadingTime_PassesOverwriteToBridge()
    {
        DdRum.AddViewLoadingTime(true);

        Assert.Single(bridge.AddViewLoadingTimeCalls);
        Assert.True(bridge.AddViewLoadingTimeCalls[0]);
    }

    // --- Session -------------------------------------------------------------

    [Fact]
    public void StopSession_CallsBridge()
    {
        DdRum.StopSession();

        Assert.Equal(1, bridge.StopSessionCallCount);
    }

    // --- View Attributes -----------------------------------------------------

    [Fact]
    public void AddViewAttribute_PassesKeyAndValueToBridge()
    {
        DdRum.AddViewAttribute("theme", "dark");

        Assert.Single(bridge.AddViewAttributeCalls);
        Assert.Equal("theme", bridge.AddViewAttributeCalls[0].Key);
        Assert.Equal("dark", bridge.AddViewAttributeCalls[0].Value);
    }

    [Fact]
    public void RemoveViewAttribute_PassesKeyToBridge()
    {
        DdRum.RemoveViewAttribute("theme");

        Assert.Single(bridge.RemoveViewAttributeCalls);
        Assert.Equal("theme", bridge.RemoveViewAttributeCalls[0]);
    }

    [Fact]
    public void AddViewAttributes_PassesDictionaryToBridge()
    {
        var attrs = new Dictionary<string, object> { { "theme", "dark" }, { "lang", "en" } };
        DdRum.AddViewAttributes(attrs);

        Assert.Single(bridge.AddViewAttributesCalls);
        Assert.Equal("dark", bridge.AddViewAttributesCalls[0]["theme"]);
        Assert.Equal("en", bridge.AddViewAttributesCalls[0]["lang"]);
    }

    [Fact]
    public void RemoveViewAttributes_PassesKeysToBridge()
    {
        DdRum.RemoveViewAttributes(new List<string> { "theme", "lang" });

        Assert.Single(bridge.RemoveViewAttributesCalls);
        Assert.Equal(new List<string> { "theme", "lang" }, bridge.RemoveViewAttributesCalls[0]);
    }
}
