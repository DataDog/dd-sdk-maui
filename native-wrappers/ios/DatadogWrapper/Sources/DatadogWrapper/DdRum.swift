import Foundation
import DatadogCore
import DatadogCrashReporting
import DatadogInternal
import DatadogRUM
@objc(DdRum)
public class DdRum: NSObject {

    // Dependencies - injectable for testing
    private static var rumModule: RumModuleProtocol = RealRumModule()

    // Stored for future resource tracking configuration
    static var initialResourceThreshold: Double? = nil

    // For testing: inject dependencies
    static func setRumModule(_ module: RumModuleProtocol) {
        rumModule = module
    }

    // Reset to production dependencies (for test cleanup)
    static func resetDependencies() {
        rumModule = RealRumModule()
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

    static func mapErrorSource(_ source: String) -> RUMErrorSource {
        switch source.lowercased() {
        case "network": return .network
        case "source": return .source
        case "console": return .console
        case "webview": return .webview
        default: return .custom
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

        // First party hosts (stored during SDK initialization) + resource trace sample rate
        if let hosts = DdSdkNativeWrapper.firstPartyHosts {
            let resourceTraceSampleRate = config["resourceTraceSampleRate"] as? Double ?? 20.0
            rumConfig.urlSessionTracking = .init(
                firstPartyHostsTracing: .traceWithHeaders(
                    hostsWithHeaders: hosts,
                    sampleRate: Float(resourceTraceSampleRate)
                )
            )
        }

        // Initial resource threshold (stored for resource tracking configuration)
        initialResourceThreshold = config["initialResourceThreshold"] as? Double

        rumModule.enable(with: rumConfig)

        if DdSdkNativeWrapper.nativeCrashReportEnabled {
            CrashReporting.enable()
        }
    }

    // MARK: - Add Error

    /// Add a RUM error event
    /// - Parameters:
    ///   - message: Error message
    ///   - source: Error source (e.g., "source", "network", "console", "webview", "custom")
    ///   - stacktrace: Error stacktrace string
    ///   - context: Additional context dictionary
    ///   - timestampMs: Timestamp in milliseconds
    @objc(addError:source:stacktrace:context:timestampMs:)
    public static func addError(
        message: String,
        source: String,
        stacktrace: String,
        context: NSDictionary,
        timestampMs: Int64
    ) {
        var attributes: [AttributeKey: AttributeValue] = [:]

        if let contextDict = context as? [String: Any] {
            for (key, value) in contextDict {
                switch value {
                case let boolVal as Bool:
                    attributes[key] = boolVal
                case let intVal as Int:
                    attributes[key] = intVal
                case let doubleVal as Double:
                    attributes[key] = doubleVal
                case let stringVal as String:
                    attributes[key] = stringVal
                case let int64Val as Int64:
                    attributes[key] = int64Val
                default:
                    attributes[key] = String(describing: value)
                }
            }
        }

        if timestampMs > 0 {
            attributes["_dd.timestamp"] = timestampMs
        }

        attributes["_dd.error.source_type"] = "maui"

        rumModule.addError(
            message: message,
            source: mapErrorSource(source),
            stacktrace: stacktrace,
            attributes: attributes
        )
    }
}
