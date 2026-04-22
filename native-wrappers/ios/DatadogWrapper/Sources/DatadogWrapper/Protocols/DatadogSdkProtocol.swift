/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

import Foundation
import DatadogCore
import DatadogInternal
import DatadogRUM

/// Protocol for Datadog core SDK operations
/// Allows for dependency injection and testing
protocol DatadogSdkProtocol {
    /// Set the tracking consent for data collection
    /// - Parameter consent: The new tracking consent value
    func setTrackingConsent(_ consent: TrackingConsent)

    /// Add a global attribute on the RUM monitor
    func addAttribute(forKey key: String, value: Any)

    /// Add multiple global attributes on the RUM monitor
    func addAttributes(_ attributes: [String: Any])

    /// Remove a global attribute from the RUM monitor
    func removeAttribute(forKey key: String)

    /// Remove multiple global attributes from the RUM monitor
    func removeAttributes(forKeys keys: [String])

    // User Info
    func setUserInfo(id: String, name: String?, email: String?, extraInfo: [String: Any])
    func addUserExtraInfo(_ extraInfo: [String: Any])
    func clearUserInfo()

    // Account Info
    func setAccountInfo(id: String, name: String?, extraInfo: [String: Any])
    func addAccountExtraInfo(_ extraInfo: [String: Any])
    func clearAccountInfo()
}

/// Production implementation that wraps the real Datadog SDK
class RealDatadogSdk: DatadogSdkProtocol {
    func setTrackingConsent(_ consent: TrackingConsent) {
        Datadog.set(trackingConsent: consent)
    }

    func addAttribute(forKey key: String, value: Any) {
        RUMMonitor.shared().addAttribute(forKey: key, value: AnyEncodable(value))
    }

    func addAttributes(_ attributes: [String: Any]) {
        let monitor = RUMMonitor.shared()
        for (key, value) in attributes {
            monitor.addAttribute(forKey: key, value: AnyEncodable(value))
        }
    }

    func removeAttribute(forKey key: String) {
        RUMMonitor.shared().removeAttribute(forKey: key)
    }

    func removeAttributes(forKeys keys: [String]) {
        let monitor = RUMMonitor.shared()
        for key in keys {
            monitor.removeAttribute(forKey: key)
        }
    }

    func setUserInfo(id: String, name: String?, email: String?, extraInfo: [String: Any]) {
        let encodableExtra = extraInfo.mapValues { AnyEncodable($0) }
        Datadog.setUserInfo(id: id, name: name, email: email, extraInfo: encodableExtra)
    }

    func addUserExtraInfo(_ extraInfo: [String: Any]) {
        let encodableExtra: [String: Encodable?] = extraInfo.mapValues { AnyEncodable($0) }
        Datadog.addUserExtraInfo(encodableExtra)
    }

    func clearUserInfo() {
        Datadog.clearUserInfo()
    }

    func setAccountInfo(id: String, name: String?, extraInfo: [String: Any]) {
        let encodableExtra = extraInfo.mapValues { AnyEncodable($0) }
        Datadog.setAccountInfo(id: id, name: name, extraInfo: encodableExtra)
    }

    func addAccountExtraInfo(_ extraInfo: [String: Any]) {
        let encodableExtra: [String: Encodable?] = extraInfo.mapValues { AnyEncodable($0) }
        Datadog.addAccountExtraInfo(encodableExtra)
    }

    func clearAccountInfo() {
        Datadog.clearAccountInfo()
    }
}