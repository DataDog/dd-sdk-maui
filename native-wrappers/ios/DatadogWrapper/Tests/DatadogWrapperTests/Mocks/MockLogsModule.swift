/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

import Foundation
import DatadogLogs
@testable import DatadogWrapper

class MockLogsModule: LogsModuleProtocol {
    var enableCalled = false
    var enableWithConfigCalled = false
    var capturedConfig: Logs.Configuration?

    func enable() {
        enableCalled = true
    }

    func enable(with configuration: Logs.Configuration) {
        enableWithConfigCalled = true
        capturedConfig = configuration
    }

    func reset() {
        enableCalled = false
        enableWithConfigCalled = false
        capturedConfig = nil
    }
}