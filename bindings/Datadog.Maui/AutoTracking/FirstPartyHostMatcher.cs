/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.Text.RegularExpressions;
using Datadog.Maui.Configuration;

namespace Datadog.Maui.AutoTracking
{
    /// <summary>
    /// Matches request URLs against the configured first-party hosts and returns which
    /// TracingHeaderTypes to inject.
    ///
    /// Matching rule: a configured Match of "example.com" matches both
    /// "example.com" and any subdomain such as "api.example.com".
    /// Pattern: ^(.*\.)*(host$)
    /// </summary>
    internal sealed class FirstPartyHostMatcher
    {
        private readonly List<(Regex Regex, TracingHeaderType Type)> _entries;

        internal FirstPartyHostMatcher(List<FirstPartyHost>? hosts)
        {
            _entries = Build(hosts);
        }

        /// <summary>
        /// Returns the list of TracingHeaderTypes to inject for the given URL's host,
        /// or null if the URL does not match any first-party host.
        /// </summary>
        internal List<TracingHeaderType>? GetHeaderTypes(Uri uri)
        {
            var host = uri.Host;
            List<TracingHeaderType>? result = null;

            foreach (var (regex, type) in _entries)
            {
                if (regex.IsMatch(host))
                {
                    result ??= new List<TracingHeaderType>();
                    result.Add(type);
                }
            }

            return result;
        }

        // ── Construction ─────────────────────────────────────────────────────

        private static List<(Regex, TracingHeaderType)> Build(List<FirstPartyHost>? hosts)
        {
            if (hosts == null || hosts.Count == 0)
                return new List<(Regex, TracingHeaderType)>();

            // Group host strings by header type so we emit one Regex per type.
            var byType = new Dictionary<TracingHeaderType, List<string>>();
            foreach (var host in hosts)
            {
                if (string.IsNullOrWhiteSpace(host.Match))
                {
                    continue;
                }

                foreach (var type in host.HeaderTypes)
                {
                    if (!byType.TryGetValue(type, out var list))
                    {
                        list = new List<string>();
                        byType[type] = list;
                    }
                    list.Add(host.Match);
                }
            }

            var entries = new List<(Regex, TracingHeaderType)>(byType.Count);
            foreach (var (type, hostList) in byType)
            {
                try
                {
                    var pattern = BuildPattern(hostList);
                    entries.Add((new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase), type));
                }
                catch (ArgumentException)
                {
                    InternalLog.Log(
                        $"FirstPartyHostMatcher: invalid host pattern for {type}, skipping.",
                        SdkVerbosity.WARN);
                }
            }

            return entries;
        }

        // ^(.*\.)*(host1$|host2$|...)
        // Matches "example.com" and "api.example.com" but not "notexample.com".
        private static string BuildPattern(List<string> hosts)
        {
            var alternatives = string.Join("|", hosts.Select(h => Regex.Escape(h) + "$"));
            return $@"^(.*\.)*({alternatives})";
        }
    }
}
