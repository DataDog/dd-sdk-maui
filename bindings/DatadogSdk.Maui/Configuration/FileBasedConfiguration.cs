using System.Collections.Generic;
using System.Text.Json;

namespace DatadogSdk.Maui.Configuration
{
    public static class FileBasedConfiguration
    {
        public static DdSdkConfiguration ParseJsonConfig(string json)
        {
            JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            // Required fields
            string clientToken = GetRequiredString(root, "ClientToken");
            string environment = GetRequiredString(root, "Environment");

            var config = new DdSdkConfiguration
            {
                ClientToken = clientToken,
                Environment = environment
            };

            // Optional string fields
            if (TryGetString(root, "Service", out string? service))
                config.Service = service;

            if (TryGetString(root, "Version", out string? version))
                config.Version = version;

            if (TryGetString(root, "VersionSuffix", out string? versionSuffix))
                config.VersionSuffix = versionSuffix;

            // Optional enum fields
            if (TryParseEnum<DatadogSite>(root, "Site", out DatadogSite site))
                config.Site = site;

            if (TryParseEnum<TrackingConsent>(root, "TrackingConsent", out TrackingConsent consent))
                config.TrackingConsent = consent;

            if (TryParseEnum<SdkVerbosity>(root, "Verbosity", out SdkVerbosity verbosity))
                config.Verbosity = verbosity;

            if (TryParseEnum<BatchSize>(root, "BatchSize", out BatchSize batchSize))
                config.BatchSize = batchSize;

            if (TryParseEnum<UploadFrequency>(root, "UploadFrequency", out UploadFrequency uploadFrequency))
                config.UploadFrequency = uploadFrequency;

            if (TryParseEnum<BatchProcessingLevel>(root, "BatchProcessingLevel", out BatchProcessingLevel batchProcessingLevel))
                config.BatchProcessingLevel = batchProcessingLevel;

            // Optional additional configuration
            if (root.TryGetProperty("AdditionalConfiguration", out JsonElement additionalElement)
                && additionalElement.ValueKind == JsonValueKind.Object)
            {
                config.AdditionalConfiguration = ParseAdditionalConfiguration(additionalElement);
            }

            return config;
        }

        private static string GetRequiredString(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out JsonElement element)
                || element.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(element.GetString()))
            {
                throw new ArgumentException($"Required property '{propertyName}' is missing or empty.");
            }

            return element.GetString()!;
        }

        private static bool TryGetString(JsonElement root, string propertyName, out string? value)
        {
            value = null;
            if (root.TryGetProperty(propertyName, out JsonElement element)
                && element.ValueKind == JsonValueKind.String)
            {
                value = element.GetString();
                return true;
            }
            return false;
        }

        private static bool TryParseEnum<T>(JsonElement root, string propertyName, out T value) where T : struct, Enum
        {
            value = default;
            if (root.TryGetProperty(propertyName, out JsonElement element)
                && element.ValueKind == JsonValueKind.String)
            {
                string? raw = element.GetString();
                if (raw != null && Enum.TryParse(raw, ignoreCase: true, out T parsed))
                {
                    value = parsed;
                    return true;
                }

                throw new ArgumentException(
                    $"Invalid value '{raw}' for property '{propertyName}'. " +
                    $"Valid values are: {string.Join(", ", Enum.GetNames(typeof(T)))}.");
            }
            return false;
        }

        private static Dictionary<string, object> ParseAdditionalConfiguration(JsonElement element)
        {
            var dict = new Dictionary<string, object>();
            foreach (JsonProperty property in element.EnumerateObject())
            {
                dict[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString()!,
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Number when property.Value.TryGetInt64(out long l) => l,
                    JsonValueKind.Number => property.Value.GetDouble(),
                    _ => property.Value.GetRawText()
                };
            }
            return dict;
        }
    }
}
