using DatadogSdk.Maui;
using DatadogSdk.Maui.Configuration;
using Xunit;

namespace DatadogSdk.Maui.Tests;

[Collection("InternalLog")]
public class DdSdkUserInfoTests : IDisposable
{
    private readonly MockNativeSdkBridge bridge = new();

    public DdSdkUserInfoTests()
    {
        DdSdk.testBridge = bridge;
        DdSdk.ClearUserAndAccountInfoForTesting();
    }

    public void Dispose()
    {
        DdSdk.testBridge = null;
        DdSdk.ClearUserAndAccountInfoForTesting();
        InternalLog.Verbosity = null;
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void SetUserInfo_PassesAllFieldsToNative()
    {
        var extra = new Dictionary<string, object> { { "plan", "premium" } };
        DdSdk.SetUserInfo("user-123", "John", "john@example.com", extra);

        Assert.Single(bridge.SetUserInfoCalls);
        Assert.Equal("user-123", bridge.SetUserInfoCalls[0].Id);
        Assert.Equal("John", bridge.SetUserInfoCalls[0].Name);
        Assert.Equal("john@example.com", bridge.SetUserInfoCalls[0].Email);
        Assert.Equal("premium", bridge.SetUserInfoCalls[0].ExtraInfo["plan"]);
    }

    [Fact]
    public void SetUserInfo_StoresLocally()
    {
        DdSdk.SetUserInfo("user-123", "John", "john@example.com");

        var user = DdSdk.GetUserInfo();
        Assert.NotNull(user);
        Assert.Equal("user-123", user!.Id);
        Assert.Equal("John", user.Name);
        Assert.Equal("john@example.com", user.Email);
    }

    [Fact]
    public void SetUserInfo_WithOptionalNulls_PassesNullsToNative()
    {
        DdSdk.SetUserInfo("user-123");

        Assert.Single(bridge.SetUserInfoCalls);
        Assert.Equal("user-123", bridge.SetUserInfoCalls[0].Id);
        Assert.Null(bridge.SetUserInfoCalls[0].Name);
        Assert.Null(bridge.SetUserInfoCalls[0].Email);
    }

    [Fact]
    public void SetUserInfo_OverwritesPreviousUser()
    {
        DdSdk.SetUserInfo("user-1", "First");
        DdSdk.SetUserInfo("user-2", "Second");

        var user = DdSdk.GetUserInfo();
        Assert.Equal("user-2", user!.Id);
        Assert.Equal("Second", user.Name);
    }

    [Fact]
    public void AddUserExtraInfo_MergesIntoExistingUser()
    {
        DdSdk.SetUserInfo("user-123", extraInfo: new Dictionary<string, object> { { "plan", "free" } });
        DdSdk.AddUserExtraInfo(new Dictionary<string, object> { { "tier", "gold" } });

        var user = DdSdk.GetUserInfo();
        Assert.Equal("free", user!.ExtraInfo!["plan"]);
        Assert.Equal("gold", user.ExtraInfo!["tier"]);
    }

    [Fact]
    public void AddUserExtraInfo_OverwritesExistingKeys()
    {
        DdSdk.SetUserInfo("user-123", extraInfo: new Dictionary<string, object> { { "plan", "free" } });
        DdSdk.AddUserExtraInfo(new Dictionary<string, object> { { "plan", "premium" } });

        var user = DdSdk.GetUserInfo();
        Assert.Equal("premium", user!.ExtraInfo!["plan"]);
    }

    [Fact]
    public void AddUserExtraInfo_WithNoUserSet_DoesNotCallNative()
    {
        DdSdk.AddUserExtraInfo(new Dictionary<string, object> { { "plan", "premium" } });

        Assert.Empty(bridge.AddUserExtraInfoCalls);
    }

    [Fact]
    public void AddUserExtraInfo_PassesToNative()
    {
        DdSdk.SetUserInfo("user-123");
        DdSdk.AddUserExtraInfo(new Dictionary<string, object> { { "plan", "premium" } });

        Assert.Single(bridge.AddUserExtraInfoCalls);
        Assert.Equal("premium", bridge.AddUserExtraInfoCalls[0]["plan"]);
    }

    [Fact]
    public void ClearUserInfo_ClearsLocalState()
    {
        DdSdk.SetUserInfo("user-123", "John");
        DdSdk.ClearUserInfo();

        Assert.Null(DdSdk.GetUserInfo());
    }

    [Fact]
    public void ClearUserInfo_CallsNative()
    {
        DdSdk.ClearUserInfo();
        Assert.Equal(1, bridge.ClearUserInfoCallCount);
    }

    [Fact]
    public void GetUserInfo_ReturnsNullWhenNotSet()
    {
        Assert.Null(DdSdk.GetUserInfo());
    }

    [Fact]
    public void GetUserInfo_ReturnsCopyNotReference()
    {
        DdSdk.SetUserInfo("user-123", "John");
        var user = DdSdk.GetUserInfo();
        user!.Name = "Modified";

        var user2 = DdSdk.GetUserInfo();
        Assert.Equal("John", user2!.Name);
    }
}
