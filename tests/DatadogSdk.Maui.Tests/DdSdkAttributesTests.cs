using DatadogSdk.Maui;
using Xunit;

namespace DatadogSdk.Maui.Tests;

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
        DdSdk.AddAttribute("user.plan", "premium");

        Assert.Single(bridge.AddAttributeCalls);
        Assert.Equal("user.plan", bridge.AddAttributeCalls[0].Key);
        Assert.Equal("premium", bridge.AddAttributeCalls[0].Value);
    }

    [Fact]
    public void AddAttribute_StoresValueInLocalDictionary()
    {
        DdSdk.AddAttribute("user.plan", "premium");

        var attrs = DdSdk.GetAttributes();
        Assert.Equal("premium", attrs["user.plan"]);
    }

    [Fact]
    public void AddAttribute_OverwritesExistingKey()
    {
        DdSdk.AddAttribute("user.plan", "free");
        DdSdk.AddAttribute("user.plan", "premium");

        var attrs = DdSdk.GetAttributes();
        Assert.Equal("premium", attrs["user.plan"]);
        Assert.Equal(2, bridge.AddAttributeCalls.Count);
    }

    [Fact]
    public void AddAttribute_SupportsMultipleKeys()
    {
        DdSdk.AddAttribute("user.plan", "premium");
        DdSdk.AddAttribute("app.version", "2.0");

        var attrs = DdSdk.GetAttributes();
        Assert.Equal(2, attrs.Count);
        Assert.Equal("premium", attrs["user.plan"]);
        Assert.Equal("2.0", attrs["app.version"]);
    }

    [Fact]
    public void AddAttribute_SupportsNumericValues()
    {
        DdSdk.AddAttribute("item.count", 42);
        DdSdk.AddAttribute("price", 9.99);

        var attrs = DdSdk.GetAttributes();
        Assert.Equal(42, attrs["item.count"]);
        Assert.Equal(9.99, attrs["price"]);
    }

    [Fact]
    public void AddAttribute_SupportsBooleanValues()
    {
        DdSdk.AddAttribute("is.premium", true);

        var attrs = DdSdk.GetAttributes();
        Assert.Equal(true, attrs["is.premium"]);
    }

    // --- AddAttributes (plural) -------------------------------------------------

    [Fact]
    public void AddAttributes_PassesDictionaryToNative()
    {
        var attrs = new Dictionary<string, object>
        {
            { "user.plan", "premium" },
            { "app.version", "2.0" }
        };

        DdSdk.AddAttributes(attrs);

        Assert.Single(bridge.AddAttributesCalls);
        Assert.Equal("premium", bridge.AddAttributesCalls[0]["user.plan"]);
        Assert.Equal("2.0", bridge.AddAttributesCalls[0]["app.version"]);
    }

    [Fact]
    public void AddAttributes_StoresAllValuesInLocalDictionary()
    {
        DdSdk.AddAttributes(new Dictionary<string, object>
        {
            { "user.plan", "premium" },
            { "app.version", "2.0" }
        });

        var attrs = DdSdk.GetAttributes();
        Assert.Equal(2, attrs.Count);
        Assert.Equal("premium", attrs["user.plan"]);
        Assert.Equal("2.0", attrs["app.version"]);
    }

    // --- RemoveAttribute -----------------------------------------------------

    [Fact]
    public void RemoveAttribute_PassesKeyToNative()
    {
        DdSdk.RemoveAttribute("user.plan");

        Assert.Single(bridge.RemoveAttributeCalls);
        Assert.Equal("user.plan", bridge.RemoveAttributeCalls[0]);
    }

    [Fact]
    public void RemoveAttribute_RemovesFromLocalDictionary()
    {
        DdSdk.AddAttribute("user.plan", "premium");
        DdSdk.RemoveAttribute("user.plan");

        var attrs = DdSdk.GetAttributes();
        Assert.False(attrs.ContainsKey("user.plan"));
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
