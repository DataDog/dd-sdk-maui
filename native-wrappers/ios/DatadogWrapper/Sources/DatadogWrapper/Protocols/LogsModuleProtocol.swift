import Foundation
import DatadogLogs

/// Protocol for the Logs module functionality
/// Allows for dependency injection and testing
protocol LogsModuleProtocol {
    /// Enable the Logs module
    func enable()

    /// Enable the Logs module with custom configuration
    /// - Parameter configuration: Configuration for the Logs module
    func enable(with configuration: Logs.Configuration)
}

/// Production implementation that wraps the real Datadog Logs module
class RealLogsModule: LogsModuleProtocol {
    func enable() {
        Logs.enable()
    }

    func enable(with configuration: Logs.Configuration) {
        Logs.enable(with: configuration)
    }
}
