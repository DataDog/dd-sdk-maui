import Foundation
import DatadogCore
import DatadogInternal

@objc(DatadogWrapper)
public class DdSdkNativeWrapper: NSObject {

    // MARK: - Mapping helpers

    static func mapSite(_ site: String) -> DatadogSite {
        switch site.lowercased() {
        case "us3": return .us3
        case "us5": return .us5
        case "eu1": return .eu1
        case "ap1": return .ap1
        case "ap2": return .ap2
        case "us1_fed": return .us1_fed
        default: return .us1
        }
    }

    static func mapTrackingConsent(_ consent: String) -> TrackingConsent {
        switch consent.lowercased() {
        case "granted": return .granted
        case "not_granted": return .notGranted
        default: return .pending
        }
    }

    static func mapVerbosity(_ verbosity: String) -> CoreLoggerLevel {
        switch verbosity.lowercased() {
        case "debug": return .debug
        case "info": return .debug  // iOS has no info level, use debug
        case "warn": return .warn
        case "error": return .error
        default: return .error
        }
    }

    static func mapBatchSize(_ batchSize: String) -> Datadog.Configuration.BatchSize {
        switch batchSize.lowercased() {
        case "small": return .small
        case "large": return .large
        default: return .medium
        }
    }

    static func mapUploadFrequency(_ uploadFrequency: String) -> Datadog.Configuration.UploadFrequency {
        switch uploadFrequency.lowercased() {
        case "frequent": return .frequent
        case "rare": return .rare
        default: return .average
        }
    }

    static func mapBatchProcessingLevel(_ level: String) -> Datadog.Configuration.BatchProcessingLevel {
        switch level.lowercased() {
        case "low": return .low
        case "high": return .high
        default: return .medium
        }
    }

    // MARK: - Initialization

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
        var configuration = DatadogCore.Datadog.Configuration(
            clientToken: clientToken,
            env: environment,
            site: mapSite(site),
            service: service
        )

        if let batchSize = batchSize {
            configuration.batchSize = mapBatchSize(batchSize)
        }

        if let uploadFrequency = uploadFrequency {
            configuration.uploadFrequency = mapUploadFrequency(uploadFrequency)
        }

        if let batchProcessingLevel = batchProcessingLevel {
            configuration.batchProcessingLevel = mapBatchProcessingLevel(batchProcessingLevel)
        }

        DatadogCore.Datadog.verbosityLevel = mapVerbosity(verbosity)

        // Apply additional configuration
        if let additionalConfig = additionalConfiguration as? [String: Any] {
            configuration._internal_mutation {
                $0.additionalConfiguration = additionalConfig
            }
        }

        // Initialize Datadog SDK
        let core = DatadogCore.Datadog.initialize(
            with: configuration,
            trackingConsent: mapTrackingConsent(trackingConsent)
        )

        return !(core is DatadogInternal.NOPDatadogCore)
    }

}
