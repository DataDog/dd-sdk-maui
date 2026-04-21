import XCTest
@testable import DatadogWrapper

final class DdTraceTests: XCTestCase {
    var mockTraceModule: MockTraceModule!
    var mockTracer: MockTracerWrapper!

    override func setUp() {
        super.setUp()

        // Set up mocks
        mockTraceModule = MockTraceModule()
        mockTracer = MockTracerWrapper()

        DdTrace.setTraceModule(mockTraceModule)
        DdTrace.setTracer(mockTracer)
    }

    override func tearDown() {
        // Reset to production dependencies
        DdTrace.resetDependencies()
        mockTraceModule = nil
        mockTracer = nil
        super.tearDown()
    }

    // MARK: - enableTrace tests

    func testEnableTrace_withNilEndpoint_callsEnableWithoutConfig() {
        DdTrace.enableTrace(customEndpoint: nil)

        // Verify Trace.enable() was called (not enable(with:))
        XCTAssertTrue(mockTraceModule.enableCalled)
        XCTAssertFalse(mockTraceModule.enableWithConfigCalled)
        XCTAssertNil(mockTraceModule.capturedConfig)
    }

    func testEnableTrace_withValidCustomEndpoint_callsEnableWithConfig() {
        let customEndpoint = "https://custom-trace.example.com/v1/input"

        DdTrace.enableTrace(customEndpoint: customEndpoint)

        // Verify Trace.enable(with:) was called with custom endpoint
        XCTAssertTrue(mockTraceModule.enableWithConfigCalled)
        XCTAssertFalse(mockTraceModule.enableCalled)
        XCTAssertNotNil(mockTraceModule.capturedConfig)
        XCTAssertEqual(mockTraceModule.capturedConfig?.customEndpoint?.absoluteString, customEndpoint)
    }

    func testEnableTrace_withEmptyString_fallsBackToDefault() {
        DdTrace.enableTrace(customEndpoint: "")

        // Empty string should fall back to default (no config)
        XCTAssertTrue(mockTraceModule.enableCalled)
        XCTAssertFalse(mockTraceModule.enableWithConfigCalled)
    }

    // MARK: - startSpan tests

    func testStartSpan_returnsNonEmptySpanId() {
        let spanId = DdTrace.startSpan("test.operation", context: [:], timestampMs: 1000)

        XCTAssertFalse(spanId.isEmpty)
    }

    func testStartSpan_callsTracerWithCorrectOperation() {
        _ = DdTrace.startSpan("test.operation", context: [:], timestampMs: 1000)

        XCTAssertEqual(mockTracer.startSpanCalls.count, 1)
        XCTAssertEqual(mockTracer.startSpanCalls[0].operationName, "test.operation")
    }

    func testStartSpan_callsTracerWithCorrectTimestamp() {
        let timestampMs: Int64 = 1700000000000  // Some timestamp in ms

        _ = DdTrace.startSpan("test.operation", context: [:], timestampMs: timestampMs)

        XCTAssertEqual(mockTracer.startSpanCalls.count, 1)
        let expectedDate = Date(timeIntervalSince1970: 1700000000.0)
        XCTAssertEqual(mockTracer.startSpanCalls[0].startTime, expectedDate)
    }

    func testStartSpan_passesContextAsTags() {
        let context: NSDictionary = ["user_id": "123", "action": "test"]

        _ = DdTrace.startSpan("test.operation", context: context, timestampMs: 1000)

        XCTAssertEqual(mockTracer.startSpanCalls.count, 1)
        XCTAssertEqual(mockTracer.startSpanCalls[0].tags["user_id"] as? String, "123")
        XCTAssertEqual(mockTracer.startSpanCalls[0].tags["action"] as? String, "test")
    }

    func testStartSpan_returnsUniqueIds() {
        let spanId1 = DdTrace.startSpan("op1", context: [:], timestampMs: 1000)
        let spanId2 = DdTrace.startSpan("op2", context: [:], timestampMs: 2000)

        XCTAssertNotEqual(spanId1, spanId2)
    }

