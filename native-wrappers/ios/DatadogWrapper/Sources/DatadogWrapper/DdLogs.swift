import Foundation
import DatadogCore
import DatadogLogs

@objc(DdLogs)
public class DdLogs: NSObject {

    private static var logger: LoggerProtocol?

    /// Enable the Logs module with default configuration
    /// Must be called after DatadogWrapper.initialize()
    @objc public static func enableLogs() {
        // Enable Logs feature (dd-sdk-ios v3.x API)
        Logs.enable()

        // Create logger with service name from global config
        let serviceName = DdSdkNativeWrapper.getServiceName()
        logger = Logger.create(
            with: Logger.Configuration(
                service: serviceName
            )
        )
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
        attributes: [String: String]
    ) {
        ensureLogger()

        // Convert String dictionary to [String: Encodable]
        let encodableAttributes: [String: Encodable] = attributes.mapValues { $0 as Encodable }

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

    /// Ensure logger is initialized (lazy initialization)
    private static func ensureLogger() {
        if logger == nil {
            enableLogs()
        }
    }
}
