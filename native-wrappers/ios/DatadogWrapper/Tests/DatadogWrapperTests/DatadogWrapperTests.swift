import XCTest
import DatadogCore
import DatadogInternal
@testable import DatadogWrapper

final class DatadogWrapperTests: XCTestCase {

    // MARK: - Site mapping

    func test_mapSite_us1() {
        XCTAssertEqual(DdSdkNativeWrapper.mapSite("us1"), .us1)
    }

    func test_mapSite_us3() {
        XCTAssertEqual(DdSdkNativeWrapper.mapSite("us3"), .us3)
    }

    func test_mapSite_us5() {
        XCTAssertEqual(DdSdkNativeWrapper.mapSite("us5"), .us5)
    }

    func test_mapSite_eu1() {
        XCTAssertEqual(DdSdkNativeWrapper.mapSite("eu1"), .eu1)
    }

    func test_mapSite_ap1() {
        XCTAssertEqual(DdSdkNativeWrapper.mapSite("ap1"), .ap1)
    }

    func test_mapSite_ap2() {
        XCTAssertEqual(DdSdkNativeWrapper.mapSite("ap2"), .ap2)
    }

    func test_mapSite_us1Fed() {
        XCTAssertEqual(DdSdkNativeWrapper.mapSite("us1_fed"), .us1_fed)
    }

    func test_mapSite_unknownDefaultsToUs1() {
        XCTAssertEqual(DdSdkNativeWrapper.mapSite("invalid"), .us1)
    }

    func test_mapSite_isCaseInsensitive() {
        XCTAssertEqual(DdSdkNativeWrapper.mapSite("EU1"), .eu1)
    }

    // MARK: - Tracking consent mapping

    func test_mapTrackingConsent_granted() {
        XCTAssertEqual(DdSdkNativeWrapper.mapTrackingConsent("granted"), .granted)
    }

    func test_mapTrackingConsent_notGranted() {
        XCTAssertEqual(DdSdkNativeWrapper.mapTrackingConsent("not_granted"), .notGranted)
    }

    func test_mapTrackingConsent_pending() {
        XCTAssertEqual(DdSdkNativeWrapper.mapTrackingConsent("pending"), .pending)
    }

    func test_mapTrackingConsent_unknownDefaultsToPending() {
        XCTAssertEqual(DdSdkNativeWrapper.mapTrackingConsent("invalid"), .pending)
    }

    // MARK: - Verbosity mapping

    func test_mapVerbosity_debug() {
        XCTAssertEqual(DdSdkNativeWrapper.mapVerbosity("debug"), .debug)
    }

    func test_mapVerbosity_infoMapsToDebug() {
        XCTAssertEqual(DdSdkNativeWrapper.mapVerbosity("info"), .debug)
    }

    func test_mapVerbosity_warn() {
        XCTAssertEqual(DdSdkNativeWrapper.mapVerbosity("warn"), .warn)
    }

    func test_mapVerbosity_error() {
        XCTAssertEqual(DdSdkNativeWrapper.mapVerbosity("error"), .error)
    }

    func test_mapVerbosity_unknownDefaultsToError() {
        XCTAssertEqual(DdSdkNativeWrapper.mapVerbosity("invalid"), .error)
    }

    // MARK: - Batch size mapping

    func test_mapBatchSize_small() {
        XCTAssertEqual(DdSdkNativeWrapper.mapBatchSize("small"), .small)
    }

    func test_mapBatchSize_medium() {
        XCTAssertEqual(DdSdkNativeWrapper.mapBatchSize("medium"), .medium)
    }

    func test_mapBatchSize_large() {
        XCTAssertEqual(DdSdkNativeWrapper.mapBatchSize("large"), .large)
    }

    func test_mapBatchSize_unknownDefaultsToMedium() {
        XCTAssertEqual(DdSdkNativeWrapper.mapBatchSize("invalid"), .medium)
    }

    // MARK: - Upload frequency mapping

    func test_mapUploadFrequency_frequent() {
        XCTAssertEqual(DdSdkNativeWrapper.mapUploadFrequency("frequent"), .frequent)
    }

    func test_mapUploadFrequency_average() {
        XCTAssertEqual(DdSdkNativeWrapper.mapUploadFrequency("average"), .average)
    }

    func test_mapUploadFrequency_rare() {
        XCTAssertEqual(DdSdkNativeWrapper.mapUploadFrequency("rare"), .rare)
    }

    func test_mapUploadFrequency_unknownDefaultsToAverage() {
        XCTAssertEqual(DdSdkNativeWrapper.mapUploadFrequency("invalid"), .average)
    }

    // MARK: - Batch processing level mapping

    func test_mapBatchProcessingLevel_low() {
        XCTAssertEqual(DdSdkNativeWrapper.mapBatchProcessingLevel("low"), .low)
    }

    func test_mapBatchProcessingLevel_medium() {
        XCTAssertEqual(DdSdkNativeWrapper.mapBatchProcessingLevel("medium"), .medium)
    }

