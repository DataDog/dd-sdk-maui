/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using Datadog.Maui;
using Datadog.Maui.Configuration;
using Xunit;

namespace Datadog.Maui.Tests;

[Collection("DdRum")]
public class DdRumActionEventMapperTests : IDisposable
{
    private readonly MockRumBridge bridge = new();

    public DdRumActionEventMapperTests()
    {
        DdRum.testBridge = bridge;
        DdRum.ResetForTesting();
    }

    public void Dispose()
    {
        DdRum.testBridge = null;
        DdRum.ResetForTesting();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void AddAction_WithNoMapper_PassesThroughUnchanged()
    {
        DdRum.AddAction(RumActionType.Tap, "Login Button",
            new Dictionary<string, object> { { "screen", "login" } });

        Assert.Single(bridge.AddActionCalls);
        Assert.Equal(RumActionType.Tap, bridge.AddActionCalls[0].Type);
        Assert.Equal("Login Button", bridge.AddActionCalls[0].Name);
    }

    [Fact]
    public void AddAction_WithMapper_AppliesModifications()
    {
        DdRum.actionEventMapper = (evt) =>
        {
            evt.Name = "Modified: " + evt.Name;
            evt.Type = RumActionType.Custom;
            return evt;
        };

        DdRum.AddAction(RumActionType.Tap, "Login Button");

        Assert.Single(bridge.AddActionCalls);
        Assert.Equal(RumActionType.Custom, bridge.AddActionCalls[0].Type);
        Assert.Equal("Modified: Login Button", bridge.AddActionCalls[0].Name);
    }

    [Fact]
    public void AddAction_WithMapperReturningNull_DropsAction()
    {
        DdRum.actionEventMapper = (evt) => null;

        DdRum.AddAction(RumActionType.Tap, "Login Button");

        Assert.Empty(bridge.AddActionCalls);
    }

    [Fact]
    public void AddAction_WithMapperThatThrows_SendsOriginalAction()
    {
        DdRum.actionEventMapper = (evt) => throw new Exception("Mapper error");

        DdRum.AddAction(RumActionType.Tap, "Login Button");

        Assert.Single(bridge.AddActionCalls);
        Assert.Equal("Login Button", bridge.AddActionCalls[0].Name);
    }

    [Fact]
    public void AddAction_MapperCanModifyContext()
    {
        DdRum.actionEventMapper = (evt) =>
        {
            evt.Context["injected"] = "by_mapper";
            return evt;
        };

        DdRum.AddAction(RumActionType.Tap, "Button",
            new Dictionary<string, object> { { "original", "value" } });

        Assert.Equal("value", bridge.AddActionCalls[0].Context["original"]);
        Assert.Equal("by_mapper", bridge.AddActionCalls[0].Context["injected"]);
    }

    [Fact]
    public void AddAction_MapperReceivesCorrectTimestamp()
    {
        long capturedTimestamp = 0;
        DdRum.actionEventMapper = (evt) =>
        {
            capturedTimestamp = evt.TimestampMs;
            return evt;
        };

        DdRum.AddAction(RumActionType.Tap, "Button", timestampMs: 1234567890L);

        Assert.Equal(1234567890L, capturedTimestamp);
    }

    [Fact]
    public void AddAction_MapperCanChangeType()
    {
        DdRum.actionEventMapper = (evt) =>
        {
            evt.Type = RumActionType.Swipe;
            return evt;
        };

        DdRum.AddAction(RumActionType.Tap, "Button");

        Assert.Equal(RumActionType.Swipe, bridge.AddActionCalls[0].Type);
    }
}
