import Foundation
import DatadogCore

@objc(DatadogWrapper)
public class DdSdkNativeWrapper: NSObject {

    // Store service name for use by feature modules
    private static var currentServiceName: String = "dd-sdk-maui"

    /// Initialize the Datadog SDK with configuration
    /// - Parameters:
    ///   - clientToken: Datadog client token for your application
    ///   - environment: Environment name (e.g., "prod", "staging")
    ///   - service: Service name for your application (nullable)
    ///   - site: Datadog site (e.g., "us1", "eu1", "us3", "us5", "ap1", "us1_fed")
    ///   - verbosity: SDK verbosity level ("debug", "info", "warn", "error")
    ///   - trackingConsent: Initial tracking consent ("granted", "not_granted", "pending")
    ///   - batchSize: Batch size for data uploads
    ///   - uploadFrequency: Upload frequency for data batches
    ///   - batchProcessingLevel: Batch processing level
    ///   - additionalConfiguration: Additional configuration dictionary (includes _dd.version, _dd.version_suffix, etc.)
    /// - Returns: Boolean indicating successful initialization
    @objc public static func initialize(
        clientToken: String,
        environment: String,
        service: String?,
        site: String,
        verbosity: String,
        trackingConsent: String,
        batchSize: String?,
        uploadFrequency: String?,
        batchProcessingLevel: String?,
        additionalConfiguration: NSDictionary?
    ) -> Bool {
        // Store service name for feature modules
        if let service = service {
            currentServiceName = service
        }

        var configuration = DatadogCore.Datadog.Configuration(
            clientToken: clientToken,
            env: environment,
            site: { () in
                switch site.lowercased() {
                case "us3": return .us3
                case "us5": return .us5
                case "eu1": return .eu1
                case "ap1": return .ap1
                case "ap2": return .ap2
                case "us1_fed": return .us1_fed
                default: return .us1
                }
            }(),
            service: service
        )

        // Map batch size
        if let batchSize = batchSize?.lowercased() {
            switch batchSize {
            case "small": configuration.batchSize = .small
            case "large": configuration.batchSize = .large
            default: configuration.batchSize = .medium
            }
        }

        // Map upload frequency
        if let uploadFrequency = uploadFrequency?.lowercased() {
            switch uploadFrequency {
            case "frequent": configuration.uploadFrequency = .frequent
            case "rare": configuration.uploadFrequency = .rare
            default: configuration.uploadFrequency = .average
            }
        }

        // Map batch processing level
        if let batchProcessingLevel = batchProcessingLevel?.lowercased() {
            switch batchProcessingLevel {
            case "low": configuration.batchProcessingLevel = .low
            case "high": configuration.batchProcessingLevel = .high
            default: configuration.batchProcessingLevel = .medium
            }
        }

        // Set SDK verbosity level
        switch verbosity.lowercased() {
        case "debug": DatadogCore.Datadog.verbosityLevel = .debug
        case "info": DatadogCore.Datadog.verbosityLevel = .debug  // iOS has no info level, use debug
        case "warn": DatadogCore.Datadog.verbosityLevel = .warn
        case "error": DatadogCore.Datadog.verbosityLevel = .error
        default: DatadogCore.Datadog.verbosityLevel = .error
        }

        // Map tracking consent
        let consent: TrackingConsent = {
            switch trackingConsent.lowercased() {
            case "granted": return .granted
            case "not_granted": return .notGranted
            default: return .pending
            }
        }()

        // Apply additional configuration
        if let additionalConfig = additionalConfiguration as? [String: Any] {
            configuration._internal_mutation {
                $0.additionalConfiguration = additionalConfig
            }
        }

        // Initialize Datadog SDK
        DatadogCore.Datadog.initialize(
            with: configuration,
            trackingConsent: consent
        )

        return true
    }

    /// Get the current service name (for use by feature modules)
    @objc public static func getServiceName() -> String {
        return currentServiceName
    }
}
