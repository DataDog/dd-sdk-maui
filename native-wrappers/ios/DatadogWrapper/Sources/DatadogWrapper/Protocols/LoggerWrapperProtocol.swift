import Foundation
import DatadogLogs

/// Protocol for Logger functionality
/// Allows for dependency injection and testing
protocol LoggerWrapperProtocol {
    func debug(_ message: String)
    func info(_ message: String)
    func warn(_ message: String)
    func error(_ message: String)
    func critical(_ message: String)
    func debug(_ message: String, attributes: [String: Encodable])
    func info(_ message: String, attributes: [String: Encodable])
    func warn(_ message: String, attributes: [String: Encodable])
    func error(_ message: String, attributes: [String: Encodable])
    func critical(_ message: String, attributes: [String: Encodable])
}

/// Production implementation that wraps a real DatadogLogs.LoggerProtocol
class RealLoggerWrapper: LoggerWrapperProtocol {
    private let logger: LoggerProtocol

    init(logger: LoggerProtocol) {
        self.logger = logger
    }

    func debug(_ message: String) {
        logger.debug(message)
    }

    func info(_ message: String) {
        logger.info(message)
    }

    func warn(_ message: String) {
        logger.warn(message)
    }

    func error(_ message: String) {
        logger.error(message)
    }

    func critical(_ message: String) {
        logger.critical(message)
    }

    func debug(_ message: String, attributes: [String: Encodable]) {
        logger.debug(message, attributes: attributes)
    }

    func info(_ message: String, attributes: [String: Encodable]) {
        logger.info(message, attributes: attributes)
    }

    func warn(_ message: String, attributes: [String: Encodable]) {
        logger.warn(message, attributes: attributes)
    }

    func error(_ message: String, attributes: [String: Encodable]) {
        logger.error(message, attributes: attributes)
    }

    func critical(_ message: String, attributes: [String: Encodable]) {
        logger.critical(message, attributes: attributes)
    }
}
