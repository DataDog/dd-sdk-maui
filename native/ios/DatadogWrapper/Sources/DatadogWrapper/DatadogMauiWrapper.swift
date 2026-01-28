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
        let datadogSite = mapSite(site)
        let consent = mapTrackingConsent(trackingConsent)

        var configuration = Datadog.Configuration(
            clientToken: clientToken,
            env: env,
            site: datadogSite
        )

        if let serviceName = service, !serviceName.isEmpty {
            configuration.service = serviceName
        }

        Datadog.initialize(with: configuration, trackingConsent: consent)

        return Datadog.isInitialized
    }

    /// Checks if the Datadog SDK has been initialized.
    /// - Returns: true if initialized, false otherwise.
    @objc
    public static func isInitialized() -> Bool {
        return Datadog.isInitialized
    }

    /// Maps a site string to the corresponding Datadog.Configuration.DatadogSite value.
    private static func mapSite(_ site: String) -> DatadogSite {
        switch site.lowercased() {
        case "us1":
            return .us1
        case "us3":
            return .us3
        case "us5":
            return .us5
        case "eu1":
            return .eu1
        case "ap1":
            return .ap1
        case "ap2":
            // AP2 is supported in dd-sdk-ios 2.22.0
            return .ap2
        case "us1_fed":
            return .us1_fed
        default:
            return .us1
        }
    }

    /// Maps a tracking consent string to the corresponding TrackingConsent enum value.
    private static func mapTrackingConsent(_ consent: String) -> TrackingConsent {
        switch consent.lowercased() {
        case "granted":
            return .granted
        case "notgranted":
            return .notGranted
        default:
            return .pending
        }
    }
}
