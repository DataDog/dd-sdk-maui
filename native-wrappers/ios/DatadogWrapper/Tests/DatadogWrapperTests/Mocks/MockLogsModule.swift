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
