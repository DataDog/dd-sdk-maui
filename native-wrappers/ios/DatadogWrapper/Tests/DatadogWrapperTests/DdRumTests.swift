import XCTest
import DatadogRUM
@testable import DatadogWrapper

final class DdRumTests: XCTestCase {
    var mockRumModule: MockRumModule!

    override func setUp() {
        super.setUp()
        mockRumModule = MockRumModule()
        DdRum.setRumModule(mockRumModule)
    }

    override func tearDown() {
        DdRum.resetDependencies()
        mockRumModule = nil
        super.tearDown()
    }

    // MARK: - enableRum basic tests

    func testEnableRum_withMinimalConfig_callsEnableWithApplicationId() {
        let config: NSDictionary = [
            "applicationId": "test-app-id"
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertTrue(mockRumModule.enableCalled)
        XCTAssertEqual(mockRumModule.capturedConfig?.applicationID, "test-app-id")
    }

    func testEnableRum_withMissingApplicationId_doesNotCallEnable() {
        let config: NSDictionary = [
            "sessionSampleRate": 80.0
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertFalse(mockRumModule.enableCalled)
    }

    func testEnableRum_withEmptyApplicationId_doesNotCallEnable() {
        let config: NSDictionary = [
            "applicationId": ""
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertFalse(mockRumModule.enableCalled)
    }

    // MARK: - Sample rates

    func testEnableRum_setsSessionSampleRate() {
        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "sessionSampleRate": 75.0
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertEqual(mockRumModule.capturedConfig?.sessionSampleRate, 75.0)
    }

    func testEnableRum_setsTelemetrySampleRate() {
        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "telemetrySampleRate": 10.0
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertEqual(mockRumModule.capturedConfig?.telemetrySampleRate, 10.0)
    }

    // MARK: - Vitals update frequency

    func testEnableRum_vitalsFrequency_never() {
        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "vitalsUpdateFrequency": "never"
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertNil(mockRumModule.capturedConfig?.vitalsUpdateFrequency)
    }

    func testEnableRum_vitalsFrequency_rare() {
        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "vitalsUpdateFrequency": "rare"
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertEqual(mockRumModule.capturedConfig?.vitalsUpdateFrequency, .rare)
    }

    func testEnableRum_vitalsFrequency_average() {
        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "vitalsUpdateFrequency": "average"
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertEqual(mockRumModule.capturedConfig?.vitalsUpdateFrequency, .average)
    }

    func testEnableRum_vitalsFrequency_frequent() {
        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "vitalsUpdateFrequency": "frequent"
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertEqual(mockRumModule.capturedConfig?.vitalsUpdateFrequency, .frequent)
    }

    // MARK: - View and interaction tracking

    func testEnableRum_nativeViewTrackingTrue_setsViewsPredicate() {
        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "nativeViewTracking": true
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertNotNil(mockRumModule.capturedConfig?.uiKitViewsPredicate)
    }

    func testEnableRum_nativeViewTrackingFalse_leavesViewsPredicateNil() {
        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "nativeViewTracking": false
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertNil(mockRumModule.capturedConfig?.uiKitViewsPredicate)
    }

    func testEnableRum_nativeInteractionTrackingTrue_setsActionsPredicate() {
        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "nativeInteractionTracking": true
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertNotNil(mockRumModule.capturedConfig?.uiKitActionsPredicate)
    }

    func testEnableRum_nativeInteractionTrackingFalse_leavesActionsPredicateNil() {
        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "nativeInteractionTracking": false
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertNil(mockRumModule.capturedConfig?.uiKitActionsPredicate)
    }

    // MARK: - Long task threshold

    func testEnableRum_nativeLongTaskThresholdMs_convertsToSeconds() {
        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "nativeLongTaskThresholdMs": 500.0
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertEqual(mockRumModule.capturedConfig?.longTaskThreshold, 0.5)
    }

    // MARK: - Tracking flags

    func testEnableRum_trackFrustrationsFalse() {
        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "trackFrustrations": false
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertEqual(mockRumModule.capturedConfig?.trackFrustrations, false)
    }

    func testEnableRum_trackBackgroundEventsTrue() {
        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "trackBackgroundEvents": true
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertEqual(mockRumModule.capturedConfig?.trackBackgroundEvents, true)
    }

    func testEnableRum_trackMemoryWarningsTrue() {
        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "trackMemoryWarnings": true
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertEqual(mockRumModule.capturedConfig?.trackMemoryWarnings, true)
    }

    // MARK: - iOS-specific thresholds

    func testEnableRum_setsAppHangThreshold() {
        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "appHangThreshold": 2.0
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertEqual(mockRumModule.capturedConfig?.appHangThreshold, 2.0)
    }

    func testEnableRum_setsTrackWatchdogTerminations() {
        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "trackWatchdogTerminations": true
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertEqual(mockRumModule.capturedConfig?.trackWatchdogTerminations, true)
    }

    // MARK: - Custom endpoint

    func testEnableRum_customEndpoint_usesUrlAsIs() {
        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "customEndpoint": "https://rum.example.com"
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertEqual(
            mockRumModule.capturedConfig?.customEndpoint?.absoluteString,
            "https://rum.example.com"
        )
    }

    func testEnableRum_customEndpointWithPath_preservesPath() {
        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "customEndpoint": "https://rum.example.com/api/v2/rum"
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertEqual(
            mockRumModule.capturedConfig?.customEndpoint?.absoluteString,
            "https://rum.example.com/api/v2/rum"
        )
    }

    func testEnableRum_emptyCustomEndpoint_doesNotSetEndpoint() {
        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "customEndpoint": ""
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertNil(mockRumModule.capturedConfig?.customEndpoint)
    }

    // MARK: - First party hosts

    func testEnableRum_firstPartyHosts_parsesJsonAndSetsUrlSessionTracking() {
        let hostsJson = """
        [{"match":"api.example.com","headerTypes":["datadog","tracecontext"]}]
        """
        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "firstPartyHosts": hostsJson,
            "resourceTraceSampleRate": 50.0
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertNotNil(mockRumModule.capturedConfig?.urlSessionTracking)
    }

    // MARK: - Initial resource threshold

    func testEnableRum_storesInitialResourceThreshold() {
        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "initialResourceThreshold": 0.5
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertEqual(DdRum.initialResourceThreshold, 0.5)
    }

    func testEnableRum_initialResourceThresholdDefaultsNil() {
        let config: NSDictionary = [
            "applicationId": "test-app-id"
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertNil(DdRum.initialResourceThreshold)
    }

    // MARK: - Native crash report enabled (stored for RUM-15107)

    func testEnableRum_storesNativeCrashReportEnabled() {
        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "nativeCrashReportEnabled": true
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertTrue(DdRum.nativeCrashReportEnabled)
    }

    func testEnableRum_nativeCrashReportDefaultsFalse() {
        let config: NSDictionary = [
            "applicationId": "test-app-id"
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertFalse(DdRum.nativeCrashReportEnabled)
    }

    // MARK: - Mapping helpers

    func testMapVitalsUpdateFrequency_allValues() {
        XCTAssertNil(DdRum.mapVitalsUpdateFrequency("never"))
        XCTAssertEqual(DdRum.mapVitalsUpdateFrequency("rare"), .rare)
        XCTAssertEqual(DdRum.mapVitalsUpdateFrequency("average"), .average)
        XCTAssertEqual(DdRum.mapVitalsUpdateFrequency("frequent"), .frequent)
        // Unknown defaults to average
        XCTAssertEqual(DdRum.mapVitalsUpdateFrequency("unknown"), .average)
    }

    func testMapTracingHeaderType_allValues() {
        XCTAssertEqual(DdRum.mapTracingHeaderType("datadog"), .datadog)
        XCTAssertEqual(DdRum.mapTracingHeaderType("b3"), .b3)
        XCTAssertEqual(DdRum.mapTracingHeaderType("b3multi"), .b3multi)
        XCTAssertEqual(DdRum.mapTracingHeaderType("tracecontext"), .tracecontext)
        // Unknown defaults to datadog
        XCTAssertEqual(DdRum.mapTracingHeaderType("unknown"), .datadog)
    }
}
