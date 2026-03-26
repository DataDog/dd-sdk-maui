import Foundation
import DatadogCore

/// Protocol for Datadog core SDK operations
/// Allows for dependency injection and testing
protocol DatadogSdkProtocol {
    /// Set the tracking consent for data collection
    /// - Parameter consent: The new tracking consent value
    func setTrackingConsent(_ consent: TrackingConsent)
}

/// Production implementation that wraps the real Datadog SDK
class RealDatadogSdk: DatadogSdkProtocol {
    func setTrackingConsent(_ consent: TrackingConsent) {
        Datadog.set(trackingConsent: consent)
    }
}
