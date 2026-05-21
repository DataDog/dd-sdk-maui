/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using DatadogSdk.Maui.Configuration;

namespace DatadogSdk.Maui.AutoTracking
{
    /// <summary>
    /// Automatically tracks HTTP requests as RUM resources via DiagnosticListener.
    /// Subscribes to HttpHandlerDiagnosticListener which fires for all HttpClient requests.
    /// </summary>
    internal class DdAutoResourceTracker : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>
    {
        private readonly List<IDisposable> _subscriptions = new();

        // Cache PropertyInfo per payload type for reflection on internal sealed types.
        // Each diagnostic event has a different payload type (ActivityStartData, ActivityStopData, etc.)
        // so we cache per (Type, PropertyName) pair.
        private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> _propertyCache = new();

        // Key for storing resource tracking key in HttpRequestMessage.Options
        private static readonly HttpRequestOptionsKey<string> ResourceKeyOption = new("dd-resource-key");
        private static readonly HttpRequestOptionsKey<bool> TrackedByMauiOption = new("dd-tracked-by-maui");

        internal DdAutoResourceTracker() { }

        /// <summary>
        /// Start tracking HTTP requests by subscribing to DiagnosticListener.
        /// </summary>
        internal void Start()
        {
            _subscriptions.Add(DiagnosticListener.AllListeners.Subscribe(this));
            InternalLog.Log("DdAutoResourceTracker: Started automatic resource tracking", SdkVerbosity.DEBUG);
        }

        /// <summary>
        /// Stop tracking and dispose all subscriptions.
        /// </summary>
        internal void Stop()
        {
            foreach (var sub in _subscriptions)
            {
                sub.Dispose();
            }
            _subscriptions.Clear();
            InternalLog.Log("DdAutoResourceTracker: Stopped automatic resource tracking", SdkVerbosity.DEBUG);
        }

        // IObserver<DiagnosticListener> — called for each DiagnosticListener source
        public void OnNext(DiagnosticListener listener)
        {
            InternalLog.Log($"DdAutoResourceTracker: DiagnosticListener discovered: {listener.Name}", SdkVerbosity.DEBUG);

            if (listener.Name == "HttpHandlerDiagnosticListener")
            {
                _subscriptions.Add(listener.Subscribe(this));
                InternalLog.Log("DdAutoResourceTracker: Subscribed to HttpHandlerDiagnosticListener", SdkVerbosity.INFO);
            }
        }

        // IObserver<KeyValuePair<string, object?>> — called for each HTTP diagnostic event
        public void OnNext(KeyValuePair<string, object?> kvp)
        {
            try
            {
                switch (kvp.Key)
                {
                    case "System.Net.Http.HttpRequestOut.Start":
                        OnRequestStart(kvp.Value);
                        break;
                    case "System.Net.Http.HttpRequestOut.Stop":
                        OnRequestStop(kvp.Value);
                        break;
                    case "System.Net.Http.Exception":
                        OnRequestException(kvp.Value);
                        break;
                }
            }
            catch (Exception ex)
            {
                InternalLog.Log($"DdAutoResourceTracker: Error processing event {kvp.Key}: {ex.Message}", SdkVerbosity.ERROR);
                InternalTelemetry.Error($"DdAutoResourceTracker: Error processing event {kvp.Key}", ex);
            }
        }

        private void OnRequestStart(object? payload)
        {
            var request = GetProperty<HttpRequestMessage>(payload, "Request");
            if (request?.RequestUri == null) return;

            var key = Guid.NewGuid().ToString();
            request.Options.Set(ResourceKeyOption, key);
            request.Options.Set(TrackedByMauiOption, true);

            // Add marker header so iOS native SDK skips duplicate tracking
            request.Headers.TryAddWithoutValidation("x-datadog-tracked-by", "maui");

            var method = MapHttpMethod(request.Method);

            DdRum.StartResource(key, method, request.RequestUri.ToString());
        }

