/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

import Foundation
import DatadogCore
import DatadogTrace

@objc(DdTrace)
public class DdTrace: NSObject {

    // Lock object for thread-safe access to shared state
    private static let lock = NSObject()

    // Dependencies - injectable for testing
    private static var traceModule: TraceModuleProtocol = RealTraceModule()
    private static var tracer: TracerWrapperProtocol?

    // Active spans tracked by ID
    private static var activeSpans: [String: SpanWrapperProtocol] = [:]
    // Span stack for parent-child nesting (LIFO)
    private static var spanStack: [String] = []

    /// The currently active span (top of the stack)
    private static var activeSpan: SpanWrapperProtocol? {
        spanStack.last.flatMap { activeSpans[$0] }
    }

    // For testing: inject dependencies
    static func setTraceModule(_ module: TraceModuleProtocol) {
        traceModule = module
    }

    static func setTracer(_ tracerWrapper: TracerWrapperProtocol?) {
        tracer = tracerWrapper
    }

    // Reset to production dependencies (for test cleanup)
    static func resetDependencies() {
        traceModule = RealTraceModule()
        tracer = nil
        activeSpans = [:]
        spanStack = []
    }

    /// Enable the Trace module with optional configuration
    /// - Parameter customEndpoint: Optional custom server URL for sending traces
    /// Must be called after DatadogWrapper.initialize()
    @objc(enableTrace:)
    public static func enableTrace(customEndpoint: String?) {
        if let customEndpoint = customEndpoint,
           !customEndpoint.isEmpty,
           let url = URL(string: customEndpoint) {
            // Enable Trace with custom configuration
            let config = Trace.Configuration(customEndpoint: url)
            traceModule.enable(with: config)
        } else {
            // Enable Trace with default configuration
            traceModule.enable()
        }

        tracer = RealTracerWrapper(tracer: Tracer.shared())
    }

    /// Start a new span with the given operation name
    /// - Parameters:
    ///   - operation: The name of the operation being traced
    ///   - context: Additional context attributes for the span
    ///   - timestampMs: The start timestamp in milliseconds since epoch
    /// - Returns: A unique span ID string
    @objc public static func startSpan(
        _ operation: String,
        context: NSDictionary,
        timestampMs: Int64
    ) -> String {
        objc_sync_enter(lock)
        defer { objc_sync_exit(lock) }

        ensureTracer()

        let spanId = UUID().uuidString
        let startDate = Date(timeIntervalSince1970: TimeInterval(timestampMs) / 1_000)
        var encodableContext: [String: Encodable] = [:]
        if let dict = context as? [String: Any] {
            for (key, value) in dict {
                switch value {
                case let boolVal as Bool:
                    encodableContext[key] = boolVal
                case let intVal as Int:
                    encodableContext[key] = intVal
                case let doubleVal as Double:
                    encodableContext[key] = doubleVal
                case let stringVal as String:
                    encodableContext[key] = stringVal
                case let int64Val as Int64:
                    encodableContext[key] = int64Val
                default:
                    encodableContext[key] = String(describing: value)
                }
            }
        }

        if let span = tracer?.startSpan(
            operationName: operation,
            childOf: activeSpan?.spanContext,
            tags: encodableContext,
            startTime: startDate
        ) {
            span.setActive()
            activeSpans[spanId] = span
            spanStack.append(spanId)
        }

        return spanId
    }

    /// Finish a previously started span
    /// - Parameters:
    ///   - spanId: The unique identifier of the span returned by startSpan
    ///   - context: Additional context attributes to add before finishing
    ///   - timestampMs: The finish timestamp in milliseconds since epoch
    @objc public static func finishSpan(
        _ spanId: String,
        context: NSDictionary,
        timestampMs: Int64
    ) {
        objc_sync_enter(lock)
        defer { objc_sync_exit(lock) }

        ensureTracer()

        guard let span = activeSpans.removeValue(forKey: spanId) else {
            print("[Datadog] DdTrace.finishSpan: No active span found for id \(spanId).")
            return
        }

        // Set additional context tags
        if let dict = context as? [String: Any] {
            for (key, value) in dict {
                span.setTag(key: key, value: String(describing: value))
            }
        }

        let finishDate = Date(timeIntervalSince1970: TimeInterval(timestampMs) / 1_000)
        span.finish(at: finishDate)

        // Remove from stack and re-activate previous span if this was the top
        if let idx = spanStack.lastIndex(of: spanId) {
            let wasTop = (idx == spanStack.count - 1)
            spanStack.remove(at: idx)

            if wasTop, let prev = activeSpan {
                prev.setActive()
            }
        }
    }

    /// Guard that a tracer exists before forwarding a trace call.
    private static func ensureTracer() {
        if tracer == nil {
            print("[Datadog] DdTrace.enable() must be called before tracing. Call will be dropped.")
        }
    }
}