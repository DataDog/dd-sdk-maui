/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using Datadog.Maui;
using Xunit;

namespace Datadog.Maui.Tests;

[Collection("InternalLog")]
public class DdSdkAttributesTests : IDisposable
{
    private readonly MockNativeSdkBridge bridge = new();

    public DdSdkAttributesTests()
    {
        DdSdk.testBridge = bridge;
        DdSdk.ClearAttributesForTesting();
    }

    public void Dispose()
    {
        DdSdk.testBridge = null;
        DdSdk.ClearAttributesForTesting();
        InternalLog.Verbosity = null;
        GC.SuppressFinalize(this);
    }

    // --- AddAttribute --------------------------------------------------------

    [Fact]
    public void AddAttribute_PassesKeyAndValueToNative()
    {
        DdSdk.AddAttribute("plan", "premium");

        Assert.Single(bridge.AddAttributeCalls);
        Assert.Equal("plan", bridge.AddAttributeCalls[0].Key);
        Assert.Equal("premium", bridge.AddAttributeCalls[0].Value);
    }

    [Fact]
    public void AddAttribute_StoresValueInLocalDictionary()
    {
        DdSdk.AddAttribute("plan", "premium");

        var attrs = DdSdk.GetAttributes();
        Assert.Equal("premium", attrs["plan"]);
    }

    [Fact]
    public void AddAttribute_OverwritesExistingKey()
    {
        DdSdk.AddAttribute("plan", "free");
        DdSdk.AddAttribute("plan", "premium");

        var attrs = DdSdk.GetAttributes();
        Assert.Equal("premium", attrs["plan"]);
        Assert.Equal(2, bridge.AddAttributeCalls.Count);
    }

    [Fact]
    public void AddAttribute_SupportsMultipleKeys()
    {
        DdSdk.AddAttribute("plan", "premium");
        DdSdk.AddAttribute("version", "2.0");

        var attrs = DdSdk.GetAttributes();
        Assert.Equal(2, attrs.Count);
        Assert.Equal("premium", attrs["plan"]);
        Assert.Equal("2.0", attrs["version"]);
    }

    [Fact]
    public void AddAttribute_SupportsNumericValues()
    {
        DdSdk.AddAttribute("item_count", 42);
        DdSdk.AddAttribute("price", 9.99);

        var attrs = DdSdk.GetAttributes();
        Assert.Equal(42, attrs["item_count"]);
        Assert.Equal(9.99, attrs["price"]);
    }

    [Fact]
    public void AddAttribute_SupportsBooleanValues()
    {
        DdSdk.AddAttribute("is_premium", true);

        var attrs = DdSdk.GetAttributes();
        Assert.Equal(true, attrs["is_premium"]);
    }

    // --- AddAttributes (plural) -------------------------------------------------

    [Fact]
    public void AddAttributes_PassesDictionaryToNative()
    {
        var attrs = new Dictionary<string, object>
        {
            { "plan", "premium" },
            { "version", "2.0" }
        };

        DdSdk.AddAttributes(attrs);

        Assert.Single(bridge.AddAttributesCalls);
        Assert.Equal("premium", bridge.AddAttributesCalls[0]["plan"]);
        Assert.Equal("2.0", bridge.AddAttributesCalls[0]["version"]);
    }

    [Fact]
    public void AddAttributes_StoresAllValuesInLocalDictionary()
    {
        DdSdk.AddAttributes(new Dictionary<string, object>
        {
            { "plan", "premium" },
            { "version", "2.0" }
        });

        var attrs = DdSdk.GetAttributes();
        Assert.Equal(2, attrs.Count);
        Assert.Equal("premium", attrs["plan"]);
        Assert.Equal("2.0", attrs["version"]);
    }

    // --- RemoveAttribute -----------------------------------------------------

    [Fact]
    public void RemoveAttribute_PassesKeyToNative()
    {
        DdSdk.RemoveAttribute("plan");

        Assert.Single(bridge.RemoveAttributeCalls);
        Assert.Equal("plan", bridge.RemoveAttributeCalls[0]);
    }

    [Fact]
    public void RemoveAttribute_RemovesFromLocalDictionary()
    {
        DdSdk.AddAttribute("plan", "premium");
        DdSdk.RemoveAttribute("plan");

        var attrs = DdSdk.GetAttributes();
        Assert.False(attrs.ContainsKey("plan"));
    }

    [Fact]
    public void RemoveAttribute_NonExistentKey_DoesNotThrow()
    {
        DdSdk.RemoveAttribute("nonexistent");

        Assert.Single(bridge.RemoveAttributeCalls);
        Assert.Empty(DdSdk.GetAttributes());
    }

    // --- RemoveAttributes (plural) -----------------------------------------------

    [Fact]
    public void RemoveAttributes_PassesKeysToNative()
    {
        DdSdk.RemoveAttributes(new List<string> { "key1", "key2" });

        Assert.Single(bridge.RemoveAttributesCalls);
        Assert.Equal(new List<string> { "key1", "key2" }, bridge.RemoveAttributesCalls[0]);
    }

    [Fact]
    public void RemoveAttributes_RemovesAllFromLocalDictionary()
    {
        DdSdk.AddAttribute("key1", "val1");
        DdSdk.AddAttribute("key2", "val2");
        DdSdk.AddAttribute("key3", "val3");
        DdSdk.RemoveAttributes(new List<string> { "key1", "key3" });

        var attrs = DdSdk.GetAttributes();
        Assert.Single(attrs);
        Assert.Equal("val2", attrs["key2"]);
    }

    // --- GetAttributes -------------------------------------------------------

    [Fact]
    public void GetAttributes_ReturnsEmptyDictionaryWhenNoneSet()
    {
        var attrs = DdSdk.GetAttributes();
        Assert.Empty(attrs);
    }

    [Fact]
    public void GetAttributes_ReturnsCopyNotReference()
    {
        DdSdk.AddAttribute("key", "value");
        var attrs = DdSdk.GetAttributes();
        attrs["injected"] = "hack";

        var attrs2 = DdSdk.GetAttributes();
        Assert.False(attrs2.ContainsKey("injected"));
    }
}
