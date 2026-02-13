using System.Text.Json.Nodes;

namespace example;

public class AppSettings
{
    private static JsonNode? _instance;

    public static JsonNode Load()
    {
        if (_instance != null) return _instance;

        using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").Result;
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        _instance = JsonNode.Parse(json)
            ?? throw new InvalidOperationException("Failed to parse appsettings.json");
        return _instance;
    }
}
