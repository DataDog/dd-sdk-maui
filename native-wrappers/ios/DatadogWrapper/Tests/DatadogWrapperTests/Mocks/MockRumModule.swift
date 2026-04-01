import Foundation
import DatadogInternal
import DatadogRUM
@testable import DatadogWrapper

class MockRumModule: RumModuleProtocol {
    var enableCalled = false
    var capturedConfig: RUM.Configuration?

    var addErrorCalled = false
    var capturedErrorMessage: String?
    var capturedErrorSource: RUMErrorSource?
    var capturedErrorStacktrace: String?
    var capturedErrorAttributes: [AttributeKey: AttributeValue]?

    func enable(with configuration: RUM.Configuration) {
        enableCalled = true
        capturedConfig = configuration
    }

    func addError(message: String, source: RUMErrorSource, stacktrace: String?, attributes: [AttributeKey: AttributeValue]) {
        addErrorCalled = true
        capturedErrorMessage = message
        capturedErrorSource = source
        capturedErrorStacktrace = stacktrace
        capturedErrorAttributes = attributes
    }

    func reset() {
        enableCalled = false
        capturedConfig = nil
        addErrorCalled = false
        capturedErrorMessage = nil
        capturedErrorSource = nil
        capturedErrorStacktrace = nil
        capturedErrorAttributes = nil
    }
}
