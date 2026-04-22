/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

import XCTest
import DatadogSessionReplay
@testable import DatadogWrapper

final class DdSessionReplayTests: XCTestCase {

    // MARK: - enableSessionReplay

    func testEnableSessionReplay_doesNotCrash() {
        DdSessionReplay.enableSessionReplay(
            replaySampleRate: 100.0,
            textAndInputPrivacy: "mask_all",
            imagePrivacy: "mask_all",
            touchPrivacy: "hide",
            customEndpoint: nil
        )
    }

    func testEnableSessionReplay_withCustomEndpoint_doesNotCrash() {
        DdSessionReplay.enableSessionReplay(
            replaySampleRate: 50.0,
            textAndInputPrivacy: "mask_sensitive_inputs",
            imagePrivacy: "mask_none",
            touchPrivacy: "show",
            customEndpoint: "https://sr.example.com"
        )
    }

    // MARK: - mapTextAndInputPrivacy

    func testMapTextAndInputPrivacy_allValues() {
        XCTAssertEqual(DdSessionReplay.mapTextAndInputPrivacy("mask_all"), .maskAll)
        XCTAssertEqual(DdSessionReplay.mapTextAndInputPrivacy("mask_all_inputs"), .maskAllInputs)
        XCTAssertEqual(DdSessionReplay.mapTextAndInputPrivacy("mask_sensitive_inputs"), .maskSensitiveInputs)
        XCTAssertEqual(DdSessionReplay.mapTextAndInputPrivacy("unknown"), .maskAll)
    }

    // MARK: - mapImagePrivacy

    func testMapImagePrivacy_allValues() {
        XCTAssertEqual(DdSessionReplay.mapImagePrivacy("mask_all"), .maskAll)
        XCTAssertEqual(DdSessionReplay.mapImagePrivacy("mask_none"), .maskNone)
        XCTAssertEqual(DdSessionReplay.mapImagePrivacy("unknown"), .maskAll)
    }

    // MARK: - mapTouchPrivacy

    func testMapTouchPrivacy_allValues() {
        XCTAssertEqual(DdSessionReplay.mapTouchPrivacy("hide"), .hide)
        XCTAssertEqual(DdSessionReplay.mapTouchPrivacy("show"), .show)
        XCTAssertEqual(DdSessionReplay.mapTouchPrivacy("unknown"), .hide)
    }
}
