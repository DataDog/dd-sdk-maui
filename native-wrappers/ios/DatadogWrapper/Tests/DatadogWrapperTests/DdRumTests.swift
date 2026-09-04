/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

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
        DdSdkNativeWrapper.firstPartyHosts = nil
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

    // MARK: - First party hosts (stored during SDK init)

    func testEnableRum_withStoredFirstPartyHosts_setsUrlSessionTracking() {
        // Simulate hosts stored during DdSdk.Initialize()
        DdSdkNativeWrapper.firstPartyHosts = [
            "api.example.com": Set([.datadog, .tracecontext])
        ]

        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "resourceTraceSampleRate": 50.0
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertNotNil(mockRumModule.capturedConfig?.urlSessionTracking)
    }

    func testEnableRum_withoutStoredFirstPartyHosts_setsUrlSessionTrackingForResourceDrop() {
        // When C# auto-resource-tracking is enabled (default), urlSessionTracking is always
        // configured so the resourceAttributesProvider can drop native URLSession resources
        // already tracked by the C# DiagnosticListener layer — even if no first-party hosts
        // are configured (and thus no distributed tracing headers will be injected).
        DdSdkNativeWrapper.firstPartyHosts = nil

        let config: NSDictionary = [
            "applicationId": "test-app-id"
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertNotNil(mockRumModule.capturedConfig?.urlSessionTracking)
    }

    func testEnableRum_firstPartyHosts_withAutoResourceTrackingOff_stillSetsUrlSessionTracking() {
        DdSdkNativeWrapper.firstPartyHosts = [
            "api.example.com": Set([.datadog])
        ]

        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "automaticResourceTracking": false
        ]

        DdRum.enableRum(configuration: config)

        // urlSessionTracking should still be set for distributed tracing header injection
        XCTAssertNotNil(mockRumModule.capturedConfig?.urlSessionTracking)
    }

    func testEnableRum_noFirstPartyHosts_withAutoResourceTrackingOff_doesNotSetUrlSessionTracking() {
        DdSdkNativeWrapper.firstPartyHosts = nil

        let config: NSDictionary = [
            "applicationId": "test-app-id",
            "automaticResourceTracking": false
        ]

        DdRum.enableRum(configuration: config)

        XCTAssertNil(mockRumModule.capturedConfig?.urlSessionTracking)
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

    // MARK: - addError

    func testAddError_callsRumModuleWithMessage() {
        let config: NSDictionary = [
            "applicationId": "test-app-id"
        ]
        DdRum.enableRum(configuration: config)

        DdRum.addError(
            message: "Test error",
            source: "source",
            stacktrace: "at Foo.Bar()",
            context: [:],
            timestampMs: 0
        )

        XCTAssertTrue(mockRumModule.addErrorCalled)
        XCTAssertEqual(mockRumModule.capturedErrorMessage, "Test error")
    }

    func testAddError_mapsSourceCorrectly() {
        let config: NSDictionary = [
            "applicationId": "test-app-id"
        ]
        DdRum.enableRum(configuration: config)

        DdRum.addError(
            message: "Error",
            source: "network",
            stacktrace: "stack",
            context: [:],
            timestampMs: 0
        )

        XCTAssertEqual(mockRumModule.capturedErrorSource, .network)
    }

    func testAddError_passesStacktrace() {
        let config: NSDictionary = [
            "applicationId": "test-app-id"
        ]
        DdRum.enableRum(configuration: config)

        DdRum.addError(
            message: "Error",
            source: "source",
            stacktrace: "System.Exception: test\n   at Foo.Bar()",
            context: [:],
            timestampMs: 0
        )

        XCTAssertEqual(mockRumModule.capturedErrorStacktrace, "System.Exception: test\n   at Foo.Bar()")
    }

    func testAddError_mergesContextAndTimestamp() {
        let config: NSDictionary = [
            "applicationId": "test-app-id"
        ]
        DdRum.enableRum(configuration: config)

        let context: NSDictionary = [
            "_dd.error.is_crash": true
        ]

        DdRum.addError(
            message: "Error",
            source: "source",
            stacktrace: "stack",
            context: context,
            timestampMs: 1234567890
        )

        XCTAssertNotNil(mockRumModule.capturedErrorAttributes)
        let attrs = mockRumModule.capturedErrorAttributes!
        XCTAssertEqual(attrs["_dd.error.is_crash"] as? Bool, true)
        XCTAssertEqual(attrs["_dd.timestamp"] as? Int64, 1234567890)
        XCTAssertEqual(attrs["_dd.error.source_type"] as? String, "maui")
    }

    func testAddError_withZeroTimestamp_doesNotAddTimestampAttribute() {
        let config: NSDictionary = [
            "applicationId": "test-app-id"
        ]
        DdRum.enableRum(configuration: config)

        DdRum.addError(
            message: "Error",
            source: "source",
            stacktrace: "stack",
            context: [:],
            timestampMs: 0
        )

        let attrs = mockRumModule.capturedErrorAttributes!
        XCTAssertNil(attrs["_dd.timestamp"] as? Int64)
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

    func testMapErrorSource_allValues() {
        XCTAssertEqual(DdRum.mapErrorSource("source"), .source)
        XCTAssertEqual(DdRum.mapErrorSource("network"), .network)
        XCTAssertEqual(DdRum.mapErrorSource("console"), .console)
        XCTAssertEqual(DdRum.mapErrorSource("webview"), .webview)
        XCTAssertEqual(DdRum.mapErrorSource("custom"), .custom)
        // Unknown defaults to custom
        XCTAssertEqual(DdRum.mapErrorSource("unknown"), .custom)
    }

    func testMapTracingHeaderType_allValues() {
        XCTAssertEqual(DdRum.mapTracingHeaderType("datadog"), .datadog)
        XCTAssertEqual(DdRum.mapTracingHeaderType("b3"), .b3)
        XCTAssertEqual(DdRum.mapTracingHeaderType("b3multi"), .b3multi)
        XCTAssertEqual(DdRum.mapTracingHeaderType("tracecontext"), .tracecontext)
        // Unknown defaults to datadog
        XCTAssertEqual(DdRum.mapTracingHeaderType("unknown"), .datadog)
    }

    func testMapActionType_allValues() {
        XCTAssertEqual(DdRum.mapActionType("tap"), .tap)
        XCTAssertEqual(DdRum.mapActionType("click"), .tap)  // iOS has no .click, maps to .tap
        XCTAssertEqual(DdRum.mapActionType("scroll"), .scroll)
        XCTAssertEqual(DdRum.mapActionType("swipe"), .swipe)
        XCTAssertEqual(DdRum.mapActionType("custom"), .custom)
        // Unknown defaults to custom
        XCTAssertEqual(DdRum.mapActionType("unknown"), .custom)
    }

    func testMapResourceMethod_allValues() {
        XCTAssertEqual(DdRum.mapResourceMethod("get"), .get)
        XCTAssertEqual(DdRum.mapResourceMethod("post"), .post)
        XCTAssertEqual(DdRum.mapResourceMethod("put"), .put)
        XCTAssertEqual(DdRum.mapResourceMethod("delete"), .delete)
        XCTAssertEqual(DdRum.mapResourceMethod("head"), .head)
        XCTAssertEqual(DdRum.mapResourceMethod("patch"), .patch)
        // Unknown defaults to get
        XCTAssertEqual(DdRum.mapResourceMethod("unknown"), .get)
    }

    func testMapResourceKind_allValues() {
        XCTAssertEqual(DdRum.mapResourceKind("xhr"), .xhr)
        XCTAssertEqual(DdRum.mapResourceKind("native"), .native)
        XCTAssertEqual(DdRum.mapResourceKind("fetch"), .fetch)
        XCTAssertEqual(DdRum.mapResourceKind("document"), .document)
        XCTAssertEqual(DdRum.mapResourceKind("beacon"), .beacon)
        XCTAssertEqual(DdRum.mapResourceKind("image"), .image)
        XCTAssertEqual(DdRum.mapResourceKind("font"), .font)
        XCTAssertEqual(DdRum.mapResourceKind("css"), .css)
        XCTAssertEqual(DdRum.mapResourceKind("media"), .media)
        XCTAssertEqual(DdRum.mapResourceKind("js"), .js)
        // Unknown defaults to other
        XCTAssertEqual(DdRum.mapResourceKind("unknown"), .other)
    }

    // MARK: - startView

    func testStartView_callsRumModuleWithKeyAndName() {
        DdRum.startView("view-key", name: "ViewName", context: [:], timestampMs: 0)

        XCTAssertTrue(mockRumModule.startViewCalled)
        XCTAssertEqual(mockRumModule.capturedStartViewKey, "view-key")
        XCTAssertEqual(mockRumModule.capturedStartViewName, "ViewName")
    }

    func testStartView_mergesContextAndTimestamp() {
        let context: NSDictionary = ["custom_key": "custom_value"]
        DdRum.startView("view-key", name: "ViewName", context: context, timestampMs: 1234567890)

        let attrs = mockRumModule.capturedStartViewAttributes!
        XCTAssertEqual(attrs["_dd.timestamp"] as? Int64, 1234567890)
    }

    // MARK: - stopView

    func testStopView_callsRumModuleWithKey() {
        DdRum.stopView("view-key", context: [:], timestampMs: 0)

        XCTAssertTrue(mockRumModule.stopViewCalled)
        XCTAssertEqual(mockRumModule.capturedStopViewKey, "view-key")
    }

    // MARK: - startAction

    func testStartAction_callsRumModuleWithTypeAndName() {
        DdRum.startAction("tap", name: "Button Tap", context: [:], timestampMs: 0)

        XCTAssertTrue(mockRumModule.startActionCalled)
        XCTAssertEqual(mockRumModule.capturedStartActionType, .tap)
        XCTAssertEqual(mockRumModule.capturedStartActionName, "Button Tap")
    }

    // MARK: - stopAction

    func testStopAction_callsRumModuleWithTypeAndName() {
        DdRum.stopAction("scroll", name: "List Scroll", context: [:], timestampMs: 0)

        XCTAssertTrue(mockRumModule.stopActionCalled)
        XCTAssertEqual(mockRumModule.capturedStopActionType, .scroll)
        XCTAssertEqual(mockRumModule.capturedStopActionName, "List Scroll")
    }

    // MARK: - addAction

    func testAddAction_callsRumModuleWithTypeAndName() {
        DdRum.addAction("swipe", name: "Swipe Left", context: [:], timestampMs: 0)

        XCTAssertTrue(mockRumModule.addActionCalled)
        XCTAssertEqual(mockRumModule.capturedAddActionType, .swipe)
        XCTAssertEqual(mockRumModule.capturedAddActionName, "Swipe Left")
    }

    // MARK: - startResource

    func testStartResource_callsRumModuleWithParams() {
        DdRum.startResource("res-key", method: "post", url: "https://api.example.com/data", context: [:], timestampMs: 0)

        XCTAssertTrue(mockRumModule.startResourceCalled)
        XCTAssertEqual(mockRumModule.capturedStartResourceKey, "res-key")
        XCTAssertEqual(mockRumModule.capturedStartResourceMethod, .post)
        XCTAssertEqual(mockRumModule.capturedStartResourceUrl, "https://api.example.com/data")
    }

    // MARK: - stopResource

    func testStopResource_callsRumModuleWithParams() {
        DdRum.stopResource("res-key", statusCode: 200, kind: "xhr", size: 1024, context: [:], timestampMs: 0)

        XCTAssertTrue(mockRumModule.stopResourceCalled)
        XCTAssertEqual(mockRumModule.capturedStopResourceKey, "res-key")
        XCTAssertEqual(mockRumModule.capturedStopResourceStatusCode, 200)
        XCTAssertEqual(mockRumModule.capturedStopResourceKind, .xhr)
        XCTAssertEqual(mockRumModule.capturedStopResourceSize, 1024)
    }

    func testStopResource_withNegativeSize_passesNil() {
        DdRum.stopResource("res-key", statusCode: 200, kind: "fetch", size: -1, context: [:], timestampMs: 0)

        XCTAssertNil(mockRumModule.capturedStopResourceSize)
    }

    // MARK: - addTiming

    func testAddTiming_callsRumModuleWithName() {
        DdRum.addTiming("hero_ready")

        XCTAssertTrue(mockRumModule.addTimingCalled)
        XCTAssertEqual(mockRumModule.capturedTimingName, "hero_ready")
    }

    // MARK: - addViewLoadingTime

    func testAddViewLoadingTime_callsRumModuleWithOverwrite() {
        DdRum.addViewLoadingTime(true)

        XCTAssertTrue(mockRumModule.addViewLoadingTimeCalled)
        XCTAssertEqual(mockRumModule.capturedViewLoadingTimeOverwrite, true)
    }

    func testAddViewLoadingTime_withFalse_callsRumModule() {
        DdRum.addViewLoadingTime(false)

        XCTAssertTrue(mockRumModule.addViewLoadingTimeCalled)
        XCTAssertEqual(mockRumModule.capturedViewLoadingTimeOverwrite, false)
    }

    // MARK: - stopSession

    func testStopSession_callsRumModule() {
        DdRum.stopSession()

        XCTAssertTrue(mockRumModule.stopSessionCalled)
    }

    // MARK: - addViewAttribute

    func testAddViewAttribute_callsRumModuleWithKeyAndValue() {
        DdRum.addViewAttribute("theme", value: "dark")

        XCTAssertTrue(mockRumModule.addViewAttributeCalled)
        XCTAssertEqual(mockRumModule.capturedAddViewAttributeKey, "theme")
    }

    // MARK: - removeViewAttribute

    func testRemoveViewAttribute_callsRumModuleWithKey() {
        DdRum.removeViewAttribute("theme")

        XCTAssertTrue(mockRumModule.removeViewAttributeCalled)
        XCTAssertEqual(mockRumModule.capturedRemoveViewAttributeKey, "theme")
    }

    // MARK: - addViewAttributes

    func testAddViewAttributes_callsRumModuleWithDict() {
        let attrs: NSDictionary = ["key1": "val1", "key2": 42]
        DdRum.addViewAttributes(attrs)

        XCTAssertTrue(mockRumModule.addViewAttributesCalled)
        XCTAssertNotNil(mockRumModule.capturedAddViewAttributesDict)
        XCTAssertEqual(mockRumModule.capturedAddViewAttributesDict?.count, 2)
    }

    // MARK: - removeViewAttributes

    func testRemoveViewAttributes_callsRumModuleWithKeys() {
        let keys: NSArray = ["key1", "key2"]
        DdRum.removeViewAttributes(keys)

        XCTAssertTrue(mockRumModule.removeViewAttributesCalled)
        XCTAssertEqual(mockRumModule.capturedRemoveViewAttributesKeys, ["key1", "key2"])
    }

    // MARK: - startFeatureOperation

    func testStartFeatureOperation_callsRumModuleWithNameAndKey() {
        let context: NSDictionary = ["step": "checkout"]
        DdRum.startFeatureOperation("checkout", operationKey: "op-1", context: context)

        XCTAssertTrue(mockRumModule.startFeatureOperationCalled)
        XCTAssertEqual(mockRumModule.capturedStartFeatureOperationName, "checkout")
        XCTAssertEqual(mockRumModule.capturedStartFeatureOperationKey, "op-1")
    }

    func testStartFeatureOperation_withNilOperationKey() {
        DdRum.startFeatureOperation("checkout", operationKey: nil, context: [:])

        XCTAssertTrue(mockRumModule.startFeatureOperationCalled)
        XCTAssertNil(mockRumModule.capturedStartFeatureOperationKey)
    }

    // MARK: - succeedFeatureOperation

    func testSucceedFeatureOperation_callsRumModuleWithNameAndKey() {
        let context: NSDictionary = ["result": "ok"]
        DdRum.succeedFeatureOperation("checkout", operationKey: "op-1", context: context)

        XCTAssertTrue(mockRumModule.succeedFeatureOperationCalled)
        XCTAssertEqual(mockRumModule.capturedSucceedFeatureOperationName, "checkout")
        XCTAssertEqual(mockRumModule.capturedSucceedFeatureOperationKey, "op-1")
    }

    func testSucceedFeatureOperation_withNilOperationKey() {
        DdRum.succeedFeatureOperation("checkout", operationKey: nil, context: [:])

        XCTAssertTrue(mockRumModule.succeedFeatureOperationCalled)
        XCTAssertNil(mockRumModule.capturedSucceedFeatureOperationKey)
    }

    // MARK: - failFeatureOperation

    func testFailFeatureOperation_callsRumModuleWithNameKeyAndReason() {
        let context: NSDictionary = ["error_code": 500]
        DdRum.failFeatureOperation("checkout", operationKey: "op-1", reason: "error", context: context)

        XCTAssertTrue(mockRumModule.failFeatureOperationCalled)
        XCTAssertEqual(mockRumModule.capturedFailFeatureOperationName, "checkout")
        XCTAssertEqual(mockRumModule.capturedFailFeatureOperationKey, "op-1")
        XCTAssertEqual(mockRumModule.capturedFailFeatureOperationReason, .error)
    }

    func testFailFeatureOperation_withNilOperationKey() {
        DdRum.failFeatureOperation("checkout", operationKey: nil, reason: "abandoned", context: [:])

        XCTAssertTrue(mockRumModule.failFeatureOperationCalled)
        XCTAssertNil(mockRumModule.capturedFailFeatureOperationKey)
        XCTAssertEqual(mockRumModule.capturedFailFeatureOperationReason, .abandoned)
    }

    // MARK: - mapFailureReason

    func testMapFailureReason_allValues() {
        XCTAssertEqual(DdRum.mapFailureReason("error"), .error)
        XCTAssertEqual(DdRum.mapFailureReason("abandoned"), .abandoned)
        XCTAssertEqual(DdRum.mapFailureReason("other"), .other)
        // Unknown defaults to error
        XCTAssertEqual(DdRum.mapFailureReason("unknown"), .error)
    }
}
