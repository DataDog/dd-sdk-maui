/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

import Foundation
import DatadogCore
import DatadogLogs

@objc(DdLogs)
public class DdLogs: NSObject {

    // Dependencies - injectable for testing
    private static var logsModule: LogsModuleProtocol = RealLogsModule()
    private static var logger: LoggerWrapperProtocol?

    // For testing: inject dependencies
    static func setLogsModule(_ module: LogsModuleProtocol) {
        logsModule = module
    }

    static func setLogger(_ loggerWrapper: LoggerWrapperProtocol?) {
        logger = loggerWrapper
    }

    // Reset to production dependencies (for test cleanup)
    static func resetDependencies() {
        logsModule = RealLogsModule()
        logger = nil
    }

    /// Enable the Logs module with optional configuration
    /// - Parameter customEndpoint: Optional custom server URL for sending logs
    /// Must be called after DatadogWrapper.initialize()
    @objc(enableLogs:)
    public static func enableLogs(customEndpoint: String?) {
        if let customEndpoint = customEndpoint,
           !customEndpoint.isEmpty,
           let url = URL(string: customEndpoint) {
            // Enable Logs with custom configuration
            var config = Logs.Configuration()
            config.customEndpoint = url
            logsModule.enable(with: config)
        } else {
            // Enable Logs with default configuration
            logsModule.enable()
        }

        logger = RealLoggerWrapper(logger: Logger.create(with: Logger.Configuration()))
    }

    /// Log a debug message
    /// - Parameter message: The message to log
    @objc public static func logDebug(_ message: String) {
        ensureLogger()
        logger?.debug(message)
    }

    /// Log an info message
    /// - Parameter message: The message to log
    @objc public static func logInfo(_ message: String) {
        ensureLogger()
        logger?.info(message)
    }

    /// Log a warning message
    /// - Parameter message: The message to log
    @objc public static func logWarn(_ message: String) {
        ensureLogger()
        logger?.warn(message)
    }

    /// Log an error message
    /// - Parameter message: The message to log
    @objc public static func logError(_ message: String) {
        ensureLogger()
        logger?.error(message)
    }

    /// Log a critical message
    /// - Parameter message: The message to log
    @objc public static func logCritical(_ message: String) {
        ensureLogger()
        logger?.critical(message)
    }

    /// Log a message with custom attributes
    /// - Parameters:
    ///   - level: Log level as string ("debug", "info", "warn", "error", "critical")
    ///   - message: The message to log
    ///   - attributes: Dictionary of custom attributes to attach
    @objc public static func logWithAttributes(
        level: String,
        message: String,
        attributes: NSDictionary
    ) {
        ensureLogger()

        // Convert NSDictionary to [String: Encodable]
        var encodableAttributes: [String: Encodable] = [:]
        if let dict = attributes as? [String: Any] {
            for (key, value) in dict {
                switch value {
                case let boolVal as Bool:
                    encodableAttributes[key] = boolVal
                case let intVal as Int:
                    encodableAttributes[key] = intVal
                case let doubleVal as Double:
                    encodableAttributes[key] = doubleVal
                case let stringVal as String:
                    encodableAttributes[key] = stringVal
                case let int64Val as Int64:
                    encodableAttributes[key] = int64Val
                default:
                    encodableAttributes[key] = String(describing: value)
                }
            }
        }

        switch level.lowercased() {
        case "debug":
            logger?.debug(message, attributes: encodableAttributes)
        case "info":
            logger?.info(message, attributes: encodableAttributes)
        case "warn", "warning":
            logger?.warn(message, attributes: encodableAttributes)
        case "error":
            logger?.error(message, attributes: encodableAttributes)
        case "critical":
            logger?.critical(message, attributes: encodableAttributes)
        default:
            logger?.info(message, attributes: encodableAttributes)
        }
    }

    /// Guard that a logger exists before forwarding a log call.
    /// If the logger has not been initialised (i.e. enableLogs was never called),
    /// the call is silently dropped and a warning is printed to the console.
    private static func ensureLogger() {
        if logger == nil {
            print("[Datadog] DdLogs.enable() must be called before logging. Log will be dropped.")
        }
    }
}
