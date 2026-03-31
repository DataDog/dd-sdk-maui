import DatadogCore
@testable import DatadogWrapper

class MockDatadogCore: DatadogSdkProtocol {
    var setTrackingConsentCalls: [TrackingConsent] = []

    func setTrackingConsent(_ consent: TrackingConsent) {
        setTrackingConsentCalls.append(consent)
    }
}