    func test_mapBatchProcessingLevel_high() {
        XCTAssertEqual(DdSdkNativeWrapper.mapBatchProcessingLevel("high"), .high)
    }

    func test_mapBatchProcessingLevel_unknownDefaultsToMedium() {
        XCTAssertEqual(DdSdkNativeWrapper.mapBatchProcessingLevel("invalid"), .medium)
    }

    // MARK: - SDK initialization

    override func tearDown() {
        Datadog.stopInstance()
        Datadog.verbosityLevel = nil
        super.tearDown()
    }

    func test_initialize_initializesTheSdk() {
        let result = DdSdkNativeWrapper.initialize(
            clientToken: "pub-test-token",
            environment: "test",
            service: nil,
            site: "us1",
            verbosity: "error",
            trackingConsent: "pending",
            batchSize: nil,
            uploadFrequency: nil,
            batchProcessingLevel: nil,
            additionalConfiguration: nil
        )

        XCTAssertTrue(result)
        XCTAssertTrue(Datadog.isInitialized())
    }

    func test_initialize_setsVerbosityLevel() {
        _ = DdSdkNativeWrapper.initialize(
            clientToken: "pub-test-token",
            environment: "test",
            service: nil,
            site: "us1",
            verbosity: "warn",
            trackingConsent: "pending",
            batchSize: nil,
            uploadFrequency: nil,
            batchProcessingLevel: nil,
            additionalConfiguration: nil
        )

        XCTAssertEqual(Datadog.verbosityLevel, .warn)
    }

    func test_initialize_withDebugVerbosity_setsDebugLevel() {
        _ = DdSdkNativeWrapper.initialize(
            clientToken: "pub-test-token",
            environment: "test",
            service: nil,
            site: "us1",
            verbosity: "debug",
            trackingConsent: "pending",
            batchSize: nil,
            uploadFrequency: nil,
            batchProcessingLevel: nil,
            additionalConfiguration: nil
        )

        XCTAssertEqual(Datadog.verbosityLevel, .debug)
    }

    // MARK: - Set tracking consent

    func test_setTrackingConsent_granted_callsDatadogWithGranted() {
        let mockCore = MockDatadogCore()
        DdSdkNativeWrapper.setDatadogCore(mockCore)
        defer { DdSdkNativeWrapper.resetDependencies() }

        DdSdkNativeWrapper.setTrackingConsent("granted")

        XCTAssertEqual(mockCore.setTrackingConsentCalls.count, 1)
        XCTAssertEqual(mockCore.setTrackingConsentCalls[0], .granted)
    }

    func test_setTrackingConsent_notGranted_callsDatadogWithNotGranted() {
        let mockCore = MockDatadogCore()
        DdSdkNativeWrapper.setDatadogCore(mockCore)
        defer { DdSdkNativeWrapper.resetDependencies() }

        DdSdkNativeWrapper.setTrackingConsent("not_granted")

        XCTAssertEqual(mockCore.setTrackingConsentCalls.count, 1)
        XCTAssertEqual(mockCore.setTrackingConsentCalls[0], .notGranted)
    }

    func test_setTrackingConsent_pending_callsDatadogWithPending() {
        let mockCore = MockDatadogCore()
        DdSdkNativeWrapper.setDatadogCore(mockCore)
        defer { DdSdkNativeWrapper.resetDependencies() }

        DdSdkNativeWrapper.setTrackingConsent("pending")

        XCTAssertEqual(mockCore.setTrackingConsentCalls.count, 1)
        XCTAssertEqual(mockCore.setTrackingConsentCalls[0], .pending)
    }

    // MARK: - Global attributes

    func test_addAttribute_callsDatadogCoreAddAttribute() {
        let mockCore = MockDatadogCore()
        DdSdkNativeWrapper.setDatadogCore(mockCore)
        defer { DdSdkNativeWrapper.resetDependencies() }

        DdSdkNativeWrapper.addAttribute("plan", value: "premium")

        XCTAssertEqual(mockCore.addAttributeCalls.count, 1)
        XCTAssertEqual(mockCore.addAttributeCalls[0].key, "plan")
        XCTAssertEqual(mockCore.addAttributeCalls[0].value as? String, "premium")
    }

    func test_addAttributes_callsDatadogCoreAddAttributes() {
        let mockCore = MockDatadogCore()
        DdSdkNativeWrapper.setDatadogCore(mockCore)
        defer { DdSdkNativeWrapper.resetDependencies() }

        let attrs: NSDictionary = ["plan": "premium", "version": "2.0"]
        DdSdkNativeWrapper.addAttributes(attrs)

        XCTAssertEqual(mockCore.addAttributesCalls.count, 1)
        XCTAssertEqual(mockCore.addAttributesCalls[0]["plan"] as? String, "premium")
        XCTAssertEqual(mockCore.addAttributesCalls[0]["version"] as? String, "2.0")
    }

