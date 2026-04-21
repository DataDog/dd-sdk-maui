import Foundation
import DatadogCore
import DatadogInternal
import DatadogRUM

/// Protocol for Datadog core SDK operations
/// Allows for dependency injection and testing
protocol DatadogSdkProtocol {
    /// Set the tracking consent for data collection
    /// - Parameter consent: The new tracking consent value
    func setTrackingConsent(_ consent: TrackingConsent)

    /// Add a global attribute on the RUM monitor
    func addAttribute(forKey key: String, value: Any)

    /// Add multiple global attributes on the RUM monitor
    func addAttributes(_ attributes: [String: Any])

    /// Remove a global attribute from the RUM monitor
    func removeAttribute(forKey key: String)

    /// Remove multiple global attributes from the RUM monitor
    func removeAttributes(forKeys keys: [String])
}

/// Production implementation that wraps the real Datadog SDK
class RealDatadogSdk: DatadogSdkProtocol {
    func setTrackingConsent(_ consent: TrackingConsent) {
        Datadog.set(trackingConsent: consent)
    }

    func addAttribute(forKey key: String, value: Any) {
        RUMMonitor.shared().addAttribute(forKey: key, value: AnyEncodable(value))
    }

    func addAttributes(_ attributes: [String: Any]) {
        let monitor = RUMMonitor.shared()
        for (key, value) in attributes {
            monitor.addAttribute(forKey: key, value: AnyEncodable(value))
        }
    }

    func removeAttribute(forKey key: String) {
        RUMMonitor.shared().removeAttribute(forKey: key)
    }

    func removeAttributes(forKeys keys: [String]) {
        let monitor = RUMMonitor.shared()
        for key in keys {
            monitor.removeAttribute(forKey: key)
        }
    }
}
