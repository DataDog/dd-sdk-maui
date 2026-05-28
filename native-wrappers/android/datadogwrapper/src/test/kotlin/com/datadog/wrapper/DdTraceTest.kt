/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

package com.datadog.wrapper

import android.util.Log
import com.datadog.android.Datadog
import com.datadog.android.trace.DatadogTracing
import com.datadog.android.trace.GlobalDatadogTracer
import com.datadog.android.trace.Trace
import com.datadog.android.trace.TraceConfiguration
import com.datadog.android.trace.api.scope.DatadogScope
import com.datadog.android.trace.api.span.DatadogSpan
import com.datadog.android.trace.api.span.DatadogSpanBuilder
import com.datadog.android.trace.api.span.DatadogSpanContext
import com.datadog.android.trace.api.tracer.DatadogTracer
import io.mockk.every
import io.mockk.mockk
import io.mockk.mockkConstructor
import io.mockk.mockkStatic
import io.mockk.unmockkAll
import io.mockk.verify
import io.mockk.verifyOrder
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotEquals
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import java.util.concurrent.TimeUnit

class DdTraceTest {
    private val mockTracer = mockk<DatadogTracer>(relaxed = true)
    private val mockSpan = mockk<DatadogSpan>(relaxed = true)
    private val mockSpanBuilder = mockk<DatadogSpanBuilder>(relaxed = true)
    private val mockSpanContext = mockk<DatadogSpanContext>(relaxed = true)
    private val mockScope = mockk<DatadogScope>(relaxed = true)
    private val mockTraceConfig = mockk<TraceConfiguration>(relaxed = true)
    private val mockTraceConfigBuilder =
        mockk<TraceConfiguration.Builder>(relaxed = true)
    private val mockDatadogInstance =
        mockk<com.datadog.android.api.SdkCore>(relaxed = true)

    @Before
    fun setUp() {
        // Reset static state so each test starts clean
        DdTrace.resetForTesting()

        mockkStatic(Trace::class)
        mockkStatic(Datadog::class)
        mockkStatic(DatadogTracing::class)
        mockkStatic(GlobalDatadogTracer::class)
        mockkConstructor(TraceConfiguration.Builder::class)

        every { Datadog.getInstance() } returns mockDatadogInstance
        every {
            anyConstructed<TraceConfiguration.Builder>()
                .useCustomEndpoint(any())
        } returns mockTraceConfigBuilder
        every {
            anyConstructed<TraceConfiguration.Builder>().build()
        } returns mockTraceConfig
        every { Trace.enable(any(), any()) } returns Unit

        every {
            DatadogTracing.newTracerBuilder(any())
        } returns mockk(relaxed = true) {
            every { build() } returns mockTracer
        }
        every {
            GlobalDatadogTracer.registerIfAbsent(any())
        } returns true
        every { GlobalDatadogTracer.get() } returns mockTracer

        every { mockTracer.buildSpan(any()) } returns mockSpanBuilder
        every {
            mockSpanBuilder.withStartTimestamp(any())
        } returns mockSpanBuilder
        every { mockSpanBuilder.start() } returns mockSpan
        every { mockSpan.context() } returns mockSpanContext
        every { mockSpanContext.spanId } returns 123456L
        every { mockTracer.activateSpan(any()) } returns mockScope
    }

    @After
    fun tearDown() {
        unmockkAll()
    }

    // ── enableTrace ──────────────────────────────────────────

    @Test
    fun `enableTrace with null endpoint enables without custom endpoint`() {
        DdTrace.enableTrace(customEndpoint = null)

        verify { Trace.enable(any(), any()) }
        verify(exactly = 0) {
            anyConstructed<TraceConfiguration.Builder>()
                .useCustomEndpoint(any())
        }
    }

    @Test
    fun `enableTrace with valid custom endpoint calls useCustomEndpoint`() {
        val customEndpoint =
            "https://custom-trace.example.com/v1/input"

        DdTrace.enableTrace(customEndpoint = customEndpoint)

        verify {
            anyConstructed<TraceConfiguration.Builder>()
                .useCustomEndpoint(customEndpoint)
        }
        verify { Trace.enable(any(), any()) }
    }

