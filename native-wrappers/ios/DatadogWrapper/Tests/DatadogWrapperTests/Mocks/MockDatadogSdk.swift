/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

import DatadogCore
@testable import DatadogWrapper

class MockDatadogCore: DatadogSdkProtocol {
    var setTrackingConsentCalls: [TrackingConsent] = []
    var addAttributeCalls: [(key: String, value: Any)] = []
    var addAttributesCalls: [[String: Any]] = []
    var removeAttributeCalls: [String] = []
    var removeAttributesCalls: [[String]] = []

    func setTrackingConsent(_ consent: TrackingConsent) {
        setTrackingConsentCalls.append(consent)
    }

    func addAttribute(forKey key: String, value: Any) {
        addAttributeCalls.append((key: key, value: value))
    }

    func addAttributes(_ attributes: [String: Any]) {
        addAttributesCalls.append(attributes)
    }

    func removeAttribute(forKey key: String) {
        removeAttributeCalls.append(key)
    }

    func removeAttributes(forKeys keys: [String]) {
        removeAttributesCalls.append(keys)
    }

    // User Info
    var setUserInfoCalls: [(id: String, name: String?, email: String?, extraInfo: [String: Any])] = []
    var addUserExtraInfoCalls: [[String: Any]] = []
    var clearUserInfoCallCount = 0

    func setUserInfo(id: String, name: String?, email: String?, extraInfo: [String: Any]) {
        setUserInfoCalls.append((id: id, name: name, email: email, extraInfo: extraInfo))
    }

    func addUserExtraInfo(_ extraInfo: [String: Any]) {
        addUserExtraInfoCalls.append(extraInfo)
    }

    func clearUserInfo() {
        clearUserInfoCallCount += 1
    }

    // Account Info
    var setAccountInfoCalls: [(id: String, name: String?, extraInfo: [String: Any])] = []
    var addAccountExtraInfoCalls: [[String: Any]] = []
    var clearAccountInfoCallCount = 0

    func setAccountInfo(id: String, name: String?, extraInfo: [String: Any]) {
        setAccountInfoCalls.append((id: id, name: name, extraInfo: extraInfo))
    }

    func addAccountExtraInfo(_ extraInfo: [String: Any]) {
        addAccountExtraInfoCalls.append(extraInfo)
    }

    func clearAccountInfo() {
        clearAccountInfoCallCount += 1
    }

    // Flush
    var flushCallCount = 0

    func flush() {
        flushCallCount += 1
    }
}