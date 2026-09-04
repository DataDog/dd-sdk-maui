/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

import Foundation
import DatadogCore
import DatadogCrashReporting
import DatadogInternal
import DatadogRUM
@objc(DdRum)
public class DdRum: NSObject {

    // Dependencies - injectable for testing
    private static var rumModule: RumModuleProtocol = RealRumModule()

    // Stored for future resource tracking configuration
    static var initialResourceThreshold: Double? = nil

    // Cached session ID, updated via onSessionStart callback (RUM queue) and read
    // synchronously by the C# layer on the network thread — protect with a lock.
    private static let sessionIdLock = NSLock()
    private static var _cachedSessionId: String? = nil
    static var cachedSessionId: String? {
        get {
            sessionIdLock.lock()
            defer { sessionIdLock.unlock() }
            return _cachedSessionId
        }
        set {
            sessionIdLock.lock()
            defer { sessionIdLock.unlock() }
            _cachedSessionId = newValue
        }
    }

    // For testing: inject dependencies
    static func setRumModule(_ module: RumModuleProtocol) {
        rumModule = module
    }

    // Reset to production dependencies (for test cleanup)
    static func resetDependencies() {
        rumModule = RealRumModule()
        initialResourceThreshold = nil
        cachedSessionId = nil
    }

    // MARK: - Mapping helpers

    static func mapVitalsUpdateFrequency(_ frequency: String) -> RUM.Configuration.VitalsFrequency? {
        switch frequency.lowercased() {
        case "never": return nil
        case "rare": return .rare
        case "average": return .average
        case "frequent": return .frequent
        default: return .average
        }
    }

    static func mapTracingHeaderType(_ type: String) -> TracingHeaderType {
        switch type.lowercased() {
        case "b3": return .b3
        case "b3multi": return .b3multi
        case "tracecontext": return .tracecontext
        default: return .datadog
        }
    }

    static func mapErrorSource(_ source: String) -> RUMErrorSource {
        switch source.lowercased() {
        case "network": return .network
        case "source": return .source
        case "console": return .console
        case "webview": return .webview
        default: return .custom
        }
    }

    static func mapActionType(_ type: String) -> RUMActionType {
        switch type.lowercased() {
        case "tap", "click": return .tap
        case "scroll": return .scroll
        case "swipe": return .swipe
        default: return .custom
        }
    }

    static func mapResourceMethod(_ method: String) -> RUMMethod {
        switch method.lowercased() {
        case "post": return .post
        case "put": return .put
        case "delete": return .delete
        case "head": return .head
        case "patch": return .patch
        default: return .get
        }
    }

    static func mapResourceKind(_ kind: String) -> RUMResourceType {
        switch kind.lowercased() {
        case "xhr": return .xhr
        case "native": return .native
        case "fetch": return .fetch
        case "document": return .document
        case "beacon": return .beacon
        case "image": return .image
        case "font": return .font
        case "css": return .css
        case "media": return .media
        case "js": return .js
        default: return .other
        }
    }

    static func mapFailureReason(_ reason: String) -> RUMFeatureOperationFailureReason {
        switch reason.lowercased() {
        case "abandoned": return .abandoned
        case "other": return .other
        default: return .error
        }
    }

    private static func buildAttributes(from context: NSDictionary, timestampMs: Int64) -> [AttributeKey: AttributeValue] {
        var attributes: [AttributeKey: AttributeValue] = [:]
        if let contextDict = context as? [String: Any] {
            for (key, value) in contextDict {
                attributes[key] = AnyEncodable(value)
            }
        }
        if timestampMs > 0 {
            attributes["_dd.timestamp"] = timestampMs
        }
        return attributes
    }

    // MARK: - Enable