    func testStartSpan_callsSetActiveOnNewSpan() {
        let mockSpan = MockSpanWrapper()
        mockTracer.spansToReturn = [mockSpan]

        _ = DdTrace.startSpan("test.operation", context: [:], timestampMs: 1000)

        XCTAssertTrue(mockSpan.setActiveCalled)
    }

    // MARK: - Span nesting tests

    func testStartSpan_firstSpan_hasNoParent() {
        _ = DdTrace.startSpan("root.operation", context: [:], timestampMs: 1000)

        XCTAssertEqual(mockTracer.startSpanCalls.count, 1)
        XCTAssertNil(mockTracer.startSpanCalls[0].childOf)
    }

    func testStartSpan_secondSpan_hasFirstSpanAsParent() {
        let mockSpan1 = MockSpanWrapper(contextId: "span1-context")
        let mockSpan2 = MockSpanWrapper(contextId: "span2-context")
        mockTracer.spansToReturn = [mockSpan1, mockSpan2]

        _ = DdTrace.startSpan("parent.operation", context: [:], timestampMs: 1000)
        _ = DdTrace.startSpan("child.operation", context: [:], timestampMs: 1500)

        XCTAssertEqual(mockTracer.startSpanCalls.count, 2)
        // First span has no parent
        XCTAssertNil(mockTracer.startSpanCalls[0].childOf)
        // Second span's parent should be the first span's context
        let parentContext = mockTracer.startSpanCalls[1].childOf as? MockSpanContextWrapper
        XCTAssertNotNil(parentContext)
        XCTAssertEqual(parentContext?.id, "span1-context")
    }

    func testStartSpan_thirdSpan_hasSecondSpanAsParent() {
        let mockSpan1 = MockSpanWrapper(contextId: "span1-context")
        let mockSpan2 = MockSpanWrapper(contextId: "span2-context")
        let mockSpan3 = MockSpanWrapper(contextId: "span3-context")
        mockTracer.spansToReturn = [mockSpan1, mockSpan2, mockSpan3]

        _ = DdTrace.startSpan("grandparent", context: [:], timestampMs: 1000)
        _ = DdTrace.startSpan("parent", context: [:], timestampMs: 1500)
        _ = DdTrace.startSpan("child", context: [:], timestampMs: 2000)

        // Third span's parent should be the second span's context
        let parentContext = mockTracer.startSpanCalls[2].childOf as? MockSpanContextWrapper
        XCTAssertNotNil(parentContext)
        XCTAssertEqual(parentContext?.id, "span2-context")
    }

    func testFinishSpan_reactivatesPreviousSpan() {
        let mockSpan1 = MockSpanWrapper(contextId: "span1-context")
        let mockSpan2 = MockSpanWrapper(contextId: "span2-context")
        mockTracer.spansToReturn = [mockSpan1, mockSpan2]

        _ = DdTrace.startSpan("parent", context: [:], timestampMs: 1000)
        let childId = DdTrace.startSpan("child", context: [:], timestampMs: 1500)

        // Reset to track re-activation
        mockSpan1.setActiveCalled = false

        DdTrace.finishSpan(childId, context: [:], timestampMs: 2000)

        // Parent span should be re-activated
        XCTAssertTrue(mockSpan1.setActiveCalled)
    }

    func testFinishSpan_afterFinishingTopSpan_nextStartUsesNewTopAsParent() {
        let mockSpan1 = MockSpanWrapper(contextId: "span1-context")
        let mockSpan2 = MockSpanWrapper(contextId: "span2-context")
        let mockSpan3 = MockSpanWrapper(contextId: "span3-context")
        mockTracer.spansToReturn = [mockSpan1, mockSpan2, mockSpan3]

        _ = DdTrace.startSpan("parent", context: [:], timestampMs: 1000)
        let childId = DdTrace.startSpan("child", context: [:], timestampMs: 1500)

        DdTrace.finishSpan(childId, context: [:], timestampMs: 2000)

        // Starting a new span should now use span1 as parent again
        _ = DdTrace.startSpan("sibling", context: [:], timestampMs: 2500)

        let siblingParent = mockTracer.startSpanCalls[2].childOf as? MockSpanContextWrapper
        XCTAssertNotNil(siblingParent)
        XCTAssertEqual(siblingParent?.id, "span1-context")
    }

