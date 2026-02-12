import Foundation
import DatadogCore

@objc(DatadogWrapper)
public class DatadogWrapper: NSObject {

    // Store service name for use by feature modules
    private static var currentServiceName: String = "dd-sdk-maui"

    /// Initialize the Datadog SDK with basic configuration
    /// - Parameters:
    ///   - clientToken: Datadog client token for your application
    ///   - environment: Environment name (e.g., "prod", "staging")
    ///   - service: Service name for your application
    ///   - site: Datadog site (e.g., "us1", "eu1", "us3", "us5", "ap1", "us1_fed")
    /// - Returns: Boolean indicating successful initialization
    @objc public static func initialize(
        clientToken: String,
        environment: String,
        service: String,
        site: String,
        verbosity: String
    ) -> Bool {
        // Store service name for feature modules
        currentServiceName = service

        let configuration = DatadogCore.Datadog.Configuration(
            clientToken: clientToken,
            env: environment,
            site: { () in
                switch site.lowercased() {
                case "us3": return .us3
                case "us5": return .us5
                case "eu1": return .eu1
                case "ap1": return .ap1
                case "us1_fed": return .us1_fed
                default: return .us1
                }
            }()
        )

        // Set SDK verbosity level
        switch verbosity.lowercased() {
        case "debug": DatadogCore.Datadog.verbosityLevel = .debug
        case "info": DatadogCore.Datadog.verbosityLevel = .debug  // iOS has no info level, use debug
        case "warn": DatadogCore.Datadog.verbosityLevel = .warn
        case "error": DatadogCore.Datadog.verbosityLevel = .error
        default: DatadogCore.Datadog.verbosityLevel = .error
        }

        // Initialize Datadog SDK
        DatadogCore.Datadog.initialize(
            with: configuration,
            trackingConsent: .granted
        )

        return true
    }

    /// Get the current service name (for use by feature modules)
    @objc public static func getServiceName() -> String {
        return currentServiceName
    }
}
