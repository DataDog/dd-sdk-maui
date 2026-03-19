import Foundation
import DatadogRUM
@testable import DatadogWrapper

class MockRumModule: RumModuleProtocol {
    var enableCalled = false
    var capturedConfig: RUM.Configuration?

    func enable(with configuration: RUM.Configuration) {
        enableCalled = true
        capturedConfig = configuration
    }

    func reset() {
        enableCalled = false
        capturedConfig = nil
    }
}
