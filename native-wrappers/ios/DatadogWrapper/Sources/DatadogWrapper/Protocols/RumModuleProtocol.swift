import Foundation
import DatadogRUM

/// Protocol for the RUM module functionality
/// Allows for dependency injection and testing
protocol RumModuleProtocol {
    /// Enable the RUM module with configuration
    /// - Parameter configuration: Configuration for the RUM module
    func enable(with configuration: RUM.Configuration)
}

/// Production implementation that wraps the real Datadog RUM module
class RealRumModule: RumModuleProtocol {
    func enable(with configuration: RUM.Configuration) {
        RUM.enable(with: configuration)
    }
}
