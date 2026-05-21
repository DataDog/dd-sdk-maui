/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

import Foundation
import DatadogCore
import DatadogInternal

/// Bridge for SDK-internal telemetry. Forwards wrapper-level errors, debug
/// messages, and configuration metadata into the iOS SDK's telemetry pipeline,
/// so they end up alongside native telemetry in the Datadog backend.
///
/// All entry points are no-ops if Datadog is not initialized.
@objc(DdTelemetry)
public class DdTelemetry: NSObject {

    // Dependencies - injectable for testing
    private static var telemetryProvider: TelemetryProviderProtocol = RealTelemetryProvider()

    static func setTelemetryProvider(_ provider: TelemetryProviderProtocol) {
        telemetryProvider = provider
    }

    // Reset to production dependencies (for test cleanup)
    static func resetDependencies() {
        telemetryProvider = RealTelemetryProvider()
    }

    // MARK: - Error / Debug

    @objc public static func error(id: String, message: String, kind: String?, stack: String?) {
        guard let telemetry = telemetryProvider.telemetry else { return }
        telemetry.error(
            id: id,
            message: message,
            kind: kind ?? "MauiSdkError",
            stack: stack ?? ""
        )
    }

    @objc public static func debug(id: String, message: String) {
        guard let telemetry = telemetryProvider.telemetry else { return }
        telemetry.debug(id: id, message: message)
    }

    // MARK: - Configuration telemetry

    /// Reports configuration telemetry. Fields are read out of the dictionary using
    /// keys that match the corresponding `Telemetry.configuration(...)` parameters.
    /// Unknown/missing keys are simply omitted.
    /// Argument order must match the SDK's `Telemetry.configuration(...)` declaration.
    @objc public static func reportConfiguration(_ fields: NSDictionary) {
        guard let telemetry = telemetryProvider.telemetry else { return }

        let dict = fields as? [String: Any] ?? [:]
        telemetry.configuration(
            appHangThreshold: (dict["appHangThreshold"] as? NSNumber)?.int64Value,
            initializationType: dict["initializationType"] as? String,
            sessionSampleRate: (dict["sessionSampleRate"] as? NSNumber)?.int64Value,
            telemetryConfigurationSampleRate: (dict["telemetryConfigurationSampleRate"] as? NSNumber)?.int64Value,
            telemetrySampleRate: (dict["telemetrySampleRate"] as? NSNumber)?.int64Value,
            tracerAPI: dict["tracerAPI"] as? String,
            traceSampleRate: (dict["traceSampleRate"] as? NSNumber)?.int64Value,
            trackBackgroundEvents: dict["trackBackgroundEvents"] as? Bool,
            trackErrors: dict["trackErrors"] as? Bool,
            trackFrustrations: dict["trackFrustrations"] as? Bool,
            trackLongTask: dict["trackLongTask"] as? Bool,
            trackNativeErrors: dict["trackNativeErrors"] as? Bool,
            trackNativeLongTasks: dict["trackNativeLongTasks"] as? Bool,
            trackNativeViews: dict["trackNativeViews"] as? Bool,
            trackResources: dict["trackResources"] as? Bool,
            trackUserInteractions: dict["trackUserInteractions"] as? Bool,
            trackViewsManually: dict["trackViewsManually"] as? Bool,
            mauiVersion: dict["mauiVersion"] as? String,
            useFirstPartyHosts: dict["useFirstPartyHosts"] as? Bool,
            useProxy: dict["useProxy"] as? Bool,
            useTracing: dict["useTracing"] as? Bool
        )
    }
}

/// Provider of the Telemetry handle. Allows test injection.
protocol TelemetryProviderProtocol {
    var telemetry: Telemetry? { get }
}

class RealTelemetryProvider: TelemetryProviderProtocol {
    var telemetry: Telemetry? {
        // CoreRegistry.default.telemetry returns a NOP if uninitialized,
        // so we can safely return it; callers do not need to check.
        return CoreRegistry.default.telemetry
    }
}
