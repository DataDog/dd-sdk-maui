/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

package com.datadog.wrapper

import android.util.Log
import com.datadog.android.log.Logger
import com.datadog.android.log.Logs
import com.datadog.android.log.LogsConfiguration
import io.mockk.*
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Before
import org.junit.Test

class DdLogsTest {
    private val mockLogger = mockk<Logger>(relaxed = true)
    private val mockLogsConfig = mockk<LogsConfiguration>(relaxed = true)
    private val mockLogsConfigBuilder = mockk<LogsConfiguration.Builder>(relaxed = true)

    @Before
    fun setUp() {
        // Reset static state so each test starts with no logger
        DdLogs.resetForTesting()

        mockkStatic(Logs::class)
        mockkConstructor(LogsConfiguration.Builder::class)
        mockkConstructor(Logger.Builder::class)

        every { anyConstructed<LogsConfiguration.Builder>().useCustomEndpoint(any()) } returns mockLogsConfigBuilder
        every { anyConstructed<LogsConfiguration.Builder>().build() } returns mockLogsConfig
        every { Logs.enable(any()) } returns Unit

        // Use `answers { self }` so the builder chain stays on the same constructed instance
        // and build() can reliably return mockLogger
        every { anyConstructed<Logger.Builder>().setNetworkInfoEnabled(any()) } answers { self as Logger.Builder }
        every { anyConstructed<Logger.Builder>().setLogcatLogsEnabled(any()) } answers { self as Logger.Builder }
        every { anyConstructed<Logger.Builder>().setBundleWithTraceEnabled(any()) } answers { self as Logger.Builder }
        every { anyConstructed<Logger.Builder>().setService(any()) } answers { self as Logger.Builder }
        every { anyConstructed<Logger.Builder>().build() } returns mockLogger
    }

    @After
    fun tearDown() {
        unmockkAll()
    }

    // ── enableLogs ────────────────────────────────────────────────────────────

    @Test
    fun `enableLogs with null endpoint calls enable without custom endpoint`() {
        DdLogs.enableLogs(customEndpoint = null)

        verify { Logs.enable(any()) }
        verify(exactly = 0) { anyConstructed<LogsConfiguration.Builder>().useCustomEndpoint(any()) }
    }

    @Test
    fun `enableLogs with valid custom endpoint calls useCustomEndpoint`() {
        val customEndpoint = "https://custom-logs.example.com/v1/input"

        DdLogs.enableLogs(customEndpoint = customEndpoint)

        verify { anyConstructed<LogsConfiguration.Builder>().useCustomEndpoint(customEndpoint) }
        verify { Logs.enable(any()) }
    }

    @Test
    fun `enableLogs with empty string falls back to default`() {
        DdLogs.enableLogs(customEndpoint = "")

        verify(exactly = 0) { anyConstructed<LogsConfiguration.Builder>().useCustomEndpoint(any()) }
        verify { Logs.enable(any()) }
    }

    @Test
    fun `enableLogs with whitespace only string falls back to default`() {
        DdLogs.enableLogs(customEndpoint = "   ")

        verify(exactly = 0) { anyConstructed<LogsConfiguration.Builder>().useCustomEndpoint(any()) }
        verify { Logs.enable(any()) }
    }

    @Test
    fun `enableLogs with valid https endpoint succeeds`() {
        DdLogs.enableLogs(customEndpoint = "https://logs.example.com/api")

        verify { anyConstructed<LogsConfiguration.Builder>().useCustomEndpoint("https://logs.example.com/api") }
        verify { Logs.enable(any()) }
    }

    @Test
    fun `enableLogs with valid http endpoint succeeds`() {
        DdLogs.enableLogs(customEndpoint = "http://localhost:8080/logs")

        verify { anyConstructed<LogsConfiguration.Builder>().useCustomEndpoint("http://localhost:8080/logs") }
        verify { Logs.enable(any()) }
    }

    @Test
    fun `enableLogs without service name does not set service on logger`() {
        // DatadogWrapper is never initialized in this test, so getServiceName() returns null
        DdLogs.enableLogs(customEndpoint = null)

        verify(exactly = 0) { anyConstructed<Logger.Builder>().setService(any()) }
    }

    // ── Individual log level methods ──────────────────────────────────────────

    @Test
    fun `logDebug calls logger d method`() {
        DdLogs.enableLogs(customEndpoint = null)

        DdLogs.logDebug("Debug message")

        verify { mockLogger.d("Debug message") }
    }

