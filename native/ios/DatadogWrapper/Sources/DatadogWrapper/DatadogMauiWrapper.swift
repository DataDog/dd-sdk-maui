import Foundation
import DatadogCore

/// ObjC-compatible wrapper for Datadog iOS SDK to be consumed by .NET MAUI iOS bindings.
@objc(DDMauiWrapper)
public class DatadogMauiWrapper: NSObject {

    /// Initializes the Datadog SDK with the provided configuration.
    /// - Parameters:
    ///   - clientToken: The client token for your Datadog application.
    ///   - env: The environment name (e.g., "production", "staging", "development").
    ///   - site: The Datadog site string (e.g., "us1", "eu1", "us3", "us5", "ap1", "ap2", "us1_fed").
    ///   - service: Optional service name. If nil, the app bundle identifier is used.
    ///   - trackingConsent: The tracking consent string ("granted", "notgranted", "pending").
    /// - Returns: true if initialization succeeds, false otherwise.
    @objc
    public static func initialize(
        clientToken: String,
        env: String,
        site: String,
        service: String?,
        trackingConsent: String
    ) -> Bool {
        // Create configuration - site will be set based on the string parameter
        var configuration = Datadog.Configuration(
            clientToken: clientToken,
            env: env
        )

        // Set site using string-based mapping
        // Since we can't directly reference DatadogSite, we use the default init
        // and then set properties via the Configuration's public setters
        switch site.lowercased() {
        case "us1":
            configuration.site = .us1
        case "us3":
            configuration.site = .us3
        case "us5":
            configuration.site = .us5
        case "eu1":
            configuration.site = .eu1
        case "ap1":
            configuration.site = .ap1
        case "ap2":
            // AP2 not available in dd-sdk-ios 2.22.0, falling back to AP1
            configuration.site = .ap1
        case "us1_fed":
            configuration.site = .us1_fed
        default:
            configuration.site = .us1
        }

        if let serviceName = service, !serviceName.isEmpty {
            configuration.service = serviceName
        }

        // Map tracking consent
        let consent: TrackingConsent
        switch trackingConsent.lowercased() {
        case "granted":
            consent = .granted
        case "notgranted":
            consent = .notGranted
        default:
            consent = .pending
        }

        Datadog.initialize(with: configuration, trackingConsent: consent)

        return Datadog.isInitialized()
    }

    /// Checks if the Datadog SDK has been initialized.
    /// - Returns: true if initialized, false otherwise.
    @objc
    public static func isInitialized() -> Bool {
        return Datadog.isInitialized()
    }
}
