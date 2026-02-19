package com.datadog.wrapper

import com.datadog.android.log.Logger
import com.datadog.android.log.Logs
import com.datadog.android.log.LogsConfiguration
import io.mockk.*
import org.junit.After
import org.junit.Before
import org.junit.Test

class DdLogsTest {
    private val mockLogger = mockk<Logger>(relaxed = true)
    private val mockLogsConfig = mockk<LogsConfiguration>(relaxed = true)
    private val mockLogsConfigBuilder = mockk<LogsConfiguration.Builder>(relaxed = true)
    private val mockLoggerBuilder = mockk<Logger.Builder>(relaxed = true)

    @Before
    fun setUp() {
        mockkStatic(Logs::class)
        mockkConstructor(LogsConfiguration.Builder::class)
        mockkConstructor(Logger.Builder::class)

        every { anyConstructed<LogsConfiguration.Builder>().useCustomEndpoint(any()) } returns mockLogsConfigBuilder
        every { anyConstructed<LogsConfiguration.Builder>().build() } returns mockLogsConfig
        every { Logs.enable(any()) } returns Unit

        every { anyConstructed<Logger.Builder>().setNetworkInfoEnabled(any()) } returns mockLoggerBuilder
        every { anyConstructed<Logger.Builder>().setLogcatLogsEnabled(any()) } returns mockLoggerBuilder
        every { anyConstructed<Logger.Builder>().setBundleWithTraceEnabled(any()) } returns mockLoggerBuilder
        every { anyConstructed<Logger.Builder>().build() } returns mockLogger
    }

    @After
    fun tearDown() {
        unmockkAll()
    }

    @Test
    fun `enableLogs with null endpoint succeeds`() {
        DdLogs.enableLogs(customEndpoint = null)

        // Verify Logs.enable was called with config
        verify { Logs.enable(any()) }

        // Verify useCustomEndpoint was NOT called
        verify(exactly = 0) { anyConstructed<LogsConfiguration.Builder>().useCustomEndpoint(any()) }
    }

    @Test
    fun `enableLogs with custom endpoint calls useCustomEndpoint`() {
        val customEndpoint = "https://custom-logs.example.com/v1/input"

        DdLogs.enableLogs(customEndpoint = customEndpoint)

        // Verify useCustomEndpoint was called with correct value
        verify { anyConstructed<LogsConfiguration.Builder>().useCustomEndpoint(customEndpoint) }

        // Verify Logs.enable was called
        verify { Logs.enable(any()) }
    }

    @Test
    fun `enableLogs with empty string does not call useCustomEndpoint`() {
        DdLogs.enableLogs(customEndpoint = "")

        // Empty string should not call useCustomEndpoint
        verify(exactly = 0) { anyConstructed<LogsConfiguration.Builder>().useCustomEndpoint(any()) }

        verify { Logs.enable(any()) }
    }

    @Test
    fun `enableLogs with whitespace only string does not call useCustomEndpoint`() {
        DdLogs.enableLogs(customEndpoint = "   ")

        // Whitespace-only string should not call useCustomEndpoint
        verify(exactly = 0) { anyConstructed<LogsConfiguration.Builder>().useCustomEndpoint(any()) }

        verify { Logs.enable(any()) }
    }

    @Test
    fun `enableLogs with valid https endpoint succeeds`() {
        val httpsEndpoint = "https://logs.example.com/api"

        DdLogs.enableLogs(customEndpoint = httpsEndpoint)

        verify { anyConstructed<LogsConfiguration.Builder>().useCustomEndpoint(httpsEndpoint) }
        verify { Logs.enable(any()) }
    }

    @Test
    fun `enableLogs with valid http endpoint succeeds`() {
        val httpEndpoint = "http://localhost:8080/logs"

        DdLogs.enableLogs(customEndpoint = httpEndpoint)

        verify { anyConstructed<LogsConfiguration.Builder>().useCustomEndpoint(httpEndpoint) }
        verify { Logs.enable(any()) }
    }
}