    /// Enable the RUM module with configuration dictionary
    /// - Parameter configuration: NSDictionary with configuration values
    @objc(enableRum:)
    public static func enableRum(configuration: NSDictionary) {
        guard let config = configuration as? [String: Any] else {
            print("[Datadog] DdRum.enableRum: Invalid configuration dictionary.")
            return
        }

        guard let applicationId = config["applicationId"] as? String, !applicationId.isEmpty else {
            print("[Datadog] DdRum.enableRum: applicationId is required.")
            return
        }

        var rumConfig = RUM.Configuration(applicationID: applicationId)

        // Session sample rate
        if let sessionSampleRate = config["sessionSampleRate"] as? Double {
            rumConfig.sessionSampleRate = Float(sessionSampleRate)
        }

        // Telemetry sample rate
        if let telemetrySampleRate = config["telemetrySampleRate"] as? Double {
            rumConfig.telemetrySampleRate = Float(telemetrySampleRate)
        }

        // Configuration telemetry sample rate (extra sampler, applied on top of
        // telemetrySampleRate, controls the rate of `_dd.configuration` events).
        // The setter lives on the `_internal` extension of RUM.Configuration — the
        // same access point dd-sdk-flutter uses for the equivalent override.
        if let configurationTelemetrySampleRate = config["configurationTelemetrySampleRate"] as? Double {
            rumConfig._internal_mutation { $0.configurationTelemetrySampleRate = Float(configurationTelemetrySampleRate) }
        }

        // Vitals update frequency
        if let vitalsStr = config["vitalsUpdateFrequency"] as? String {
            rumConfig.vitalsUpdateFrequency = mapVitalsUpdateFrequency(vitalsStr)
        }

        // Native view tracking
        if let nativeViewTracking = config["nativeViewTracking"] as? Bool, nativeViewTracking {
            rumConfig.uiKitViewsPredicate = DefaultUIKitRUMViewsPredicate()
        }

        // Native interaction tracking
        if let nativeInteractionTracking = config["nativeInteractionTracking"] as? Bool, nativeInteractionTracking {
            rumConfig.uiKitActionsPredicate = DefaultUIKitRUMActionsPredicate()
        }

        // Long task threshold (ms -> seconds)
        if let longTaskMs = config["nativeLongTaskThresholdMs"] as? Double {
            rumConfig.longTaskThreshold = longTaskMs / 1_000.0
        }

        // Track frustrations
        if let trackFrustrations = config["trackFrustrations"] as? Bool {
            rumConfig.trackFrustrations = trackFrustrations
        }

        // Track background events
        if let trackBackgroundEvents = config["trackBackgroundEvents"] as? Bool {
            rumConfig.trackBackgroundEvents = trackBackgroundEvents
        }

        // App hang threshold (iOS only)
        if let appHangThreshold = config["appHangThreshold"] as? Double {
            rumConfig.appHangThreshold = appHangThreshold
        }

        // Track watchdog terminations (iOS only)
        if let trackWatchdogTerminations = config["trackWatchdogTerminations"] as? Bool {
            rumConfig.trackWatchdogTerminations = trackWatchdogTerminations
        }

        // Track memory warnings (iOS only)
        if let trackMemoryWarnings = config["trackMemoryWarnings"] as? Bool {
            rumConfig.trackMemoryWarnings = trackMemoryWarnings
        }

        // Custom endpoint
        if let customEndpoint = config["customEndpoint"] as? String,
           !customEndpoint.isEmpty,
           let url = URL(string: customEndpoint) {
            rumConfig.customEndpoint = url
        }

        // URLSession tracking configuration.
        let automaticResourceTrackingEnabled = config["automaticResourceTracking"] as? Bool ?? true

        if let hosts = DdSdkNativeWrapper.firstPartyHosts {
            let resourceTraceSampleRate = config["resourceTraceSampleRate"] as? Double ?? 20.0

            if automaticResourceTrackingEnabled {
                rumConfig.urlSessionTracking = .init(
                    firstPartyHostsTracing: .traceWithHeaders(
                        hostsWithHeaders: hosts,
                        sampleRate: Float(resourceTraceSampleRate)
                    ),
                    resourceAttributesProvider: { request, _, _, _ in
                        if request.value(forHTTPHeaderField: "x-datadog-tracked-by") == "maui" {
                            return ["_dd.resource.drop_resource": true]
                        }
                        return nil
                    }
                )
            } else {
                rumConfig.urlSessionTracking = .init(
                    firstPartyHostsTracing: .traceWithHeaders(
                        hostsWithHeaders: hosts,
                        sampleRate: Float(resourceTraceSampleRate)
                    )
                )
            }
        } else if automaticResourceTrackingEnabled {
            rumConfig.urlSessionTracking = .init(
                resourceAttributesProvider: { request, _, _, _ in
                    if request.value(forHTTPHeaderField: "x-datadog-tracked-by") == "maui" {
                        return ["_dd.resource.drop_resource": true]
                    }
                    return nil
                }
            )
        }

        // Initial resource threshold (stored for resource tracking configuration)
        initialResourceThreshold = config["initialResourceThreshold"] as? Double

        // Drop native resources that were already tracked at the C# level
        if automaticResourceTrackingEnabled {
            rumConfig.resourceEventMapper = { resourceEvent in
                if resourceEvent.context?.contextInfo["_dd.resource.drop_resource"] != nil {
                    return nil
                }
                return resourceEvent
            }
        }

        // Cache session ID whenever a new RUM session starts so the C# layer can
        // use it for deterministic distributed-tracing sampling (Knuth factor).
        rumConfig.onSessionStart = { sessionId, _ in
            DdRum.cachedSessionId = sessionId
        }

        rumModule.enable(with: rumConfig)

        if DdSdkNativeWrapper.nativeCrashReportEnabled {
            CrashReporting.enable()
        }
    }

