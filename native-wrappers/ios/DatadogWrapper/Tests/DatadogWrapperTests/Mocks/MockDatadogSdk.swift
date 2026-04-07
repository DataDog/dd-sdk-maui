import DatadogCore
@testable import DatadogWrapper

class MockDatadogCore: DatadogSdkProtocol {
    var setTrackingConsentCalls: [TrackingConsent] = []
    var addAttributeCalls: [(key: String, value: Any)] = []
    var addAttributesCalls: [[String: Any]] = []
    var removeAttributeCalls: [String] = []
    var removeAttributesCalls: [[String]] = []

    func setTrackingConsent(_ consent: TrackingConsent) {
        setTrackingConsentCalls.append(consent)
    }

    func addAttribute(forKey key: String, value: Any) {
        addAttributeCalls.append((key: key, value: value))
    }

    func addAttributes(_ attributes: [String: Any]) {
        addAttributesCalls.append(attributes)
    }

    func removeAttribute(forKey key: String) {
        removeAttributeCalls.append(key)
    }

    func removeAttributes(forKeys keys: [String]) {
        removeAttributesCalls.append(keys)
    }
}