    @Test
    fun `enableTrace with empty string falls back to default`() {
        DdTrace.enableTrace(customEndpoint = "")

        verify(exactly = 0) {
            anyConstructed<TraceConfiguration.Builder>()
                .useCustomEndpoint(any())
        }
        verify { Trace.enable(any(), any()) }
    }

    @Test
    fun `enableTrace with whitespace only string falls back to default`() {
        DdTrace.enableTrace(customEndpoint = "   ")

        verify(exactly = 0) {
            anyConstructed<TraceConfiguration.Builder>()
                .useCustomEndpoint(any())
        }
        verify { Trace.enable(any(), any()) }
    }

    // ── startSpan ────────────────────────────────────────────

    @Test
    fun `startSpan returns the SDK span ID`() {
        DdTrace.enableTrace(customEndpoint = null)

        val spanId = DdTrace.startSpan(
            "test.operation", emptyMap(), 1000L
        )

        assertEquals("123456", spanId)
    }

    @Test
    fun `startSpan calls buildSpan with correct operation name`() {
        DdTrace.enableTrace(customEndpoint = null)

        DdTrace.startSpan("test.operation", emptyMap(), 1000L)

        verify { mockTracer.buildSpan("test.operation") }
    }

    @Test
    fun `startSpan sets start timestamp in microseconds`() {
        DdTrace.enableTrace(customEndpoint = null)
        val timestampMs = 1700000000000L

        DdTrace.startSpan("test.operation", emptyMap(), timestampMs)

        val expectedMicros =
            TimeUnit.MILLISECONDS.toMicros(timestampMs)
        verify {
            mockSpanBuilder.withStartTimestamp(expectedMicros)
        }
    }

    @Test
    fun `startSpan sets context tags on the span`() {
        DdTrace.enableTrace(customEndpoint = null)
        val context = mapOf<String, Any?>("user_id" to "123", "action" to "test")

        DdTrace.startSpan("test.operation", context, 1000L)

        verify { mockSpan.setTag(eq("user_id"), any<String>() as Any?) }
        verify { mockSpan.setTag(eq("action"), any<String>() as Any?) }
    }

    @Test
    fun `startSpan returns unique IDs for different spans`() {
        DdTrace.enableTrace(customEndpoint = null)
        val mockSpan2 = mockk<DatadogSpan>(relaxed = true)
        val mockSpanContext2 = mockk<DatadogSpanContext>(relaxed = true)
        every { mockSpanContext2.spanId } returns 789012L
        every { mockSpan2.context() } returns mockSpanContext2
        every {
            mockSpanBuilder.start()
        } returnsMany listOf(mockSpan, mockSpan2)

        val spanId1 = DdTrace.startSpan("op1", emptyMap(), 1000L)
        val spanId2 = DdTrace.startSpan("op2", emptyMap(), 2000L)

        assertEquals("123456", spanId1)
        assertEquals("789012", spanId2)
        assertNotEquals(spanId1, spanId2)
    }

    @Test
    fun `startSpan activates the span for nesting`() {
        DdTrace.enableTrace(customEndpoint = null)

        DdTrace.startSpan("test.operation", emptyMap(), 1000L)

        verify { mockTracer.activateSpan(mockSpan) }
    }

    // ── finishSpan ───────────────────────────────────────────

    @Test
    fun `finishSpan calls finish on the correct span`() {
        DdTrace.enableTrace(customEndpoint = null)

        val spanId = DdTrace.startSpan(
            "test.operation", emptyMap(), 1000L
        )
        DdTrace.finishSpan(spanId, emptyMap(), 2000L)

        val expectedMicros =
            TimeUnit.MILLISECONDS.toMicros(2000L)
        verify { mockSpan.finish(expectedMicros) }
    }

    @Test
    fun `finishSpan closes the scope before finishing the span`() {
        DdTrace.enableTrace(customEndpoint = null)

        val spanId = DdTrace.startSpan(
            "test.operation", emptyMap(), 1000L
        )
        DdTrace.finishSpan(spanId, emptyMap(), 2000L)

        verifyOrder {
            mockScope.close()
            mockSpan.finish(any())
        }
    }

