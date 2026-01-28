import Foundation
import DatadogCore
import DatadogRUM
import DatadogInternal
import DatadogCrashReporting

/// ObjC-compatible wrapper for Datadog iOS SDK to be consumed by .NET MAUI iOS bindings.
@objc(DDMauiWrapper)
public class DatadogMauiWrapper: NSObject {

    /// Stores the current site for use in RUM configuration
    private static var currentSite: String = "us1"

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
        // Enable verbose logging for debugging
        Datadog.verbosityLevel = .debug

        NSLog("[DatadogMauiWrapper] Initializing with clientToken: \(clientToken.prefix(10))..., env: \(env), site: \(site)")

        // Store site for later use in RUM configuration
        currentSite = site.lowercased()

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
        case "staging":
            // Datadog staging environment (datad0g.com)
            // Use US1 as base site, custom endpoints will be set in RUM/Logs/Traces
            configuration.site = .us1
            NSLog("[DatadogMauiWrapper] Using staging mode with custom endpoints")
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

        let initialized = Datadog.isInitialized()
        NSLog("[DatadogMauiWrapper] SDK initialized: \(initialized)")
        return initialized
    }

    /// Checks if the Datadog SDK has been initialized.
    /// - Returns: true if initialized, false otherwise.
    @objc
    public static func isInitialized() -> Bool {
        return Datadog.isInitialized()
    }

    // MARK: - Crash Reporting

    /// Enables native crash reporting.
    /// Must be called after SDK initialization.
    /// - Returns: true if crash reporting was enabled successfully, false otherwise.
    @objc
    public static func enableCrashReporting() -> Bool {
        guard Datadog.isInitialized() else {
            NSLog("[DatadogMauiWrapper] Cannot enable crash reporting: SDK not initialized")
            return false
        }
        CrashReporting.enable()
        NSLog("[DatadogMauiWrapper] Crash reporting enabled")
        return true
    }

    // MARK: - RUM Methods

    private static var rumEnabled = false

    /// Enables RUM (Real User Monitoring) with the provided application ID.
    /// Must be called after SDK initialization.
    /// - Parameters:
    ///   - applicationId: The RUM application ID from Datadog.
    ///   - sampleRate: The sample rate for RUM sessions (0.0 to 100.0).
    /// - Returns: true if RUM was enabled successfully, false otherwise.
    @objc
    public static func enableRum(applicationId: String, sampleRate: Float) -> Bool {
        NSLog("[DatadogMauiWrapper] enableRum called with appId: \(applicationId.prefix(10))..., sampleRate: \(sampleRate)")

        guard Datadog.isInitialized() else {
            NSLog("[DatadogMauiWrapper] Cannot enable RUM: SDK not initialized")
            return false
        }

        guard !rumEnabled else {
            NSLog("[DatadogMauiWrapper] RUM is already enabled")
            return true
        }

        var rumConfig = RUM.Configuration(applicationID: applicationId)
        rumConfig.sessionSampleRate = sampleRate
        rumConfig.uiKitViewsPredicate = DefaultUIKitRUMViewsPredicate()
        rumConfig.uiKitActionsPredicate = DefaultUIKitRUMActionsPredicate()
        rumConfig.longTaskThreshold = 0.1

        // Set custom endpoint for staging
        if currentSite == "staging" {
            if let stagingEndpoint = URL(string: "https://rum.browser-intake-datad0g.com/api/v2/rum") {
                rumConfig.customEndpoint = stagingEndpoint
                NSLog("[DatadogMauiWrapper] Using staging RUM endpoint: \(stagingEndpoint)")
            }
        }

        RUM.enable(with: rumConfig)
        rumEnabled = true
        NSLog("[DatadogMauiWrapper] RUM enabled successfully")
        return true
    }

    /// Checks if RUM is enabled.
    /// - Returns: true if RUM is enabled, false otherwise.
    @objc
    public static func isRumEnabled() -> Bool {
        return rumEnabled
    }

    /// Starts tracking a view.
    /// - Parameters:
    ///   - key: Unique identifier for the view.
    ///   - name: Human-readable name for the view.
    ///   - attributes: Optional custom attributes.
    @objc
    public static func startView(key: String, name: String, attributes: NSDictionary?) {
        NSLog("[DatadogMauiWrapper] startView called: key=\(key), name=\(name)")
        guard rumEnabled else {
            NSLog("[DatadogMauiWrapper] startView skipped: RUM not enabled")
            return
        }
        let attrs = convertAttributes(attributes)
        RUMMonitor.shared().startView(key: key, name: name, attributes: attrs)
        NSLog("[DatadogMauiWrapper] startView sent to RUMMonitor")
    }

    /// Stops tracking a view.
    /// - Parameters:
    ///   - key: Unique identifier for the view.
    ///   - attributes: Optional custom attributes.
    @objc
    public static func stopView(key: String, attributes: NSDictionary?) {
        guard rumEnabled else { return }
        let attrs = convertAttributes(attributes)
        RUMMonitor.shared().stopView(key: key, attributes: attrs)
    }

    /// Adds a user action event.
    /// - Parameters:
    ///   - type: Action type string ("TAP", "CLICK", "SCROLL", "SWIPE", "CUSTOM").
    ///   - name: Human-readable name for the action.
    ///   - attributes: Optional custom attributes.
    @objc
    public static func addAction(type: String, name: String, attributes: NSDictionary?) {
        NSLog("[DatadogMauiWrapper] addAction called: type=\(type), name=\(name)")
        guard rumEnabled else {
            NSLog("[DatadogMauiWrapper] addAction skipped: RUM not enabled")
            return
        }
        let actionType = mapActionType(type)
        let attrs = convertAttributes(attributes)
        RUMMonitor.shared().addAction(type: actionType, name: name, attributes: attrs)
        NSLog("[DatadogMauiWrapper] addAction sent to RUMMonitor")
    }

    /// Adds an error event.
    /// - Parameters:
    ///   - message: Error message.
    ///   - source: Error source string ("SOURCE", "NETWORK", "WEBVIEW", "CONSOLE", "CUSTOM").
    ///   - stackTrace: Optional stack trace.
    ///   - attributes: Optional custom attributes.
    @objc
    public static func addError(message: String, source: String, stackTrace: String?, attributes: NSDictionary?) {
        guard rumEnabled else { return }
        let errorSource = mapErrorSource(source)
        var attrs: [AttributeKey: AttributeValue] = convertAttributes(attributes)
        if let stack = stackTrace, !stack.isEmpty {
            attrs["error.stack"] = stack
        }
        RUMMonitor.shared().addError(message: message, source: errorSource, attributes: attrs)
    }

    /// Starts tracking a resource request.
    /// - Parameters:
    ///   - key: Unique identifier for the resource.
    ///   - httpMethod: HTTP method (e.g., "GET", "POST").
    ///   - url: The URL of the resource.
    ///   - attributes: Optional custom attributes.
    @objc
    public static func startResource(key: String, httpMethod: String, url: String, attributes: NSDictionary?) {
        guard rumEnabled else { return }
        guard let resourceUrl = URL(string: url) else {
            NSLog("[DatadogMauiWrapper] Invalid URL for resource: \(url)")
            return
        }
        let method = mapHttpMethod(httpMethod)
        let attrs = convertAttributes(attributes)
        RUMMonitor.shared().startResource(resourceKey: key, httpMethod: method, urlString: url, attributes: attrs)
    }

    /// Stops tracking a resource request that completed successfully.
    /// - Parameters:
    ///   - key: Unique identifier for the resource.
    ///   - statusCode: HTTP status code.
    ///   - size: Response size in bytes (-1 if unknown).
    ///   - attributes: Optional custom attributes.
    @objc
    public static func stopResource(key: String, statusCode: Int32, size: Int64, attributes: NSDictionary?) {
        guard rumEnabled else { return }
        let kind = RUMResourceType.other
        let attrs = convertAttributes(attributes)
        let sizeValue: Int64? = size >= 0 ? size : nil
        RUMMonitor.shared().stopResource(resourceKey: key, statusCode: Int(statusCode), kind: kind, size: sizeValue, attributes: attrs)
    }

    /// Stops tracking a resource request that failed with an error.
    /// - Parameters:
    ///   - key: Unique identifier for the resource.
    ///   - message: Error message.
    ///   - attributes: Optional custom attributes.
    @objc
    public static func stopResourceWithError(key: String, message: String, attributes: NSDictionary?) {
        guard rumEnabled else { return }
        let attrs = convertAttributes(attributes)
        let error = NSError(domain: "DatadogMauiWrapper", code: -1, userInfo: [NSLocalizedDescriptionKey: message])
        RUMMonitor.shared().stopResourceWithError(resourceKey: key, error: error, response: nil, attributes: attrs)
    }

    // MARK: - Private Helpers

    private static func mapActionType(_ type: String) -> RUMActionType {
        switch type.uppercased() {
        case "TAP":
            return .tap
        case "CLICK":
            return .tap // iOS SDK uses tap for both
        case "SCROLL":
            return .scroll
        case "SWIPE":
            return .swipe
        case "CUSTOM":
            return .custom
        default:
            return .custom
        }
    }

    private static func mapErrorSource(_ source: String) -> RUMErrorSource {
        switch source.uppercased() {
        case "SOURCE":
            return .source
        case "NETWORK":
            return .network
        case "WEBVIEW":
            return .webview
        case "CONSOLE":
            return .console
        case "CUSTOM":
            return .source // CUSTOM maps to source as closest match
        default:
            return .source
        }
    }

    private static func mapHttpMethod(_ method: String) -> RUMMethod {
        switch method.uppercased() {
        case "GET":
            return .get
        case "POST":
            return .post
        case "PUT":
            return .put
        case "DELETE":
            return .delete
        case "PATCH":
            return .patch
        case "HEAD":
            return .head
        case "OPTIONS":
            return .options
        case "TRACE":
            return .trace
        case "CONNECT":
            return .connect
        default:
            return .get
        }
    }

    private static func convertAttributes(_ dict: NSDictionary?) -> [AttributeKey: AttributeValue] {
        guard let dict = dict else { return [:] }
        var result: [AttributeKey: AttributeValue] = [:]
        for (key, value) in dict {
            if let keyString = key as? String {
                if let stringValue = value as? String {
                    result[keyString] = stringValue
                } else if let intValue = value as? Int {
                    result[keyString] = intValue
                } else if let doubleValue = value as? Double {
                    result[keyString] = doubleValue
                } else if let boolValue = value as? Bool {
                    result[keyString] = boolValue
                } else {
                    result[keyString] = String(describing: value)
                }
            }
        }
        return result
    }
}
