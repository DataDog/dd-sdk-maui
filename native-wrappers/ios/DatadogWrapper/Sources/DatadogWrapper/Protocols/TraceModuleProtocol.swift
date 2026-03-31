import Foundation
import DatadogTrace

/// Protocol for the Trace module functionality
/// Allows for dependency injection and testing
protocol TraceModuleProtocol {
    /// Enable the Trace module
    func enable()

    /// Enable the Trace module with custom configuration
    /// - Parameter configuration: Configuration for the Trace module
    func enable(with configuration: Trace.Configuration)
}

/// Production implementation that wraps the real Datadog Trace module
class RealTraceModule: TraceModuleProtocol {
    func enable() {
        Trace.enable()
    }

    func enable(with configuration: Trace.Configuration) {
        Trace.enable(with: configuration)
    }
}