    @Test
    fun `finishSpan sets context tags before finishing`() {
        DdTrace.enableTrace(customEndpoint = null)
        val finishContext = mapOf<String, Any?>("status" to "completed")

        val spanId = DdTrace.startSpan(
            "test.operation", emptyMap(), 1000L
        )
        DdTrace.finishSpan(spanId, finishContext, 2000L)

        verify { mockSpan.setTag(eq("status"), any<String>() as Any?) }
        verify { mockSpan.finish(any()) }
    }

    @Test
    fun `finishSpan with invalid ID logs warning and does not crash`() {
        DdTrace.enableTrace(customEndpoint = null)
        mockkStatic(Log::class)
        every { Log.w(any(), any<String>()) } returns 0

        DdTrace.finishSpan("non-existent-id", emptyMap(), 2000L)

        verify {
            Log.w(
                "DatadogWrapper",
                match<String> { it.contains("No active span found") }
            )
        }
    }

    @Test
    fun `finishSpan removes span from active spans`() {
        DdTrace.enableTrace(customEndpoint = null)

        val spanId = DdTrace.startSpan(
            "test.operation", emptyMap(), 1000L
        )
        DdTrace.finishSpan(spanId, emptyMap(), 2000L)

        // Second finish should not call finish again on the span
        mockkStatic(Log::class)
        every { Log.w(any(), any<String>()) } returns 0
        DdTrace.finishSpan(spanId, emptyMap(), 3000L)

        verify(exactly = 1) { mockSpan.finish(any()) }
    }

    // ── Before enable ────────────────────────────────────────

    @Test
    fun `startSpan before enableTrace returns empty string and logs warning`() {
        mockkStatic(Log::class)
        every { Log.w(any(), any<String>()) } returns 0

        val spanId = DdTrace.startSpan(
            "test.operation", emptyMap(), 1000L
        )

        assertEquals("", spanId)
        verify {
            Log.w(
                "DatadogWrapper",
                match<String> {
                    it.contains("must be called before tracing")
                }
            )
        }
        verify(exactly = 0) { Trace.enable(any(), any()) }
    }

    @Test
    fun `finishSpan before enableTrace logs warning`() {
        mockkStatic(Log::class)
        every { Log.w(any(), any<String>()) } returns 0

        DdTrace.finishSpan("some-id", emptyMap(), 2000L)

        verify {
            Log.w(
                "DatadogWrapper",
                match<String> {
                    it.contains("must be called before tracing")
                }
            )
        }
    }

    // ── Multiple spans / nesting ─────────────────────────────

    @Test
    fun `multiple spans can be active simultaneously`() {
        DdTrace.enableTrace(customEndpoint = null)
        val mockSpan2 = mockk<DatadogSpan>(relaxed = true)
        val mockSpanContext2 = mockk<DatadogSpanContext>(relaxed = true)
        val mockScope2 = mockk<DatadogScope>(relaxed = true)

        every { mockSpanContext2.spanId } returns 789012L
        every { mockSpan2.context() } returns mockSpanContext2
        every {
            mockSpanBuilder.start()
        } returnsMany listOf(mockSpan, mockSpan2)
        every {
            mockTracer.activateSpan(mockSpan)
        } returns mockScope
        every {
            mockTracer.activateSpan(mockSpan2)
        } returns mockScope2

        val spanId1 = DdTrace.startSpan("op1", emptyMap(), 1000L)
        val spanId2 = DdTrace.startSpan("op2", emptyMap(), 1500L)

        DdTrace.finishSpan(spanId2, emptyMap(), 2000L)
        DdTrace.finishSpan(spanId1, emptyMap(), 2500L)

        verify { mockScope2.close() }
        verify { mockScope.close() }
        val micros2000 = TimeUnit.MILLISECONDS.toMicros(2000L)
        val micros2500 = TimeUnit.MILLISECONDS.toMicros(2500L)
        verify { mockSpan2.finish(micros2000) }
        verify { mockSpan.finish(micros2500) }
    }
}
