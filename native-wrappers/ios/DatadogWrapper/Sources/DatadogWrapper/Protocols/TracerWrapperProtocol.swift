import Foundation
import DatadogTrace

/// Protocol for Tracer functionality
/// Allows for dependency injection and testing
protocol TracerWrapperProtocol {
    /// Start a new span with the given operation name
    /// - Parameters:
    ///   - operationName: The name of the operation
    ///   - childOf: Optional parent span context for nesting
    ///   - tags: Tags to set on the span
    ///   - startTime: The start time of the span
    /// - Returns: A span wrapper
    func startSpan(operationName: String, childOf: SpanContextWrapperProtocol?, tags: [String: Encodable], startTime: Date) -> SpanWrapperProtocol
}

/// Protocol for Span context (used for parent-child relationships)
protocol SpanContextWrapperProtocol {}

/// Protocol for Span functionality
protocol SpanWrapperProtocol {
    /// The span's context (used to establish parent-child relationships)
    var spanContext: SpanContextWrapperProtocol { get }

    /// Set a tag on the span
    func setTag(key: String, value: Encodable)

    /// Mark this span as the active span in the execution context
    func setActive()

    /// Finish the span at the given time
    func finish(at time: Date)
}

/// Production wrapper for OTSpanContext
class RealSpanContextWrapper: SpanContextWrapperProtocol {
    let context: OTSpanContext

    init(context: OTSpanContext) {
        self.context = context
    }
}

/// Production implementation that wraps the real Datadog Tracer
class RealTracerWrapper: TracerWrapperProtocol {
    private let tracer: OTTracer

    init(tracer: OTTracer) {
        self.tracer = tracer
    }

    func startSpan(operationName: String, childOf: SpanContextWrapperProtocol?, tags: [String: Encodable], startTime: Date) -> SpanWrapperProtocol {
        let parentContext = (childOf as? RealSpanContextWrapper)?.context
        let span = tracer.startSpan(operationName: operationName, childOf: parentContext, tags: tags, startTime: startTime)
        return RealSpanWrapper(span: span)
    }
}

/// Production implementation that wraps a real OTSpan
class RealSpanWrapper: SpanWrapperProtocol {
    private let span: OTSpan

    var spanContext: SpanContextWrapperProtocol {
        RealSpanContextWrapper(context: span.context)
    }

    init(span: OTSpan) {
        self.span = span
    }

    func setTag(key: String, value: Encodable) {
        span.setTag(key: key, value: value)
    }

    func setActive() {
        span.setActive()
    }

    func finish(at time: Date) {
        span.finish(at: time)
    }
}