    // MARK: - Session ID

    @objc(getCurrentSessionId)
    public static func getCurrentSessionId() -> String? {
        return cachedSessionId
    }

    // MARK: - Add Error

    /// Add a RUM error event
    /// - Parameters:
    ///   - message: Error message
    ///   - source: Error source (e.g., "source", "network", "console", "webview", "custom")
    ///   - stacktrace: Error stacktrace string
    ///   - context: Additional context dictionary
    ///   - timestampMs: Timestamp in milliseconds
    @objc(addError:source:stacktrace:context:timestampMs:)
    public static func addError(
        message: String,
        source: String,
        stacktrace: String,
        context: NSDictionary,
        timestampMs: Int64
    ) {
        var attributes: [AttributeKey: AttributeValue] = [:]

        if let contextDict = context as? [String: Any] {
            for (key, value) in contextDict {
                switch value {
                case let boolVal as Bool:
                    attributes[key] = boolVal
                case let intVal as Int:
                    attributes[key] = intVal
                case let doubleVal as Double:
                    attributes[key] = doubleVal
                case let stringVal as String:
                    attributes[key] = stringVal
                case let int64Val as Int64:
                    attributes[key] = int64Val
                default:
                    attributes[key] = String(describing: value)
                }
            }
        }

        if timestampMs > 0 {
            attributes["_dd.timestamp"] = timestampMs
        }

        attributes["_dd.error.source_type"] = "maui"

        rumModule.addError(
            message: message,
            source: mapErrorSource(source),
            stacktrace: stacktrace,
            attributes: attributes
        )
    }

    // MARK: - Views

    @objc(startView:name:context:timestampMs:)
    public static func startView(_ key: String, name: String, context: NSDictionary, timestampMs: Int64) {
        let attributes = buildAttributes(from: context, timestampMs: timestampMs)
        rumModule.startView(key: key, name: name, attributes: attributes)
    }

    @objc(stopView:context:timestampMs:)
    public static func stopView(_ key: String, context: NSDictionary, timestampMs: Int64) {
        let attributes = buildAttributes(from: context, timestampMs: timestampMs)
        rumModule.stopView(key: key, attributes: attributes)
    }

    // MARK: - Actions

    @objc(startAction:name:context:timestampMs:)
    public static func startAction(_ type: String, name: String, context: NSDictionary, timestampMs: Int64) {
        let attributes = buildAttributes(from: context, timestampMs: timestampMs)
        rumModule.startAction(type: mapActionType(type), name: name, attributes: attributes)
    }

