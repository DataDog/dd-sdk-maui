import Foundation
import DatadogCore
import DatadogInternal
import DatadogCrashReporting

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

    static func mapProxyConfiguration(_ dict: NSDictionary?) -> [AnyHashable: Any]? {
        guard let dict = dict as? [String: Any],
              let type = dict["type"] as? String,
              let address = dict["address"] as? String,
              let port = dict["port"] as? Int else {
            return nil
        }

        var proxyDict: [AnyHashable: Any] = [:]

        switch type.lowercased() {
        case "http", "https":
            proxyDict["HTTPEnable"] = 1
            proxyDict["HTTPProxy"] = address
            proxyDict["HTTPPort"] = port
            proxyDict["HTTPSEnable"] = 1
            proxyDict["HTTPSProxy"] = address
            proxyDict["HTTPSPort"] = port
        case "socks":
            proxyDict["SOCKSEnable"] = 1
            proxyDict["SOCKSProxy"] = address
            proxyDict["SOCKSPort"] = port
        default:
            return nil
        }

        if let username = dict["username"] as? String {
            proxyDict[kCFProxyUsernameKey as String] = username
        }
        if let password = dict["password"] as? String {
            proxyDict[kCFProxyPasswordKey as String] = password
        }

        return proxyDict
    }

    // MARK: - First-party hosts storage

    /// Parsed first-party hosts stored during initialization.
    /// DdRum reads this when enabling RUM to configure urlSessionTracking on iOS.
    static var firstPartyHosts: [String: Set<TracingHeaderType>]? = nil

    /// Parses a flat dictionary of host -> comma-separated header types
    /// into the format expected by the Datadog SDK.
    static func parseFirstPartyHosts(_ dictionary: NSDictionary) -> [String: Set<TracingHeaderType>]? {
        guard let dict = dictionary as? [String: String] else { return nil }

        var hosts: [String: Set<TracingHeaderType>] = [:]
        for (host, headerTypesStr) in dict {
            let types = Set(
                headerTypesStr.split(separator: ",")
                    .map { DdRum.mapTracingHeaderType(String($0).trimmingCharacters(in: .whitespaces)) }
            )
            if !types.isEmpty {
                hosts[host] = types
            }
        }
        return hosts.isEmpty ? nil : hosts
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
    ///   - firstPartyHosts: Dictionary of host -> comma-separated header types (nullable)
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
        proxyConfiguration: NSDictionary?,
        firstPartyHosts firstPartyHostsDict: NSDictionary?,
        nativeCrashReportEnabled: Bool,
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

        if let proxyConfig = mapProxyConfiguration(proxyConfiguration) {
            configuration.proxyConfiguration = proxyConfig
        }

        // Store first-party hosts for RUM configuration
        if let hostsDict = firstPartyHostsDict {
            firstPartyHosts = parseFirstPartyHosts(hostsDict)
        } else {
            firstPartyHosts = nil
        }

        // Initialize Datadog SDK
        let core = DatadogCore.Datadog.initialize(
            with: configuration,
            trackingConsent: mapTrackingConsent(trackingConsent)
        )

        let initialized = !(core is DatadogInternal.NOPDatadogCore)

        if initialized {
            // DdRum.enableRum() will read this and activate crashReporting
            DdSdkNativeWrapper.nativeCrashReportEnabled = nativeCrashReportEnabled
        }

        return initialized
    }

    // Stored for DdRum to enable CrashReporting after RUM.enable()
    static var nativeCrashReportEnabled: Bool = false

    // MARK: - Tracking Consent

    // Dependencies - injectable for testing
    private static var datadogCore: DatadogSdkProtocol = RealDatadogSdk()

    static func setDatadogCore(_ core: DatadogSdkProtocol) {
        datadogCore = core
    }

    // Reset to production dependencies (for test cleanup)
    static func resetDependencies() {
        datadogCore = RealDatadogSdk()
        firstPartyHosts = nil
    }

    @objc public static func setTrackingConsent(_ consent: String) {
        datadogCore.setTrackingConsent(mapTrackingConsent(consent))
    }

    @objc public static func addAttribute(_ key: String, value: Any) {
        datadogCore.addAttribute(forKey: key, value: value)
    }

    @objc public static func addAttributes(_ attributes: NSDictionary) {
        guard let dict = attributes as? [String: Any] else { return }
        datadogCore.addAttributes(dict)
    }

    @objc public static func removeAttribute(_ key: String) {
        datadogCore.removeAttribute(forKey: key)
    }

    @objc public static func removeAttributes(_ keys: NSArray) {
        guard let keyList = keys as? [String] else { return }
        datadogCore.removeAttributes(forKeys: keyList)
    }

    // MARK: - User Info

    @objc public static func setUserInfo(_ id: String, name: String?, email: String?, extraInfo: NSDictionary) {
        let extra = (extraInfo as? [String: Any]) ?? [:]
        datadogCore.setUserInfo(id: id, name: name, email: email, extraInfo: extra)
    }

    @objc public static func addUserExtraInfo(_ extraInfo: NSDictionary) {
        let extra = (extraInfo as? [String: Any]) ?? [:]
        datadogCore.addUserExtraInfo(extra)
    }

    @objc public static func clearUserInfo() {
        datadogCore.clearUserInfo()
    }

    // MARK: - Account Info

    @objc public static func setAccountInfo(_ id: String, name: String?, extraInfo: NSDictionary) {
        let extra = (extraInfo as? [String: Any]) ?? [:]
        datadogCore.setAccountInfo(id: id, name: name, extraInfo: extra)
    }

    @objc public static func addAccountExtraInfo(_ extraInfo: NSDictionary) {
        let extra = (extraInfo as? [String: Any]) ?? [:]
        datadogCore.addAccountExtraInfo(extra)
    }

    @objc public static func clearAccountInfo() {
        datadogCore.clearAccountInfo()
    }

}
