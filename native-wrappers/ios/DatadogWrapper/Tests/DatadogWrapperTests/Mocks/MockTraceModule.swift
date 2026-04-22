/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

import Foundation
import DatadogTrace
@testable import DatadogWrapper

class MockTraceModule: TraceModuleProtocol {
    var enableCalled = false
    var enableWithConfigCalled = false
    var capturedConfig: Trace.Configuration?

    func enable() {
        enableCalled = true
    }

    func enable(with configuration: Trace.Configuration) {
        enableWithConfigCalled = true
        capturedConfig = configuration
    }

    func reset() {
        enableCalled = false
        enableWithConfigCalled = false
        capturedConfig = nil
    }
}