    @Test
    fun `logInfo calls logger i method`() {
        DdLogs.enableLogs(customEndpoint = null)

        DdLogs.logInfo("Info message")

        verify { mockLogger.i("Info message") }
    }

    @Test
    fun `logWarn calls logger w method`() {
        DdLogs.enableLogs(customEndpoint = null)

        DdLogs.logWarn("Warn message")

        verify { mockLogger.w("Warn message") }
    }

    @Test
    fun `logError calls logger e method`() {
        DdLogs.enableLogs(customEndpoint = null)

        DdLogs.logError("Error message")

        verify { mockLogger.e("Error message") }
    }

    // ── logWithAttributes ─────────────────────────────────────────────────────

    @Test
    fun `logWithAttributes debug level calls logger d with attributes`() {
        DdLogs.enableLogs(customEndpoint = null)
        val attributes = mapOf<String, Any?>("user_id" to "123", "action" to "test")
        val messageSlot = slot<String>()
        val attributesSlot = slot<Map<String, Any?>>()

        DdLogs.logWithAttributes(level = "debug", message = "Debug with attrs", attributes = attributes)

        verify { mockLogger.d(capture(messageSlot), attributes = capture(attributesSlot)) }
        assertEquals("Debug with attrs", messageSlot.captured)
        assertEquals("123", attributesSlot.captured["user_id"])
        assertEquals("test", attributesSlot.captured["action"])
    }

    @Test
    fun `logWithAttributes info level calls logger i with attributes`() {
        DdLogs.enableLogs(customEndpoint = null)

        DdLogs.logWithAttributes(level = "info", message = "Info with attrs", attributes = mapOf<String, Any?>("key" to "value"))

        verify { mockLogger.i(eq("Info with attrs"), attributes = any()) }
    }

    @Test
    fun `logWithAttributes warn level calls logger w with attributes`() {
        DdLogs.enableLogs(customEndpoint = null)

        DdLogs.logWithAttributes(level = "warn", message = "Warn with attrs", attributes = mapOf<String, Any?>("warning" to "true"))

        verify { mockLogger.w(eq("Warn with attrs"), attributes = any()) }
    }

    @Test
    fun `logWithAttributes warning alias calls logger w with attributes`() {
        // "warning" is an accepted alias for "warn"
        DdLogs.enableLogs(customEndpoint = null)

        DdLogs.logWithAttributes(level = "warning", message = "Warning with attrs", attributes = emptyMap())

        verify { mockLogger.w(eq("Warning with attrs"), attributes = any()) }
    }

    @Test
    fun `logWithAttributes error level calls logger e with attributes`() {
        DdLogs.enableLogs(customEndpoint = null)

        DdLogs.logWithAttributes(level = "error", message = "Error with attrs", attributes = emptyMap())

        verify { mockLogger.e(eq("Error with attrs"), attributes = any()) }
    }

    @Test
    fun `logWithAttributes critical level defaults to info`() {
        // Android Logger has no critical level — falls through to the else branch (info)
        DdLogs.enableLogs(customEndpoint = null)

        DdLogs.logWithAttributes(level = "critical", message = "Critical with attrs", attributes = emptyMap())

        verify { mockLogger.i(eq("Critical with attrs"), attributes = any()) }
    }

    @Test
    fun `logWithAttributes unknown level defaults to info`() {
        DdLogs.enableLogs(customEndpoint = null)

        DdLogs.logWithAttributes(level = "unknown", message = "Unknown level", attributes = emptyMap())

        verify { mockLogger.i(eq("Unknown level"), attributes = any()) }
    }

    // ── Multiple calls ────────────────────────────────────────────────────────

    @Test
    fun `multiple log calls all reach the logger`() {
        DdLogs.enableLogs(customEndpoint = null)

        DdLogs.logDebug("First")
        DdLogs.logInfo("Second")
        DdLogs.logWarn("Third")

        verify { mockLogger.d("First") }
        verify { mockLogger.i("Second") }
        verify { mockLogger.w("Third") }
    }

    // ── Lazy initialization ───────────────────────────────────────────────────

    @Test
    fun `logDebug before enableLogs logs warning and drops log`() {
        mockkStatic(Log::class)
        every { Log.w(any(), any<String>()) } returns 0

        DdLogs.logDebug("Should be dropped")

        // Verify the warning was emitted
        verify { Log.w("DatadogWrapper", "DdLogs.enableLogs() must be called before logging. Log will be dropped.") }
        // Logs module must NOT have been enabled lazily
        verify(exactly = 0) { Logs.enable(any()) }
        // The message must NOT have been forwarded to the logger
        verify(exactly = 0) { mockLogger.d(any()) }
    }
}