    func test_removeAttribute_callsDatadogCoreRemoveAttribute() {
        let mockCore = MockDatadogCore()
        DdSdkNativeWrapper.setDatadogCore(mockCore)
        defer { DdSdkNativeWrapper.resetDependencies() }

        DdSdkNativeWrapper.removeAttribute("plan")

        XCTAssertEqual(mockCore.removeAttributeCalls.count, 1)
        XCTAssertEqual(mockCore.removeAttributeCalls[0], "plan")
    }

    func test_removeAttributes_callsDatadogCoreRemoveAttributes() {
        let mockCore = MockDatadogCore()
        DdSdkNativeWrapper.setDatadogCore(mockCore)
        defer { DdSdkNativeWrapper.resetDependencies() }

        let keys: NSArray = ["plan", "version"]
        DdSdkNativeWrapper.removeAttributes(keys)

        XCTAssertEqual(mockCore.removeAttributesCalls.count, 1)
        XCTAssertEqual(mockCore.removeAttributesCalls[0], ["plan", "version"])
    }

    // MARK: - User Info

    func test_setUserInfo_callsDatadogCoreSetUserInfo() {
        let mockCore = MockDatadogCore()
        DdSdkNativeWrapper.setDatadogCore(mockCore)
        defer { DdSdkNativeWrapper.resetDependencies() }

        let extraInfo: NSDictionary = ["plan": "premium"]
        DdSdkNativeWrapper.setUserInfo("user-123", name: "John", email: "john@example.com", extraInfo: extraInfo)

        XCTAssertEqual(mockCore.setUserInfoCalls.count, 1)
        XCTAssertEqual(mockCore.setUserInfoCalls[0].id, "user-123")
        XCTAssertEqual(mockCore.setUserInfoCalls[0].name, "John")
        XCTAssertEqual(mockCore.setUserInfoCalls[0].email, "john@example.com")
        XCTAssertEqual(mockCore.setUserInfoCalls[0].extraInfo["plan"] as? String, "premium")
    }

    func test_addUserExtraInfo_callsDatadogCoreAddUserExtraInfo() {
        let mockCore = MockDatadogCore()
        DdSdkNativeWrapper.setDatadogCore(mockCore)
        defer { DdSdkNativeWrapper.resetDependencies() }

        let extraInfo: NSDictionary = ["plan": "premium"]
        DdSdkNativeWrapper.addUserExtraInfo(extraInfo)

        XCTAssertEqual(mockCore.addUserExtraInfoCalls.count, 1)
        XCTAssertEqual(mockCore.addUserExtraInfoCalls[0]["plan"] as? String, "premium")
    }

    func test_clearUserInfo_callsDatadogCoreClearUserInfo() {
        let mockCore = MockDatadogCore()
        DdSdkNativeWrapper.setDatadogCore(mockCore)
        defer { DdSdkNativeWrapper.resetDependencies() }

        DdSdkNativeWrapper.clearUserInfo()

        XCTAssertEqual(mockCore.clearUserInfoCallCount, 1)
    }

    // MARK: - Account Info

    func test_setAccountInfo_callsDatadogCoreSetAccountInfo() {
        let mockCore = MockDatadogCore()
        DdSdkNativeWrapper.setDatadogCore(mockCore)
        defer { DdSdkNativeWrapper.resetDependencies() }

        let extraInfo: NSDictionary = ["tier": "enterprise"]
        DdSdkNativeWrapper.setAccountInfo("acct-456", name: "Acme Corp", extraInfo: extraInfo)

        XCTAssertEqual(mockCore.setAccountInfoCalls.count, 1)
        XCTAssertEqual(mockCore.setAccountInfoCalls[0].id, "acct-456")
        XCTAssertEqual(mockCore.setAccountInfoCalls[0].name, "Acme Corp")
        XCTAssertEqual(mockCore.setAccountInfoCalls[0].extraInfo["tier"] as? String, "enterprise")
    }

    func test_addAccountExtraInfo_callsDatadogCoreAddAccountExtraInfo() {
        let mockCore = MockDatadogCore()
        DdSdkNativeWrapper.setDatadogCore(mockCore)
        defer { DdSdkNativeWrapper.resetDependencies() }

        let extraInfo: NSDictionary = ["tier": "enterprise"]
        DdSdkNativeWrapper.addAccountExtraInfo(extraInfo)

        XCTAssertEqual(mockCore.addAccountExtraInfoCalls.count, 1)
        XCTAssertEqual(mockCore.addAccountExtraInfoCalls[0]["tier"] as? String, "enterprise")
    }

    func test_clearAccountInfo_callsDatadogCoreClearAccountInfo() {
        let mockCore = MockDatadogCore()
        DdSdkNativeWrapper.setDatadogCore(mockCore)
        defer { DdSdkNativeWrapper.resetDependencies() }

        DdSdkNativeWrapper.clearAccountInfo()

        XCTAssertEqual(mockCore.clearAccountInfoCallCount, 1)
    }
}
