/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

import XCTest
import DatadogInternal
@testable import DatadogWrapper

final class DdTelemetryTests: XCTestCase {
    var mockTelemetry: MockTelemetry!
    var mockProvider: MockTelemetryProvider!

    override func setUp() {
        super.setUp()
        mockTelemetry = MockTelemetry()
        mockProvider = MockTelemetryProvider(mockTelemetry: mockTelemetry)
        DdTelemetry.setTelemetryProvider(mockProvider)
    }

    override func tearDown() {
        DdTelemetry.resetDependencies()
        mockTelemetry = nil
        mockProvider = nil
        super.tearDown()
    }

    // MARK: - Error

    func testError_forwardsAsErrorTelemetry() {
        DdTelemetry.error(id: "boom-id", message: "boom", kind: "WrapperError", stack: "at foo:1")

        XCTAssertEqual(mockTelemetry.sentTelemetry.count, 1)
        if case let .error(id, message, kind, stack) = mockTelemetry.sentTelemetry.first {
            XCTAssertEqual(id, "boom-id")
            XCTAssertEqual(message, "boom")
            XCTAssertEqual(kind, "WrapperError")
            XCTAssertEqual(stack, "at foo:1")
        } else {
            XCTFail("Expected .error telemetry")
        }
    }

    func testError_withNilKind_usesFallback() {
        DdTelemetry.error(id: "id", message: "boom", kind: nil, stack: nil)

        XCTAssertEqual(mockTelemetry.sentTelemetry.count, 1)
        if case let .error(_, _, kind, stack) = mockTelemetry.sentTelemetry.first {
            XCTAssertEqual(kind, "MauiSdkError")
            XCTAssertEqual(stack, "")
        } else {
            XCTFail("Expected .error telemetry")
        }
    }

    // MARK: - Debug

    func testDebug_forwardsAsDebugTelemetry() {
        DdTelemetry.debug(id: "id", message: "hello")

        XCTAssertEqual(mockTelemetry.sentTelemetry.count, 1)
        if case let .debug(id, message, _) = mockTelemetry.sentTelemetry.first {
            XCTAssertEqual(id, "id")
            XCTAssertEqual(message, "hello")
        } else {
            XCTFail("Expected .debug telemetry")
        }
    }

    // MARK: - Configuration

    func testReportConfiguration_emitsConfigurationTelemetry() {
        DdTelemetry.reportConfiguration([
            "mauiVersion": "9.0.0",
            "trackErrors": true,
            "useProxy": false,
            "sessionSampleRate": NSNumber(value: 80),
            "telemetrySampleRate": NSNumber(value: 100),
            "telemetryConfigurationSampleRate": NSNumber(value: 100)
        ])

        XCTAssertEqual(mockTelemetry.sentTelemetry.count, 1)
        if case let .configuration(config) = mockTelemetry.sentTelemetry.first {
            XCTAssertEqual(config.mauiVersion, "9.0.0")
            XCTAssertEqual(config.trackErrors, true)
            XCTAssertEqual(config.useProxy, false)
            XCTAssertEqual(config.sessionSampleRate, 80)
            XCTAssertEqual(config.telemetrySampleRate, 100)
            XCTAssertEqual(config.telemetryConfigurationSampleRate, 100)
        } else {
            XCTFail("Expected .configuration telemetry")
        }
    }

    func testReportConfiguration_omitsMissingFields() {
        DdTelemetry.reportConfiguration(["mauiVersion": "10.0.0"])

        XCTAssertEqual(mockTelemetry.sentTelemetry.count, 1)
        if case let .configuration(config) = mockTelemetry.sentTelemetry.first {
            XCTAssertEqual(config.mauiVersion, "10.0.0")
            XCTAssertNil(config.trackErrors)
            XCTAssertNil(config.useProxy)
        } else {
            XCTFail("Expected .configuration telemetry")
        }
    }

    // MARK: - No-op when telemetry unavailable

    func testError_isNoopWhenTelemetryNil() {
        DdTelemetry.setTelemetryProvider(MockTelemetryProvider(mockTelemetry: nil))

        // Should not crash.
        DdTelemetry.error(id: "id", message: "boom", kind: nil, stack: nil)
        DdTelemetry.debug(id: "id", message: "hello")
        DdTelemetry.reportConfiguration(["mauiVersion": "9.0.0"])
    }
}
