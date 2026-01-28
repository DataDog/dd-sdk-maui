#if ANDROID
namespace Datadog.Maui;

/// <summary>
/// Android-specific partial implementation for RUM platform instance creation.
/// </summary>
public static partial class Rum
{
    /// <inheritdoc/>
    public static partial bool Enable(string applicationId, float sampleRate)
    {
        return Com.Datadog.Maui.DatadogMauiWrapper.EnableRum(applicationId, sampleRate);
    }

    private static partial IRum CreatePlatformInstance() => new RumAndroid();
}

/// <summary>
/// Android implementation of the RUM interface.
/// </summary>
internal sealed class RumAndroid : IRum
{
    /// <inheritdoc/>
    public void StartView(string key, string? name = null, IDictionary<string, object>? attributes = null)
    {
        var viewName = name ?? key;
        var attrs = ConvertAttributes(attributes);
        Com.Datadog.Maui.DatadogMauiWrapper.StartView(key, viewName, attrs);
    }

    /// <inheritdoc/>
    public void StopView(string key, IDictionary<string, object>? attributes = null)
    {
        var attrs = ConvertAttributes(attributes);
        Com.Datadog.Maui.DatadogMauiWrapper.StopView(key, attrs);
    }

    /// <inheritdoc/>
    public void AddAction(RumActionType type, string name, IDictionary<string, object>? attributes = null)
    {
        var typeString = MapActionType(type);
        var attrs = ConvertAttributes(attributes);
        Com.Datadog.Maui.DatadogMauiWrapper.AddAction(typeString, name, attrs);
    }

    /// <inheritdoc/>
    public void AddError(string message, RumErrorSource source, string? stackTrace = null, IDictionary<string, object>? attributes = null)
    {
        var sourceString = MapErrorSource(source);
        var attrs = ConvertAttributes(attributes);
        Com.Datadog.Maui.DatadogMauiWrapper.AddError(message, sourceString, stackTrace, attrs);
    }

    /// <inheritdoc/>
    public void StartResource(string key, string httpMethod, string url, IDictionary<string, object>? attributes = null)
    {
        var attrs = ConvertAttributes(attributes);
        Com.Datadog.Maui.DatadogMauiWrapper.StartResource(key, httpMethod, url, attrs);
    }

    /// <inheritdoc/>
    public void StopResource(string key, int statusCode, long? size = null, IDictionary<string, object>? attributes = null)
    {
        var attrs = ConvertAttributes(attributes);
        var sizeValue = size ?? -1L;
        Com.Datadog.Maui.DatadogMauiWrapper.StopResource(key, statusCode, sizeValue, attrs);
    }

    /// <inheritdoc/>
    public void StopResourceWithError(string key, string message, IDictionary<string, object>? attributes = null)
    {
        var attrs = ConvertAttributes(attributes);
        Com.Datadog.Maui.DatadogMauiWrapper.StopResourceWithError(key, message, attrs);
    }

    private static string MapActionType(RumActionType type) => type switch
    {
        RumActionType.Tap => "TAP",
        RumActionType.Click => "CLICK",
        RumActionType.Scroll => "SCROLL",
        RumActionType.Swipe => "SWIPE",
        RumActionType.Custom => "CUSTOM",
        _ => "CUSTOM"
    };

    private static string MapErrorSource(RumErrorSource source) => source switch
    {
        RumErrorSource.Source => "SOURCE",
        RumErrorSource.Network => "NETWORK",
        RumErrorSource.WebView => "WEBVIEW",
        RumErrorSource.Console => "CONSOLE",
        RumErrorSource.Custom => "CUSTOM",
        _ => "SOURCE"
    };

    private static IDictionary<string, Java.Lang.Object>? ConvertAttributes(IDictionary<string, object>? attributes)
    {
        if (attributes == null || attributes.Count == 0)
        {
            return null;
        }

        var result = new Dictionary<string, Java.Lang.Object>();
        foreach (var kvp in attributes)
        {
            var value = ConvertToJavaObject(kvp.Value);
            if (value != null)
            {
                result[kvp.Key] = value;
            }
        }

        return result;
    }

    private static Java.Lang.Object? ConvertToJavaObject(object? value)
    {
        return value switch
        {
            null => null,
            string s => new Java.Lang.String(s),
            int i => Java.Lang.Integer.ValueOf(i),
            long l => Java.Lang.Long.ValueOf(l),
            float f => Java.Lang.Float.ValueOf(f),
            double d => Java.Lang.Double.ValueOf(d),
            bool b => Java.Lang.Boolean.ValueOf(b),
            _ => new Java.Lang.String(value?.ToString() ?? string.Empty)
        };
    }
}
#endif
