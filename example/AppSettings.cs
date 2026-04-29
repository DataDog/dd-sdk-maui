/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

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
