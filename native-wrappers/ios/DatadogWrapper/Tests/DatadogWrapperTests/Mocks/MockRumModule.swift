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

    // Views
    var startViewCalled = false
    var capturedStartViewKey: String?
    var capturedStartViewName: String?
    var capturedStartViewAttributes: [AttributeKey: AttributeValue]?

    var stopViewCalled = false
    var capturedStopViewKey: String?
    var capturedStopViewAttributes: [AttributeKey: AttributeValue]?

    // Actions
    var startActionCalled = false
    var capturedStartActionType: RUMActionType?
    var capturedStartActionName: String?
    var capturedStartActionAttributes: [AttributeKey: AttributeValue]?

    var stopActionCalled = false
    var capturedStopActionType: RUMActionType?
    var capturedStopActionName: String?
    var capturedStopActionAttributes: [AttributeKey: AttributeValue]?

    var addActionCalled = false
    var capturedAddActionType: RUMActionType?
    var capturedAddActionName: String?
    var capturedAddActionAttributes: [AttributeKey: AttributeValue]?

    // Resources
    var startResourceCalled = false
    var capturedStartResourceKey: String?
    var capturedStartResourceMethod: RUMMethod?
    var capturedStartResourceUrl: String?
    var capturedStartResourceAttributes: [AttributeKey: AttributeValue]?

    var stopResourceCalled = false
    var capturedStopResourceKey: String?
    var capturedStopResourceStatusCode: Int?
    var capturedStopResourceKind: RUMResourceType?
    var capturedStopResourceSize: Int64?
    var capturedStopResourceAttributes: [AttributeKey: AttributeValue]?

    // Timing
    var addTimingCalled = false
    var capturedTimingName: String?

    var addViewLoadingTimeCalled = false
    var capturedViewLoadingTimeOverwrite: Bool?

    // Session
    var stopSessionCalled = false

    // View Attributes
    var addViewAttributeCalled = false
    var capturedAddViewAttributeKey: AttributeKey?
    var capturedAddViewAttributeValue: AttributeValue?

    var removeViewAttributeCalled = false
    var capturedRemoveViewAttributeKey: AttributeKey?

    var addViewAttributesCalled = false
    var capturedAddViewAttributesDict: [AttributeKey: AttributeValue]?

    var removeViewAttributesCalled = false
    var capturedRemoveViewAttributesKeys: [AttributeKey]?

    // Feature Operations
    var startFeatureOperationCalled = false
    var capturedStartFeatureOperationName: String?
    var capturedStartFeatureOperationKey: String?
    var capturedStartFeatureOperationAttributes: [AttributeKey: AttributeValue]?

    var succeedFeatureOperationCalled = false
    var capturedSucceedFeatureOperationName: String?
    var capturedSucceedFeatureOperationKey: String?
    var capturedSucceedFeatureOperationAttributes: [AttributeKey: AttributeValue]?

    var failFeatureOperationCalled = false
    var capturedFailFeatureOperationName: String?
    var capturedFailFeatureOperationKey: String?
    var capturedFailFeatureOperationReason: RUMFeatureOperationFailureReason?
    var capturedFailFeatureOperationAttributes: [AttributeKey: AttributeValue]?

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

    func startView(key: String, name: String, attributes: [AttributeKey: AttributeValue]) {
        startViewCalled = true
        capturedStartViewKey = key
        capturedStartViewName = name
        capturedStartViewAttributes = attributes
    }

    func stopView(key: String, attributes: [AttributeKey: AttributeValue]) {
        stopViewCalled = true
        capturedStopViewKey = key
        capturedStopViewAttributes = attributes
    }

    func startAction(type: RUMActionType, name: String, attributes: [AttributeKey: AttributeValue]) {
        startActionCalled = true
        capturedStartActionType = type
        capturedStartActionName = name
        capturedStartActionAttributes = attributes
    }

    func stopAction(type: RUMActionType, name: String, attributes: [AttributeKey: AttributeValue]) {
        stopActionCalled = true
        capturedStopActionType = type
        capturedStopActionName = name
        capturedStopActionAttributes = attributes
    }

    func addAction(type: RUMActionType, name: String, attributes: [AttributeKey: AttributeValue]) {
        addActionCalled = true
        capturedAddActionType = type
        capturedAddActionName = name
        capturedAddActionAttributes = attributes
    }

    func startResource(resourceKey: String, httpMethod: RUMMethod, urlString: String, attributes: [AttributeKey: AttributeValue]) {
        startResourceCalled = true
        capturedStartResourceKey = resourceKey
        capturedStartResourceMethod = httpMethod
        capturedStartResourceUrl = urlString
        capturedStartResourceAttributes = attributes
    }

    func stopResource(resourceKey: String, statusCode: Int?, kind: RUMResourceType, size: Int64?, attributes: [AttributeKey: AttributeValue]) {
        stopResourceCalled = true
        capturedStopResourceKey = resourceKey
        capturedStopResourceStatusCode = statusCode
        capturedStopResourceKind = kind
        capturedStopResourceSize = size
        capturedStopResourceAttributes = attributes
    }

    func addTiming(name: String) {
        addTimingCalled = true
        capturedTimingName = name
    }

    func addViewLoadingTime(overwrite: Bool) {
        addViewLoadingTimeCalled = true
        capturedViewLoadingTimeOverwrite = overwrite
    }

    func stopSession() {
        stopSessionCalled = true
    }

    func addViewAttribute(forKey key: AttributeKey, value: AttributeValue) {
        addViewAttributeCalled = true
        capturedAddViewAttributeKey = key
        capturedAddViewAttributeValue = value
    }

    func removeViewAttribute(forKey key: AttributeKey) {
        removeViewAttributeCalled = true
        capturedRemoveViewAttributeKey = key
    }

    func addViewAttributes(_ attributes: [AttributeKey: AttributeValue]) {
        addViewAttributesCalled = true
        capturedAddViewAttributesDict = attributes
    }

    func removeViewAttributes(forKeys keys: [AttributeKey]) {
        removeViewAttributesCalled = true
        capturedRemoveViewAttributesKeys = keys
    }

    func startFeatureOperation(name: String, operationKey: String?, attributes: [AttributeKey: AttributeValue]) {
        startFeatureOperationCalled = true
        capturedStartFeatureOperationName = name
        capturedStartFeatureOperationKey = operationKey
        capturedStartFeatureOperationAttributes = attributes
    }

    func succeedFeatureOperation(name: String, operationKey: String?, attributes: [AttributeKey: AttributeValue]) {
        succeedFeatureOperationCalled = true
        capturedSucceedFeatureOperationName = name
        capturedSucceedFeatureOperationKey = operationKey
        capturedSucceedFeatureOperationAttributes = attributes
    }

    func failFeatureOperation(name: String, operationKey: String?, reason: RUMFeatureOperationFailureReason, attributes: [AttributeKey: AttributeValue]) {
        failFeatureOperationCalled = true
        capturedFailFeatureOperationName = name
        capturedFailFeatureOperationKey = operationKey
        capturedFailFeatureOperationReason = reason
        capturedFailFeatureOperationAttributes = attributes
    }

    func reset() {
        enableCalled = false
        capturedConfig = nil
        addErrorCalled = false
        capturedErrorMessage = nil
        capturedErrorSource = nil
        capturedErrorStacktrace = nil
        capturedErrorAttributes = nil
        startViewCalled = false
        capturedStartViewKey = nil
        capturedStartViewName = nil
        capturedStartViewAttributes = nil
        stopViewCalled = false
        capturedStopViewKey = nil
        capturedStopViewAttributes = nil
        startActionCalled = false
        capturedStartActionType = nil
        capturedStartActionName = nil
        capturedStartActionAttributes = nil
        stopActionCalled = false
        capturedStopActionType = nil
        capturedStopActionName = nil
        capturedStopActionAttributes = nil
        addActionCalled = false
        capturedAddActionType = nil
        capturedAddActionName = nil
        capturedAddActionAttributes = nil
        startResourceCalled = false
        capturedStartResourceKey = nil
        capturedStartResourceMethod = nil
        capturedStartResourceUrl = nil
        capturedStartResourceAttributes = nil
        stopResourceCalled = false
        capturedStopResourceKey = nil
        capturedStopResourceStatusCode = nil
        capturedStopResourceKind = nil
        capturedStopResourceSize = nil
        capturedStopResourceAttributes = nil
        addTimingCalled = false
        capturedTimingName = nil
        addViewLoadingTimeCalled = false
        capturedViewLoadingTimeOverwrite = nil
        stopSessionCalled = false
        addViewAttributeCalled = false
        capturedAddViewAttributeKey = nil
        capturedAddViewAttributeValue = nil
        removeViewAttributeCalled = false
        capturedRemoveViewAttributeKey = nil
        addViewAttributesCalled = false
        capturedAddViewAttributesDict = nil
        removeViewAttributesCalled = false
        capturedRemoveViewAttributesKeys = nil
        startFeatureOperationCalled = false
        capturedStartFeatureOperationName = nil
        capturedStartFeatureOperationKey = nil
        capturedStartFeatureOperationAttributes = nil
        succeedFeatureOperationCalled = false
        capturedSucceedFeatureOperationName = nil
        capturedSucceedFeatureOperationKey = nil
        capturedSucceedFeatureOperationAttributes = nil
        failFeatureOperationCalled = false
        capturedFailFeatureOperationName = nil
        capturedFailFeatureOperationKey = nil
        capturedFailFeatureOperationReason = nil
        capturedFailFeatureOperationAttributes = nil
    }
}
