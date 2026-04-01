import Foundation
import DatadogInternal
import DatadogRUM

/// Protocol for the RUM module functionality
/// Allows for dependency injection and testing
protocol RumModuleProtocol {
    /// Enable the RUM module with configuration
    /// - Parameter configuration: Configuration for the RUM module
    func enable(with configuration: RUM.Configuration)

    /// Add a RUM error with message, source, stacktrace, and attributes
    func addError(message: String, source: RUMErrorSource, stacktrace: String?, attributes: [AttributeKey: AttributeValue])
}

/// Production implementation that wraps the real Datadog RUM module
class RealRumModule: RumModuleProtocol {
    func enable(with configuration: RUM.Configuration) {
        RUM.enable(with: configuration)
    }

    func addError(message: String, source: RUMErrorSource, stacktrace: String?, attributes: [AttributeKey: AttributeValue]) {
        RUMMonitor.shared().addError(message: message, type: nil, stack: stacktrace, source: source, attributes: attributes)
    }
}