        private void OnRequestStop(object? payload)
        {
            var request = GetProperty<HttpRequestMessage>(payload, "Request");
            var response = GetProperty<HttpResponseMessage>(payload, "Response");

            if (request == null) return;
            if (!request.Options.TryGetValue(ResourceKeyOption, out var key)) return;

            var statusCode = (int)(response?.StatusCode ?? 0);
            var contentType = response?.Content?.Headers?.ContentType?.MediaType;
            var kind = ResourceKindResolver.Resolve(contentType);
            var size = response?.Content?.Headers?.ContentLength ?? -1;
            var method = MapHttpMethod(request.Method);
            var url = request.RequestUri?.ToString() ?? "unknown";

            var additionalAttributes = new Dictionary<string, object>
            {
                ["_dd.resource.source_type"] = "maui"
            };

            // Attach error context set by OnRequestException (for failed requests)
            if (request.Options.TryGetValue(new HttpRequestOptionsKey<string>("dd-error-message"), out var errorMessage))
                additionalAttributes["error.message"] = errorMessage;
            if (request.Options.TryGetValue(new HttpRequestOptionsKey<string>("dd-error-type"), out var errorType))
                additionalAttributes["error.type"] = errorType;

            // ResourceEventMapper is applied inside DdRum.StopResource() — not here,
            // to avoid double-mapping (same pattern as AddAction/ActionEventMapper).
            DdRum.StopResource(key, statusCode, kind, size, additionalAttributes);
        }

        private void OnRequestException(object? payload)
        {
            // .NET's HttpHandlerDiagnosticListener always emits HttpRequestOut.Stop after
            // HttpRequestOut.Exception for the same request. We let OnRequestStop handle
            // the StopResource call (with status 0 when there is no response) to avoid
            // double-stopping the same resource key.
            var request = GetProperty<HttpRequestMessage>(payload, "Request");
            var exception = GetProperty<Exception>(payload, "Exception");

            if (request == null || exception == null) return;

            // Annotate the request options so OnRequestStop can attach error context
            request.Options.Set(new HttpRequestOptionsKey<string>("dd-error-message"), exception.Message);
            request.Options.Set(new HttpRequestOptionsKey<string>("dd-error-type"), exception.GetType().Name);
        }

        /// <summary>
        /// Map System.Net.Http.HttpMethod to RumResourceMethod enum.
        /// </summary>
        private static RumResourceMethod MapHttpMethod(HttpMethod method)
        {
            if (method == HttpMethod.Get) return RumResourceMethod.Get;
            if (method == HttpMethod.Post) return RumResourceMethod.Post;
            if (method == HttpMethod.Put) return RumResourceMethod.Put;
            if (method == HttpMethod.Delete) return RumResourceMethod.Delete;
            if (method == HttpMethod.Head) return RumResourceMethod.Head;
            if (method == HttpMethod.Patch) return RumResourceMethod.Patch;
            if (method == HttpMethod.Options) return RumResourceMethod.Options;
            if (method == HttpMethod.Trace) return RumResourceMethod.Trace;
            return RumResourceMethod.Get;
        }

        /// <summary>
        /// Extract a property from an internal sealed payload type via reflection.
        /// Caches PropertyInfo per (Type, PropertyName) pair since each diagnostic event
        /// uses a different internal sealed payload type.
        /// </summary>
        private static T? GetProperty<T>(object? payload, string propertyName)
        {
            if (payload == null) return default;

            var payloadType = payload.GetType();
            var cacheKey = (payloadType, propertyName);

            if (!_propertyCache.TryGetValue(cacheKey, out var cached))
            {
                cached = payloadType.GetProperty(propertyName);
                _propertyCache.TryAdd(cacheKey, cached);
            }
            if (cached == null) return default;

            return (T?)cached.GetValue(payload);
        }

        public void OnError(Exception error)
        {
            InternalLog.Log($"DdAutoResourceTracker: Observer error: {error.Message}", SdkVerbosity.ERROR);
        }

        public void OnCompleted() { }
    }
}