    @objc(stopAction:name:context:timestampMs:)
    public static func stopAction(_ type: String, name: String, context: NSDictionary, timestampMs: Int64) {
        let attributes = buildAttributes(from: context, timestampMs: timestampMs)
        rumModule.stopAction(type: mapActionType(type), name: name, attributes: attributes)
    }

    @objc(addAction:name:context:timestampMs:)
    public static func addAction(_ type: String, name: String, context: NSDictionary, timestampMs: Int64) {
        let attributes = buildAttributes(from: context, timestampMs: timestampMs)
        rumModule.addAction(type: mapActionType(type), name: name, attributes: attributes)
    }

    // MARK: - Resources

    @objc(startResource:method:url:context:timestampMs:)
    public static func startResource(_ key: String, method: String, url: String, context: NSDictionary, timestampMs: Int64) {
        let attributes = buildAttributes(from: context, timestampMs: timestampMs)
        rumModule.startResource(resourceKey: key, httpMethod: mapResourceMethod(method), urlString: url, attributes: attributes)
    }

    @objc(stopResource:statusCode:kind:size:context:timestampMs:)
    public static func stopResource(_ key: String, statusCode: Int, kind: String, size: Int64, context: NSDictionary, timestampMs: Int64) {
        let attributes = buildAttributes(from: context, timestampMs: timestampMs)
        let resourceSize: Int64? = size >= 0 ? size : nil
        rumModule.stopResource(resourceKey: key, statusCode: statusCode, kind: mapResourceKind(kind), size: resourceSize, attributes: attributes)
    }

    // MARK: - Timing

    @objc(addTiming:)
    public static func addTiming(_ name: String) {
        rumModule.addTiming(name: name)
    }

    @objc(addViewLoadingTime:)
    public static func addViewLoadingTime(_ overwrite: Bool) {
        rumModule.addViewLoadingTime(overwrite: overwrite)
    }

    // MARK: - Session

    @objc
    public static func stopSession() {
        rumModule.stopSession()
    }

    // MARK: - View Attributes

    @objc(addViewAttribute:value:)
    public static func addViewAttribute(_ key: String, value: Any) {
        rumModule.addViewAttribute(forKey: key, value: AnyEncodable(value))
    }

    @objc(removeViewAttribute:)
    public static func removeViewAttribute(_ key: String) {
        rumModule.removeViewAttribute(forKey: key)
    }

    @objc(addViewAttributes:)
    public static func addViewAttributes(_ attributes: NSDictionary) {
        var encodable: [AttributeKey: AttributeValue] = [:]
        if let dict = attributes as? [String: Any] {
            for (key, value) in dict {
                encodable[key] = AnyEncodable(value)
            }
        }
        rumModule.addViewAttributes(encodable)
    }

    @objc(removeViewAttributes:)
    public static func removeViewAttributes(_ keys: NSArray) {
        guard let keyList = keys as? [String] else { return }
        rumModule.removeViewAttributes(forKeys: keyList)
    }

    // MARK: - Feature Operations

    @objc(startFeatureOperation:operationKey:context:)
    public static func startFeatureOperation(_ name: String, operationKey: String?, context: NSDictionary) {
        let attributes = buildAttributes(from: context, timestampMs: 0)
        rumModule.startFeatureOperation(name: name, operationKey: operationKey, attributes: attributes)
    }

    @objc(succeedFeatureOperation:operationKey:context:)
    public static func succeedFeatureOperation(_ name: String, operationKey: String?, context: NSDictionary) {
        let attributes = buildAttributes(from: context, timestampMs: 0)
        rumModule.succeedFeatureOperation(name: name, operationKey: operationKey, attributes: attributes)
    }

    @objc(failFeatureOperation:operationKey:reason:context:)
    public static func failFeatureOperation(_ name: String, operationKey: String?, reason: String, context: NSDictionary) {
        let attributes = buildAttributes(from: context, timestampMs: 0)
        rumModule.failFeatureOperation(name: name, operationKey: operationKey, reason: mapFailureReason(reason), attributes: attributes)
    }
}
