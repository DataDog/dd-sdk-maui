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
