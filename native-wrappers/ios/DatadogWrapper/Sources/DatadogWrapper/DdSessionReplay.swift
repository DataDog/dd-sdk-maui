/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

import Foundation
import DatadogSessionReplay

@objc(DdSessionReplay)
public class DdSessionReplay: NSObject {

    // MARK: - Mapping helpers

    static func mapTextAndInputPrivacy(_ level: String) -> TextAndInputPrivacyLevel {
        switch level.lowercased() {
        case "mask_all_inputs": return .maskAllInputs
        case "mask_sensitive_inputs": return .maskSensitiveInputs
        default: return .maskAll
        }
    }

    static func mapImagePrivacy(_ level: String) -> ImagePrivacyLevel {
        switch level.lowercased() {
        case "mask_none": return .maskNone
        default: return .maskAll
        }
    }

    static func mapTouchPrivacy(_ level: String) -> TouchPrivacyLevel {
        switch level.lowercased() {
        case "show": return .show
        default: return .hide
        }
    }

    // MARK: - Enable

    @objc(enableSessionReplay:textAndInputPrivacy:imagePrivacy:touchPrivacy:customEndpoint:)
    public static func enableSessionReplay(
        replaySampleRate: Double,
        textAndInputPrivacy: String,
        imagePrivacy: String,
        touchPrivacy: String,
        customEndpoint: String?
    ) {
        var config = SessionReplay.Configuration(
            replaySampleRate: Float(replaySampleRate),
            textAndInputPrivacyLevel: mapTextAndInputPrivacy(textAndInputPrivacy),
            imagePrivacyLevel: mapImagePrivacy(imagePrivacy),
            touchPrivacyLevel: mapTouchPrivacy(touchPrivacy)
        )

        if let customEndpoint = customEndpoint,
           !customEndpoint.isEmpty,
           let url = URL(string: customEndpoint) {
            config.customEndpoint = url
        }

        SessionReplay.enable(with: config)
    }
}
