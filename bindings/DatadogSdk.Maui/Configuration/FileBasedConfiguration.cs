/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

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

            // Optional first-party hosts
            if (root.TryGetProperty("FirstPartyHosts", out JsonElement hostsElement)
                && hostsElement.ValueKind == JsonValueKind.Array)
            {
                config.FirstPartyHosts = ParseFirstPartyHosts(hostsElement);
            }

            // Optional boolean fields
            if (root.TryGetProperty("NativeCrashReportEnabled", out JsonElement crashElement)
                && (crashElement.ValueKind == JsonValueKind.True || crashElement.ValueKind == JsonValueKind.False))
            {
                config.NativeCrashReportEnabled = crashElement.GetBoolean();
            }

            // Optional additional configuration
            if (root.TryGetProperty("AdditionalConfiguration", out JsonElement additionalElement)
                && additionalElement.ValueKind == JsonValueKind.Object)
            {
                config.AdditionalConfiguration = ParseAdditionalConfiguration(additionalElement);
            }

            // Optional proxy configuration
            if (root.TryGetProperty("ProxyConfiguration", out JsonElement proxyElement)
                && proxyElement.ValueKind == JsonValueKind.Object)
            {
                config.ProxyConfiguration = ParseProxyConfiguration(proxyElement);
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

        private static List<FirstPartyHost> ParseFirstPartyHosts(JsonElement element)
        {
            var hosts = new List<FirstPartyHost>();
            foreach (JsonElement hostElement in element.EnumerateArray())
            {
                if (hostElement.ValueKind != JsonValueKind.Object)
                    continue;

                string? match = hostElement.TryGetProperty("Match", out JsonElement matchEl)
                    ? matchEl.GetString() : null;

                if (string.IsNullOrWhiteSpace(match))
                    continue;

                var headerTypes = new List<TracingHeaderType>();
                if (hostElement.TryGetProperty("HeaderTypes", out JsonElement typesEl)
                    && typesEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement typeEl in typesEl.EnumerateArray())
                    {
                        string? raw = typeEl.GetString();
                        if (raw != null && Enum.TryParse(raw, ignoreCase: true, out TracingHeaderType parsed))
                        {
                            headerTypes.Add(parsed);
                        }
                    }
                }

                hosts.Add(new FirstPartyHost { Match = match!, HeaderTypes = headerTypes });
            }
            return hosts;
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

        private static ProxyConfiguration ParseProxyConfiguration(JsonElement element)
        {
            // Required: Type
            if (!element.TryGetProperty("Type", out JsonElement typeElement)
                || typeElement.ValueKind != JsonValueKind.String)
            {
                throw new ArgumentException("Required property 'Type' is missing or empty in ProxyConfiguration.");
            }

            string? typeRaw = typeElement.GetString();
            if (typeRaw == null
                || !Enum.TryParse<ProxyType>(typeRaw, ignoreCase: true, out ProxyType proxyType)
                || !Enum.IsDefined(proxyType))
            {
                throw new ArgumentException(
                    $"Invalid value '{typeRaw}' for property 'Type' in ProxyConfiguration. " +
                    $"Valid values are: {string.Join(", ", Enum.GetNames(typeof(ProxyType)))}.");
            }

            // Required: Address
            if (!element.TryGetProperty("Address", out JsonElement addressElement)
                || addressElement.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(addressElement.GetString()))
            {
                throw new ArgumentException("Required property 'Address' is missing or empty in ProxyConfiguration.");
            }

            // Required: Port
            if (!element.TryGetProperty("Port", out JsonElement portElement)
                || portElement.ValueKind != JsonValueKind.Number
                || !portElement.TryGetInt32(out int port))
            {
                throw new ArgumentException("Required property 'Port' is missing or invalid in ProxyConfiguration.");
            }

            if (port < 1 || port > 65535)
            {
                throw new ArgumentException(
                    $"Invalid value '{port}' for property 'Port' in ProxyConfiguration. Must be between 1 and 65535.");
            }

            var proxy = new ProxyConfiguration
            {
                Type = proxyType,
                Address = addressElement.GetString()!,
                Port = port
            };

            // Optional: Username
            if (element.TryGetProperty("Username", out JsonElement usernameElement)
                && usernameElement.ValueKind == JsonValueKind.String)
            {
                proxy.Username = usernameElement.GetString();
            }

            // Optional: Password
            if (element.TryGetProperty("Password", out JsonElement passwordElement)
                && passwordElement.ValueKind == JsonValueKind.String)
            {
                proxy.Password = passwordElement.GetString();
            }

            return proxy;
        }
    }
}
