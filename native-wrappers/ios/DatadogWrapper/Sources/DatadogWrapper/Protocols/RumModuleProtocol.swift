/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

import Foundation
import DatadogInternal
@_spi(Experimental) import DatadogRUM

/// Protocol for the RUM module functionality
/// Allows for dependency injection and testing
protocol RumModuleProtocol {
    /// Enable the RUM module with configuration
    /// - Parameter configuration: Configuration for the RUM module
    func enable(with configuration: RUM.Configuration)

    /// Add a RUM error with message, source, stacktrace, and attributes
    func addError(message: String, source: RUMErrorSource, stacktrace: String?, attributes: [AttributeKey: AttributeValue])

    // Views
    func startView(key: String, name: String, attributes: [AttributeKey: AttributeValue])
    func stopView(key: String, attributes: [AttributeKey: AttributeValue])

    // Actions
    func startAction(type: RUMActionType, name: String, attributes: [AttributeKey: AttributeValue])
    func stopAction(type: RUMActionType, name: String, attributes: [AttributeKey: AttributeValue])
    func addAction(type: RUMActionType, name: String, attributes: [AttributeKey: AttributeValue])

    // Resources
    func startResource(resourceKey: String, httpMethod: RUMMethod, urlString: String, attributes: [AttributeKey: AttributeValue])
    func stopResource(resourceKey: String, statusCode: Int?, kind: RUMResourceType, size: Int64?, attributes: [AttributeKey: AttributeValue])

    // Timing
    func addTiming(name: String)
    func addViewLoadingTime(overwrite: Bool)

    // Session
    func stopSession()

    // View Attributes
    func addViewAttribute(forKey key: AttributeKey, value: AttributeValue)
    func removeViewAttribute(forKey key: AttributeKey)
    func addViewAttributes(_ attributes: [AttributeKey: AttributeValue])
    func removeViewAttributes(forKeys keys: [AttributeKey])

    // Feature Operations
    func startFeatureOperation(name: String, operationKey: String?, attributes: [AttributeKey: AttributeValue])
    func succeedFeatureOperation(name: String, operationKey: String?, attributes: [AttributeKey: AttributeValue])
    func failFeatureOperation(name: String, operationKey: String?, reason: RUMFeatureOperationFailureReason, attributes: [AttributeKey: AttributeValue])
}

/// Production implementation that wraps the real Datadog RUM module
class RealRumModule: RumModuleProtocol {
    func enable(with configuration: RUM.Configuration) {
        RUM.enable(with: configuration)
    }

    func addError(message: String, source: RUMErrorSource, stacktrace: String?, attributes: [AttributeKey: AttributeValue]) {
        RUMMonitor.shared().addError(message: message, type: nil, stack: stacktrace, source: source, attributes: attributes)
    }

    func startView(key: String, name: String, attributes: [AttributeKey: AttributeValue]) {
        RUMMonitor.shared().startView(key: key, name: name, attributes: attributes)
    }

    func stopView(key: String, attributes: [AttributeKey: AttributeValue]) {
        RUMMonitor.shared().stopView(key: key, attributes: attributes)
    }

    func startAction(type: RUMActionType, name: String, attributes: [AttributeKey: AttributeValue]) {
        RUMMonitor.shared().startAction(type: type, name: name, attributes: attributes)
    }

    func stopAction(type: RUMActionType, name: String, attributes: [AttributeKey: AttributeValue]) {
        RUMMonitor.shared().stopAction(type: type, name: name, attributes: attributes)
    }

    func addAction(type: RUMActionType, name: String, attributes: [AttributeKey: AttributeValue]) {
        RUMMonitor.shared().addAction(type: type, name: name, attributes: attributes)
    }

    func startResource(resourceKey: String, httpMethod: RUMMethod, urlString: String, attributes: [AttributeKey: AttributeValue]) {
        RUMMonitor.shared().startResource(resourceKey: resourceKey, httpMethod: httpMethod, urlString: urlString, attributes: attributes)
    }

    func stopResource(resourceKey: String, statusCode: Int?, kind: RUMResourceType, size: Int64?, attributes: [AttributeKey: AttributeValue]) {
        RUMMonitor.shared().stopResource(resourceKey: resourceKey, statusCode: statusCode, kind: kind, size: size, attributes: attributes)
    }

    func addTiming(name: String) {
        RUMMonitor.shared().addTiming(name: name)
    }

    func addViewLoadingTime(overwrite: Bool) {
        RUMMonitor.shared().addViewLoadingTime(overwrite: overwrite)
    }

    func stopSession() {
        RUMMonitor.shared().stopSession()
    }

    func addViewAttribute(forKey key: AttributeKey, value: AttributeValue) {
        RUMMonitor.shared().addViewAttribute(forKey: key, value: value)
    }

    func removeViewAttribute(forKey key: AttributeKey) {
        RUMMonitor.shared().removeViewAttribute(forKey: key)
    }

    func addViewAttributes(_ attributes: [AttributeKey: AttributeValue]) {
        RUMMonitor.shared().addViewAttributes(attributes)
    }

    func removeViewAttributes(forKeys keys: [AttributeKey]) {
        RUMMonitor.shared().removeViewAttributes(forKeys: keys)
    }

    func startFeatureOperation(name: String, operationKey: String?, attributes: [AttributeKey: AttributeValue]) {
        RUMMonitor.shared().startFeatureOperation(name: name, operationKey: operationKey, attributes: attributes)
    }

    func succeedFeatureOperation(name: String, operationKey: String?, attributes: [AttributeKey: AttributeValue]) {
        RUMMonitor.shared().succeedFeatureOperation(name: name, operationKey: operationKey, attributes: attributes)
    }

    func failFeatureOperation(name: String, operationKey: String?, reason: RUMFeatureOperationFailureReason, attributes: [AttributeKey: AttributeValue]) {
        RUMMonitor.shared().failFeatureOperation(name: name, operationKey: operationKey, reason: reason, attributes: attributes)
    }
}
