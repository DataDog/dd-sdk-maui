import Foundation
import DatadogCore
import DatadogInternal
import DatadogRUM
import DatadogCrashReporting

@objc(DdRum)
public class DdRum: NSObject {

    // Dependencies - injectable for testing
    private static var rumModule: RumModuleProtocol = RealRumModule()

    // Stored for RUM-15107 (error tracking/crash reporting)
    static var nativeCrashReportEnabled: Bool = false

    // Stored for future resource tracking configuration
    static var initialResourceThreshold: Double? = nil

    // For testing: inject dependencies
    static func setRumModule(_ module: RumModuleProtocol) {
        rumModule = module
    }

    // Reset to production dependencies (for test cleanup)
    static func resetDependencies() {
        rumModule = RealRumModule()
        nativeCrashReportEnabled = false
        initialResourceThreshold = nil
    }

    // MARK: - Mapping helpers

    static func mapVitalsUpdateFrequency(_ frequency: String) -> RUM.Configuration.VitalsFrequency? {
        switch frequency.lowercased() {
        case "never": return nil
        case "rare": return .rare
        case "average": return .average
        case "frequent": return .frequent
        default: return .average
        }
    }

    static func mapTracingHeaderType(_ type: String) -> TracingHeaderType {
        switch type.lowercased() {
        case "b3": return .b3
        case "b3multi": return .b3multi
        case "tracecontext": return .tracecontext
        default: return .datadog
        }
    }

    // MARK: - Enable

    /// Enable the RUM module with configuration dictionary
    /// - Parameter configuration: NSDictionary with configuration values
    @objc(enableRum:)
    public static func enableRum(configuration: NSDictionary) {
        guard let config = configuration as? [String: Any] else {
            print("[Datadog] DdRum.enableRum: Invalid configuration dictionary.")
            return
        }

        guard let applicationId = config["applicationId"] as? String, !applicationId.isEmpty else {
            print("[Datadog] DdRum.enableRum: applicationId is required.")
            return
        }

        var rumConfig = RUM.Configuration(applicationID: applicationId)

        // Session sample rate
        if let sessionSampleRate = config["sessionSampleRate"] as? Double {
            rumConfig.sessionSampleRate = Float(sessionSampleRate)
        }

        // Telemetry sample rate
        if let telemetrySampleRate = config["telemetrySampleRate"] as? Double {
            rumConfig.telemetrySampleRate = Float(telemetrySampleRate)
        }

        // Vitals update frequency
        if let vitalsStr = config["vitalsUpdateFrequency"] as? String {
            rumConfig.vitalsUpdateFrequency = mapVitalsUpdateFrequency(vitalsStr)
        }

        // Native view tracking
        if let nativeViewTracking = config["nativeViewTracking"] as? Bool, nativeViewTracking {
            rumConfig.uiKitViewsPredicate = DefaultUIKitRUMViewsPredicate()
        }

        // Native interaction tracking
        if let nativeInteractionTracking = config["nativeInteractionTracking"] as? Bool, nativeInteractionTracking {
            rumConfig.uiKitActionsPredicate = DefaultUIKitRUMActionsPredicate()
        }

        // Long task threshold (ms -> seconds)
        if let longTaskMs = config["nativeLongTaskThresholdMs"] as? Double {
            rumConfig.longTaskThreshold = longTaskMs / 1_000.0
        }

        // Track frustrations
        if let trackFrustrations = config["trackFrustrations"] as? Bool {
            rumConfig.trackFrustrations = trackFrustrations
        }

        // Track background events
        if let trackBackgroundEvents = config["trackBackgroundEvents"] as? Bool {
            rumConfig.trackBackgroundEvents = trackBackgroundEvents
        }

        // App hang threshold (iOS only)
        if let appHangThreshold = config["appHangThreshold"] as? Double {
            rumConfig.appHangThreshold = appHangThreshold
        }

        // Track watchdog terminations (iOS only)
        if let trackWatchdogTerminations = config["trackWatchdogTerminations"] as? Bool {
            rumConfig.trackWatchdogTerminations = trackWatchdogTerminations
        }

        // Track memory warnings (iOS only)
        if let trackMemoryWarnings = config["trackMemoryWarnings"] as? Bool {
            rumConfig.trackMemoryWarnings = trackMemoryWarnings
        }

        // Custom endpoint
        if let customEndpoint = config["customEndpoint"] as? String,
           !customEndpoint.isEmpty,
           let url = URL(string: customEndpoint) {
            rumConfig.customEndpoint = url
        }

        // First party hosts + resource trace sample rate
        if let firstPartyHostsJson = config["firstPartyHosts"] as? String,
           let jsonData = firstPartyHostsJson.data(using: .utf8),
           let hostsArray = try? JSONSerialization.jsonObject(with: jsonData) as? [[String: Any]] {

            let resourceTraceSampleRate = config["resourceTraceSampleRate"] as? Double ?? 20.0

            var firstPartyHosts: [String: Set<TracingHeaderType>] = [:]
            for hostEntry in hostsArray {
                if let match = hostEntry["match"] as? String,
                   let headerTypes = hostEntry["headerTypes"] as? [String] {
                    let types = Set(headerTypes.map { mapTracingHeaderType($0) })
                    firstPartyHosts[match] = types
                }
            }

            if !firstPartyHosts.isEmpty {
                rumConfig.urlSessionTracking = .init(
                    firstPartyHostsTracing: .traceWithHeaders(
                        hostsWithHeaders: firstPartyHosts,
                        sampleRate: Float(resourceTraceSampleRate)
                    )
                )
            }
        }

        // Initial resource threshold (stored for resource tracking configuration)
        initialResourceThreshold = config["initialResourceThreshold"] as? Double

        // Enable native crash reporting if requested
        nativeCrashReportEnabled = config["nativeCrashReportEnabled"] as? Bool ?? false
        if nativeCrashReportEnabled {
            CrashReporting.enable()
        }

        rumModule.enable(with: rumConfig)
    }
}
