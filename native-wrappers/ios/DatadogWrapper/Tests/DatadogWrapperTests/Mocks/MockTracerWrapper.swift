/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

import Foundation
@testable import DatadogWrapper

class MockSpanContextWrapper: SpanContextWrapperProtocol {
    let id: String

    init(id: String = UUID().uuidString) {
        self.id = id
    }
}

class MockSpanWrapper: SpanWrapperProtocol {
    struct TagCall {
        let key: String
        let value: Encodable
    }

    let mockContext: MockSpanContextWrapper

    var spanContext: SpanContextWrapperProtocol { mockContext }

    var tagCalls: [TagCall] = []
    var finishTime: Date?
    var setActiveCalled = false

    init(contextId: String = UUID().uuidString) {
        self.mockContext = MockSpanContextWrapper(id: contextId)
    }

    func setTag(key: String, value: Encodable) {
        tagCalls.append(TagCall(key: key, value: value))
    }

    func setActive() {
        setActiveCalled = true
    }

    func finish(at time: Date) {
        finishTime = time
    }
}

class MockTracerWrapper: TracerWrapperProtocol {
    struct StartSpanCall {
        let operationName: String
        let childOf: SpanContextWrapperProtocol?
        let tags: [String: Encodable]
        let startTime: Date
    }

    var startSpanCalls: [StartSpanCall] = []
    var spansToReturn: [MockSpanWrapper] = []
    private var spanIndex = 0

    func startSpan(operationName: String, childOf: SpanContextWrapperProtocol?, tags: [String: Encodable], startTime: Date) -> SpanWrapperProtocol {
        startSpanCalls.append(StartSpanCall(operationName: operationName, childOf: childOf, tags: tags, startTime: startTime))
        let span: MockSpanWrapper
        if spanIndex < spansToReturn.count {
            span = spansToReturn[spanIndex]
            spanIndex += 1
        } else {
            span = MockSpanWrapper()
            spansToReturn.append(span)
        }
        return span
    }

    func reset() {
        startSpanCalls.removeAll()
        spansToReturn.removeAll()
        spanIndex = 0
    }
}
