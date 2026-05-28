/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using DatadogSdk.Maui;
using DatadogSdk.Maui.Configuration;
using Xunit;

namespace DatadogSdk.Maui.Tests;

[Collection("InternalLog")]
public class DdSdkAccountInfoTests : IDisposable
{
    private readonly MockNativeSdkBridge bridge = new();

    public DdSdkAccountInfoTests()
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
    public void SetAccountInfo_PassesAllFieldsToNative()
    {
        var extra = new Dictionary<string, object> { { "tier", "enterprise" } };
        DdSdk.SetAccountInfo("acct-456", "Acme Corp", extra);

        Assert.Single(bridge.SetAccountInfoCalls);
        Assert.Equal("acct-456", bridge.SetAccountInfoCalls[0].Id);
        Assert.Equal("Acme Corp", bridge.SetAccountInfoCalls[0].Name);
        Assert.Equal("enterprise", bridge.SetAccountInfoCalls[0].ExtraInfo["tier"]);
    }

    [Fact]
    public void SetAccountInfo_StoresLocally()
    {
        DdSdk.SetAccountInfo("acct-456", "Acme Corp");

        var account = DdSdk.GetAccountInfo();
        Assert.NotNull(account);
        Assert.Equal("acct-456", account!.Id);
        Assert.Equal("Acme Corp", account.Name);
    }

    [Fact]
    public void SetAccountInfo_WithOptionalNulls_PassesNullsToNative()
    {
        DdSdk.SetAccountInfo("acct-456");

        Assert.Single(bridge.SetAccountInfoCalls);
        Assert.Equal("acct-456", bridge.SetAccountInfoCalls[0].Id);
        Assert.Null(bridge.SetAccountInfoCalls[0].Name);
    }

    [Fact]
    public void AddAccountExtraInfo_MergesIntoExistingAccount()
    {
        DdSdk.SetAccountInfo("acct-456", extraInfo: new Dictionary<string, object> { { "tier", "free" } });
        DdSdk.AddAccountExtraInfo(new Dictionary<string, object> { { "region", "us" } });

        var account = DdSdk.GetAccountInfo();
        Assert.Equal("free", account!.ExtraInfo!["tier"]);
        Assert.Equal("us", account.ExtraInfo!["region"]);
    }

    [Fact]
    public void AddAccountExtraInfo_WithNoAccountSet_DoesNotCallNative()
    {
        DdSdk.AddAccountExtraInfo(new Dictionary<string, object> { { "tier", "enterprise" } });

        Assert.Empty(bridge.AddAccountExtraInfoCalls);
    }

    [Fact]
    public void AddAccountExtraInfo_PassesToNative()
    {
        DdSdk.SetAccountInfo("acct-456");
        DdSdk.AddAccountExtraInfo(new Dictionary<string, object> { { "tier", "enterprise" } });

        Assert.Single(bridge.AddAccountExtraInfoCalls);
        Assert.Equal("enterprise", bridge.AddAccountExtraInfoCalls[0]["tier"]);
    }

    [Fact]
    public void ClearAccountInfo_ClearsLocalState()
    {
        DdSdk.SetAccountInfo("acct-456", "Acme");
        DdSdk.ClearAccountInfo();

        Assert.Null(DdSdk.GetAccountInfo());
    }

    [Fact]
    public void ClearAccountInfo_CallsNative()
    {
        DdSdk.ClearAccountInfo();
        Assert.Equal(1, bridge.ClearAccountInfoCallCount);
    }

    [Fact]
    public void GetAccountInfo_ReturnsNullWhenNotSet()
    {
        Assert.Null(DdSdk.GetAccountInfo());
    }

    [Fact]
    public void GetAccountInfo_ReturnsCopyNotReference()
    {
        DdSdk.SetAccountInfo("acct-456", "Acme");
        var account = DdSdk.GetAccountInfo();
        account!.Name = "Modified";

        var account2 = DdSdk.GetAccountInfo();
        Assert.Equal("Acme", account2!.Name);
    }
}
