using DatadogSdk.Maui;
using DatadogSdk.Maui.Configuration;
using Xunit;

namespace DatadogSdk.Maui.Tests;

public class DdRumResourceEventMapperTests
{
    [Fact]
    public void DdRumResourceEvent_StoresAllFields()
    {
        var context = new Dictionary<string, object> { { "key", "value" } };
        var evt = new DdRumResourceEvent("res-1", RumResourceMethod.Get, "https://example.com",
            200, RumResourceKind.Xhr, 1024, context);

        Assert.Equal("res-1", evt.Key);
        Assert.Equal(RumResourceMethod.Get, evt.Method);
        Assert.Equal("https://example.com", evt.Url);
        Assert.Equal(200, evt.StatusCode);
        Assert.Equal(RumResourceKind.Xhr, evt.Kind);
        Assert.Equal(1024, evt.Size);
        Assert.Equal("value", evt.Context["key"]);
    }

    [Fact]
    public void DdRumResourceEvent_FieldsAreMutable()
    {
        var evt = new DdRumResourceEvent("res-1", RumResourceMethod.Get, "https://example.com",
            200, RumResourceKind.Xhr, 1024, new Dictionary<string, object>());

        evt.Key = "modified";
        evt.StatusCode = 404;
        evt.Kind = RumResourceKind.Image;
        evt.Size = 2048;

        Assert.Equal("modified", evt.Key);
        Assert.Equal(404, evt.StatusCode);
        Assert.Equal(RumResourceKind.Image, evt.Kind);
        Assert.Equal(2048, evt.Size);
    }

    [Fact]
    public void ResourceEventMapper_CanModifyEvent()
    {
        Func<DdRumResourceEvent, DdRumResourceEvent?> mapper = (evt) =>
        {
            evt.Kind = RumResourceKind.Native;
            evt.Context["modified"] = true;
            return evt;
        };

        var evt = new DdRumResourceEvent("res-1", RumResourceMethod.Post, "https://api.example.com",
            201, RumResourceKind.Xhr, 512, new Dictionary<string, object>());

        var result = mapper(evt);

        Assert.NotNull(result);
        Assert.Equal(RumResourceKind.Native, result!.Kind);
        Assert.Equal(true, result.Context["modified"]);
    }

    [Fact]
    public void ResourceEventMapper_CanDropEvent()
    {
        Func<DdRumResourceEvent, DdRumResourceEvent?> mapper = (evt) => null;

        var evt = new DdRumResourceEvent("res-1", RumResourceMethod.Get, "https://example.com",
            200, RumResourceKind.Xhr, 1024, new Dictionary<string, object>());

        var result = mapper(evt);

        Assert.Null(result);
    }

    [Fact]
    public void ResourceEventMapper_CanFilterByUrl()
    {
        Func<DdRumResourceEvent, DdRumResourceEvent?> mapper = (evt) =>
        {
            // Drop analytics tracking requests
            if (evt.Url.Contains("analytics")) return null;
            return evt;
        };

        var tracked = mapper(new DdRumResourceEvent("r1", RumResourceMethod.Get, "https://api.example.com/data",
            200, RumResourceKind.Xhr, 100, new Dictionary<string, object>()));
        var dropped = mapper(new DdRumResourceEvent("r2", RumResourceMethod.Get, "https://analytics.example.com/track",
            200, RumResourceKind.Xhr, 50, new Dictionary<string, object>()));

        Assert.NotNull(tracked);
        Assert.Null(dropped);
    }
}
