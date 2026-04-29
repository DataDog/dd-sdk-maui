/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

import Foundation
import DatadogLogs

/// Protocol for the Logs module functionality
/// Allows for dependency injection and testing
protocol LogsModuleProtocol {
    /// Enable the Logs module
    func enable()

    /// Enable the Logs module with custom configuration
    /// - Parameter configuration: Configuration for the Logs module
    func enable(with configuration: Logs.Configuration)
}

/// Production implementation that wraps the real Datadog Logs module
class RealLogsModule: LogsModuleProtocol {
    func enable() {
        Logs.enable()
    }

    func enable(with configuration: Logs.Configuration) {
        Logs.enable(with: configuration)
    }
}