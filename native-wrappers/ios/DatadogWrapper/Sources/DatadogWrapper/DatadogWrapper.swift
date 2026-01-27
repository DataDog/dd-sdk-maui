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
    /// - Returns: Boolean indicating successful initialization
    @objc public static func initialize(
        clientToken: String,
        environment: String,
        service: String
    ) -> Bool {
        // Store service name for feature modules
        currentServiceName = service

        // Build configuration with site (dd-sdk-ios v3.x API)
        var configuration = Datadog.Configuration(
            clientToken: clientToken,
            env: environment,
            site: .us1  // US1 site (app.datadoghq.com)
        )

        // Enable verbose logging to see SDK internals
        Datadog.verbosityLevel = .debug

        // Initialize Datadog SDK
        Datadog.initialize(
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
