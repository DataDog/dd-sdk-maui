/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

import Foundation
@testable import DatadogWrapper

class MockLoggerWrapper: LoggerWrapperProtocol {
    struct LogCall {
        let level: String
        let message: String
        let attributes: [String: Encodable]?
    }

    var logCalls: [LogCall] = []

    func debug(_ message: String) {
        logCalls.append(LogCall(level: "debug", message: message, attributes: nil))
    }

    func info(_ message: String) {
        logCalls.append(LogCall(level: "info", message: message, attributes: nil))
    }

    func warn(_ message: String) {
        logCalls.append(LogCall(level: "warn", message: message, attributes: nil))
    }

    func error(_ message: String) {
        logCalls.append(LogCall(level: "error", message: message, attributes: nil))
    }

    func critical(_ message: String) {
        logCalls.append(LogCall(level: "critical", message: message, attributes: nil))
    }

    func debug(_ message: String, attributes: [String: Encodable]) {
        logCalls.append(LogCall(level: "debug", message: message, attributes: attributes))
    }

    func info(_ message: String, attributes: [String: Encodable]) {
        logCalls.append(LogCall(level: "info", message: message, attributes: attributes))
    }

    func warn(_ message: String, attributes: [String: Encodable]) {
        logCalls.append(LogCall(level: "warn", message: message, attributes: attributes))
    }

    func error(_ message: String, attributes: [String: Encodable]) {
        logCalls.append(LogCall(level: "error", message: message, attributes: attributes))
    }

    func critical(_ message: String, attributes: [String: Encodable]) {
        logCalls.append(LogCall(level: "critical", message: message, attributes: attributes))
    }

    func reset() {
        logCalls.removeAll()
    }
}