/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

import Foundation
import DatadogInternal
@testable import DatadogWrapper

class MockTelemetry: Telemetry {
    var sentTelemetry: [TelemetryMessage] = []

    func send(telemetry: TelemetryMessage) {
        sentTelemetry.append(telemetry)
    }
}

class MockTelemetryProvider: TelemetryProviderProtocol {
    let mockTelemetry: MockTelemetry?

    init(mockTelemetry: MockTelemetry? = MockTelemetry()) {
        self.mockTelemetry = mockTelemetry
    }

    var telemetry: Telemetry? {
        return mockTelemetry
    }
}
