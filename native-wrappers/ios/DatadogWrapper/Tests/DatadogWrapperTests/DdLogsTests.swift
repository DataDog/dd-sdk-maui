import XCTest
@testable import DatadogWrapper

final class DdLogsTests: XCTestCase {
    var mockLogsModule: MockLogsModule!
    var mockLogger: MockLoggerWrapper!

    override func setUp() {
        super.setUp()

        // Set up mocks
        mockLogsModule = MockLogsModule()
        mockLogger = MockLoggerWrapper()

        DdLogs.setLogsModule(mockLogsModule)
        DdLogs.setLogger(mockLogger)
    }

    override func tearDown() {
        // Reset to production dependencies
        DdLogs.resetDependencies()
        mockLogsModule = nil
        mockLogger = nil
        super.tearDown()
    }

    func testEnableLogs_withNilEndpoint_callsEnableWithoutConfig() {
        DdLogs.enableLogs(customEndpoint: nil)

        // Verify Logs.enable() was called (not enable(with:))
        XCTAssertTrue(mockLogsModule.enableCalled)
        XCTAssertFalse(mockLogsModule.enableWithConfigCalled)
        XCTAssertNil(mockLogsModule.capturedConfig)
    }

    func testEnableLogs_withValidCustomEndpoint_callsEnableWithConfig() {
        let customEndpoint = "https://custom-logs.example.com/v1/input"

        DdLogs.enableLogs(customEndpoint: customEndpoint)

        // Verify Logs.enable(with:) was called with custom endpoint
        XCTAssertTrue(mockLogsModule.enableWithConfigCalled)
        XCTAssertFalse(mockLogsModule.enableCalled)
        XCTAssertNotNil(mockLogsModule.capturedConfig)
        XCTAssertEqual(mockLogsModule.capturedConfig?.customEndpoint?.absoluteString, customEndpoint)
    }

    func testEnableLogs_withEmptyString_fallsBackToDefault() {
        DdLogs.enableLogs(customEndpoint: "")

        // Empty string should fall back to default (no config)
        XCTAssertTrue(mockLogsModule.enableCalled)
        XCTAssertFalse(mockLogsModule.enableWithConfigCalled)
    }

    func testLogDebug_callsLoggerDebugMethod() {
        DdLogs.logDebug("Debug message")

        XCTAssertEqual(mockLogger.logCalls.count, 1)
        XCTAssertEqual(mockLogger.logCalls[0].level, "debug")
        XCTAssertEqual(mockLogger.logCalls[0].message, "Debug message")
        XCTAssertNil(mockLogger.logCalls[0].attributes)
    }

    func testLogInfo_callsLoggerInfoMethod() {
        DdLogs.logInfo("Info message")

        XCTAssertEqual(mockLogger.logCalls.count, 1)
        XCTAssertEqual(mockLogger.logCalls[0].level, "info")
        XCTAssertEqual(mockLogger.logCalls[0].message, "Info message")
    }

    func testLogWarn_callsLoggerWarnMethod() {
        DdLogs.logWarn("Warn message")

        XCTAssertEqual(mockLogger.logCalls.count, 1)
        XCTAssertEqual(mockLogger.logCalls[0].level, "warn")
        XCTAssertEqual(mockLogger.logCalls[0].message, "Warn message")
    }

    func testLogError_callsLoggerErrorMethod() {
        DdLogs.logError("Error message")

        XCTAssertEqual(mockLogger.logCalls.count, 1)
        XCTAssertEqual(mockLogger.logCalls[0].level, "error")
        XCTAssertEqual(mockLogger.logCalls[0].message, "Error message")
    }

    func testLogCritical_callsLoggerCriticalMethod() {
        DdLogs.logCritical("Critical message")

        XCTAssertEqual(mockLogger.logCalls.count, 1)
        XCTAssertEqual(mockLogger.logCalls[0].level, "critical")
        XCTAssertEqual(mockLogger.logCalls[0].message, "Critical message")
    }

    func testLogWithAttributes_debugLevel_callsLoggerDebugWithAttributes() {
        let attributes = ["user_id": "123", "action": "test"]

        DdLogs.logWithAttributes(level: "debug", message: "Debug with attrs", attributes: attributes)

        XCTAssertEqual(mockLogger.logCalls.count, 1)
        XCTAssertEqual(mockLogger.logCalls[0].level, "debug")
        XCTAssertEqual(mockLogger.logCalls[0].message, "Debug with attrs")
        XCTAssertNotNil(mockLogger.logCalls[0].attributes)
        XCTAssertEqual(mockLogger.logCalls[0].attributes?["user_id"] as? String, "123")
        XCTAssertEqual(mockLogger.logCalls[0].attributes?["action"] as? String, "test")
    }

    func testLogWithAttributes_infoLevel_callsLoggerInfoWithAttributes() {
        let attributes = ["key": "value"]

        DdLogs.logWithAttributes(level: "info", message: "Info with attrs", attributes: attributes)

        XCTAssertEqual(mockLogger.logCalls.count, 1)
        XCTAssertEqual(mockLogger.logCalls[0].level, "info")
        XCTAssertNotNil(mockLogger.logCalls[0].attributes)
    }

    func testLogWithAttributes_warnLevel_callsLoggerWarnWithAttributes() {
        let attributes = ["warning": "true"]

        DdLogs.logWithAttributes(level: "warn", message: "Warn with attrs", attributes: attributes)

        XCTAssertEqual(mockLogger.logCalls.count, 1)
        XCTAssertEqual(mockLogger.logCalls[0].level, "warn")
    }

    func testLogWithAttributes_warningLevel_callsLoggerWarnWithAttributes() {
        // "warning" should map to "warn"
        DdLogs.logWithAttributes(level: "warning", message: "Warning with attrs", attributes: [:])

        XCTAssertEqual(mockLogger.logCalls.count, 1)
        XCTAssertEqual(mockLogger.logCalls[0].level, "warn")
    }

    func testLogWithAttributes_errorLevel_callsLoggerErrorWithAttributes() {
        DdLogs.logWithAttributes(level: "error", message: "Error with attrs", attributes: [:])

        XCTAssertEqual(mockLogger.logCalls.count, 1)
        XCTAssertEqual(mockLogger.logCalls[0].level, "error")
    }

    func testLogWithAttributes_criticalLevel_callsLoggerCriticalWithAttributes() {
        DdLogs.logWithAttributes(level: "critical", message: "Critical with attrs", attributes: [:])

        XCTAssertEqual(mockLogger.logCalls.count, 1)
        XCTAssertEqual(mockLogger.logCalls[0].level, "critical")
    }

    func testLogWithAttributes_unknownLevel_defaultsToInfo() {
        DdLogs.logWithAttributes(level: "unknown", message: "Unknown level", attributes: [:])

        XCTAssertEqual(mockLogger.logCalls.count, 1)
        XCTAssertEqual(mockLogger.logCalls[0].level, "info")
    }

    func testMultipleLogCalls_allRecorded() {
        DdLogs.logDebug("First")
        DdLogs.logInfo("Second")
        DdLogs.logWarn("Third")

        XCTAssertEqual(mockLogger.logCalls.count, 3)
        XCTAssertEqual(mockLogger.logCalls[0].level, "debug")
        XCTAssertEqual(mockLogger.logCalls[1].level, "info")
        XCTAssertEqual(mockLogger.logCalls[2].level, "warn")
    }
}