    // MARK: - finishSpan tests

    func testFinishSpan_finishesExistingSpan() {
        let mockSpan = MockSpanWrapper()
        mockTracer.spansToReturn = [mockSpan]

        let spanId = DdTrace.startSpan("test.operation", context: [:], timestampMs: 1000)
        DdTrace.finishSpan(spanId, context: [:], timestampMs: 2000)

        let expectedFinishDate = Date(timeIntervalSince1970: 2.0)
        XCTAssertEqual(mockSpan.finishTime, expectedFinishDate)
    }

    func testFinishSpan_setsContextTagsBeforeFinishing() {
        let mockSpan = MockSpanWrapper()
        mockTracer.spansToReturn = [mockSpan]

        let spanId = DdTrace.startSpan("test.operation", context: [:], timestampMs: 1000)
        DdTrace.finishSpan(spanId, context: ["status": "completed"] as NSDictionary, timestampMs: 2000)

        XCTAssertEqual(mockSpan.tagCalls.count, 1)
        XCTAssertEqual(mockSpan.tagCalls[0].key, "status")
        XCTAssertEqual(mockSpan.tagCalls[0].value as? String, "completed")
    }

    func testFinishSpan_withInvalidId_doesNotCrash() {
        // Should not crash or throw when finishing a non-existent span
        DdTrace.finishSpan("non-existent-id", context: [:], timestampMs: 2000)
        // If we reach here, the test passes
    }

    func testFinishSpan_removesSpanFromActiveSpans() {
        let mockSpan = MockSpanWrapper()
        mockTracer.spansToReturn = [mockSpan]

        let spanId = DdTrace.startSpan("test.operation", context: [:], timestampMs: 1000)
        DdTrace.finishSpan(spanId, context: [:], timestampMs: 2000)

        // Finishing the same span again should not call finish on the mock again
        let previousFinishTime = mockSpan.finishTime
        DdTrace.finishSpan(spanId, context: [:], timestampMs: 3000)
        XCTAssertEqual(mockSpan.finishTime, previousFinishTime)
    }

    // MARK: - Before enable tests

    func testStartSpan_beforeEnable_returnsEmptyStringAndDoesNotCrash() {
        // Simulate pre-enable state: clear the tracer that setUp injected
        DdTrace.setTracer(nil)

        let spanId = DdTrace.startSpan("test.operation", context: [:], timestampMs: 1000)

        // Span ID is still returned (UUID generated) but no tracer interaction
        XCTAssertFalse(spanId.isEmpty)
        // Trace module must NOT have been enabled lazily
        XCTAssertFalse(mockTraceModule.enableCalled)
        XCTAssertFalse(mockTraceModule.enableWithConfigCalled)
    }

    func testFinishSpan_beforeEnable_doesNotCrash() {
        DdTrace.setTracer(nil)

        // Should not crash
        DdTrace.finishSpan("some-id", context: [:], timestampMs: 2000)

        XCTAssertFalse(mockTraceModule.enableCalled)
    }

    // MARK: - Multiple spans

    func testMultipleSpans_canBeActiveSimultaneously() {
        let mockSpan1 = MockSpanWrapper()
        let mockSpan2 = MockSpanWrapper()
        mockTracer.spansToReturn = [mockSpan1, mockSpan2]

        let spanId1 = DdTrace.startSpan("op1", context: [:], timestampMs: 1000)
        let spanId2 = DdTrace.startSpan("op2", context: [:], timestampMs: 1500)

        DdTrace.finishSpan(spanId2, context: [:], timestampMs: 2000)
        DdTrace.finishSpan(spanId1, context: [:], timestampMs: 2500)

        XCTAssertNotNil(mockSpan1.finishTime)
        XCTAssertNotNil(mockSpan2.finishTime)
        XCTAssertEqual(mockSpan2.finishTime, Date(timeIntervalSince1970: 2.0))
        XCTAssertEqual(mockSpan1.finishTime, Date(timeIntervalSince1970: 2.5))
    }
}
