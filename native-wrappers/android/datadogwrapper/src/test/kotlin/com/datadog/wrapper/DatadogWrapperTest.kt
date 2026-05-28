/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

package com.datadog.wrapper

import android.content.Context
import android.util.Log
import com.datadog.android.Datadog
import com.datadog.android.DatadogSite
import com.datadog.android.ndk.NdkCrashReports
import com.datadog.android.core.configuration.BatchProcessingLevel
import com.datadog.android.core.configuration.BatchSize
import com.datadog.android.core.configuration.Configuration
import com.datadog.android.core.configuration.UploadFrequency
import com.datadog.android.privacy.TrackingConsent
import com.datadog.android.rum.GlobalRumMonitor
import com.datadog.android.rum.RumMonitor
import com.datadog.android.trace.TracingHeaderType
import java.net.InetSocketAddress
import java.net.Proxy
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import io.mockk.every
import io.mockk.just
import io.mockk.mockk
import io.mockk.mockkStatic
import io.mockk.runs
import io.mockk.unmockkStatic
import io.mockk.verify
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

class DatadogWrapperTest {
    // -- Site mapping --

    @Test
    fun mapSite_us1() {
        assertEquals(DatadogSite.US1, DatadogWrapper.mapSite("us1"))
    }

    @Test
    fun mapSite_us3() {
        assertEquals(DatadogSite.US3, DatadogWrapper.mapSite("us3"))
    }

    @Test
    fun mapSite_us5() {
        assertEquals(DatadogSite.US5, DatadogWrapper.mapSite("us5"))
    }

    @Test
    fun mapSite_eu1() {
        assertEquals(DatadogSite.EU1, DatadogWrapper.mapSite("eu1"))
    }

    @Test
    fun mapSite_ap1() {
        assertEquals(DatadogSite.AP1, DatadogWrapper.mapSite("ap1"))
    }

    @Test
    fun mapSite_ap2() {
        assertEquals(DatadogSite.AP2, DatadogWrapper.mapSite("ap2"))
    }

    @Test
    fun mapSite_us1Fed() {
        assertEquals(DatadogSite.US1_FED, DatadogWrapper.mapSite("us1_fed"))
    }

    @Test
    fun mapSite_unknownDefaultsToUs1() {
        assertEquals(DatadogSite.US1, DatadogWrapper.mapSite("invalid"))
    }

    @Test
    fun mapSite_isCaseInsensitive() {
        assertEquals(DatadogSite.EU1, DatadogWrapper.mapSite("EU1"))
    }

    // -- Tracking consent mapping --

    @Test
    fun mapTrackingConsent_granted() {
        assertEquals(TrackingConsent.GRANTED, DatadogWrapper.mapTrackingConsent("granted"))
    }

    @Test
    fun mapTrackingConsent_notGranted() {
        assertEquals(TrackingConsent.NOT_GRANTED, DatadogWrapper.mapTrackingConsent("not_granted"))
    }

    @Test
    fun mapTrackingConsent_pending() {
        assertEquals(TrackingConsent.PENDING, DatadogWrapper.mapTrackingConsent("pending"))
    }

    @Test
    fun mapTrackingConsent_unknownDefaultsToPending() {
        assertEquals(TrackingConsent.PENDING, DatadogWrapper.mapTrackingConsent("invalid"))
    }

    // -- Verbosity mapping --

    @Test
    fun mapVerbosity_debug() {
        assertEquals(Log.DEBUG, DatadogWrapper.mapVerbosity("debug"))
    }

    @Test
    fun mapVerbosity_info() {
        assertEquals(Log.INFO, DatadogWrapper.mapVerbosity("info"))
    }

    @Test
    fun mapVerbosity_warn() {
        assertEquals(Log.WARN, DatadogWrapper.mapVerbosity("warn"))
    }

    @Test
    fun mapVerbosity_error() {
        assertEquals(Log.ERROR, DatadogWrapper.mapVerbosity("error"))
    }

    @Test
    fun mapVerbosity_unknownDefaultsToError() {
        assertEquals(Log.ERROR, DatadogWrapper.mapVerbosity("invalid"))
    }

    // -- Batch size mapping --

    @Test
    fun mapBatchSize_small() {
        assertEquals(BatchSize.SMALL, DatadogWrapper.mapBatchSize("small"))
    }

    @Test
    fun mapBatchSize_medium() {
        assertEquals(BatchSize.MEDIUM, DatadogWrapper.mapBatchSize("medium"))
    }

    @Test
    fun mapBatchSize_large() {
        assertEquals(BatchSize.LARGE, DatadogWrapper.mapBatchSize("large"))
    }

    @Test
    fun mapBatchSize_unknownDefaultsToMedium() {
        assertEquals(BatchSize.MEDIUM, DatadogWrapper.mapBatchSize("invalid"))
    }

    // -- Upload frequency mapping --

    @Test
    fun mapUploadFrequency_frequent() {
        assertEquals(UploadFrequency.FREQUENT, DatadogWrapper.mapUploadFrequency("frequent"))
    }

    @Test
    fun mapUploadFrequency_average() {
        assertEquals(UploadFrequency.AVERAGE, DatadogWrapper.mapUploadFrequency("average"))
    }

    @Test
    fun mapUploadFrequency_rare() {
        assertEquals(UploadFrequency.RARE, DatadogWrapper.mapUploadFrequency("rare"))
    }

    @Test
    fun mapUploadFrequency_unknownDefaultsToAverage() {
        assertEquals(UploadFrequency.AVERAGE, DatadogWrapper.mapUploadFrequency("invalid"))
    }

    // -- Batch processing level mapping --

    @Test
    fun mapBatchProcessingLevel_low() {
        assertEquals(BatchProcessingLevel.LOW, DatadogWrapper.mapBatchProcessingLevel("low"))
    }

    @Test
    fun mapBatchProcessingLevel_medium() {
        assertEquals(BatchProcessingLevel.MEDIUM, DatadogWrapper.mapBatchProcessingLevel("medium"))
    }

    @Test
    fun mapBatchProcessingLevel_high() {
        assertEquals(BatchProcessingLevel.HIGH, DatadogWrapper.mapBatchProcessingLevel("high"))
    }

    @Test
    fun mapBatchProcessingLevel_unknownDefaultsToMedium() {
        assertEquals(BatchProcessingLevel.MEDIUM, DatadogWrapper.mapBatchProcessingLevel("invalid"))
    }

    // -- Tracing header type mapping --

    @Test
    fun `mapTracingHeaderType maps all values`() {
        assertEquals(TracingHeaderType.DATADOG, DatadogWrapper.mapTracingHeaderType("datadog"))
        assertEquals(TracingHeaderType.B3, DatadogWrapper.mapTracingHeaderType("b3"))
        assertEquals(TracingHeaderType.B3MULTI, DatadogWrapper.mapTracingHeaderType("b3multi"))
        assertEquals(TracingHeaderType.TRACECONTEXT, DatadogWrapper.mapTracingHeaderType("tracecontext"))
        assertEquals(TracingHeaderType.DATADOG, DatadogWrapper.mapTracingHeaderType("unknown"))
    }

    // -- First-party hosts parsing --

    @Test
    fun `parseFirstPartyHosts parses dictionary with comma-separated types`() {
        val hosts = mapOf<String, Any?>(
            "api.example.com" to "datadog,tracecontext",
            "cdn.example.com" to "b3"
        )
        val result = DatadogWrapper.parseFirstPartyHosts(hosts)

        assertEquals(2, result.size)
        assertEquals(setOf(TracingHeaderType.DATADOG, TracingHeaderType.TRACECONTEXT), result["api.example.com"])
        assertEquals(setOf(TracingHeaderType.B3), result["cdn.example.com"])
    }

    @Test
    fun `parseFirstPartyHosts returns empty map for empty input`() {
        val result = DatadogWrapper.parseFirstPartyHosts(emptyMap())
        assertTrue(result.isEmpty())
    }

    // -- SDK initialization --

    private val mockContext: Context = mockk(relaxed = true)

    private fun withMockedDatadog(block: () -> Unit) {
        mockkStatic(Datadog::class)
        every { Datadog.initialize(any(), any<Configuration>(), any()) } returns null
        every { Datadog.setVerbosity(any()) } returns Unit
        try {
            block()
        } finally {
            unmockkStatic(Datadog::class)
        }
    }

    @Test
    fun initialize_callsDatadogInitialize() {
        withMockedDatadog {
            val result = DatadogWrapper.initialize(
                context = mockContext,
                clientToken = "pub-test-token",
                environment = "test",
                service = null,
                site = "us1",
                verbosity = "error",
                trackingConsent = "pending"
            )

            assertTrue(result)
            verify { Datadog.initialize(mockContext, any<Configuration>(), TrackingConsent.PENDING) }
        }
    }

    @Test
    fun initialize_setsVerbosityLevel() {
        withMockedDatadog {
            DatadogWrapper.initialize(
                context = mockContext,
                clientToken = "pub-test-token",
                environment = "test",
                service = null,
                site = "us1",
                verbosity = "warn",
                trackingConsent = "pending"
            )

            verify { Datadog.setVerbosity(Log.WARN) }
        }
    }

    @Test
    fun initialize_withDebugVerbosity_setsDebugLevel() {
        withMockedDatadog {
            DatadogWrapper.initialize(
                context = mockContext,
                clientToken = "pub-test-token",
                environment = "test",
                service = null,
                site = "us1",
                verbosity = "debug",
                trackingConsent = "pending"
            )

            verify { Datadog.setVerbosity(Log.DEBUG) }
        }
    }

    @Test
    fun initialize_withGrantedConsent_passesGrantedToDatadog() {
        withMockedDatadog {
            DatadogWrapper.initialize(
                context = mockContext,
                clientToken = "pub-test-token",
                environment = "test",
                service = null,
                site = "us1",
                verbosity = "error",
                trackingConsent = "granted"
            )

            verify { Datadog.initialize(mockContext, any<Configuration>(), TrackingConsent.GRANTED) }
        }
    }

    // -- Native crash report enabled --

    @Test
    fun initialize_withNativeCrashReportEnabled_callsNdkCrashReportsEnable() {
        mockkStatic(Datadog::class)
        mockkStatic(NdkCrashReports::class)
        every { Datadog.initialize(any(), any<Configuration>(), any()) } returns null
        every { Datadog.setVerbosity(any()) } returns Unit
        every { NdkCrashReports.enable() } just runs
        every { NdkCrashReports.enable(any()) } just runs

        try {
            DatadogWrapper.initialize(
                context = mockContext,
                clientToken = "pub-test-token",
                environment = "test",
                service = null,
                site = "us1",
                verbosity = "error",
                trackingConsent = "pending",
                nativeCrashReportEnabled = true
            )

            verify { NdkCrashReports.enable() }
        } finally {
            unmockkStatic(Datadog::class)
            unmockkStatic(NdkCrashReports::class)
        }
    }

    @Test
    fun initialize_withNativeCrashReportDisabled_doesNotCallNdkCrashReportsEnable() {
        mockkStatic(Datadog::class)
        mockkStatic(NdkCrashReports::class)
        every { Datadog.initialize(any(), any<Configuration>(), any()) } returns null
        every { Datadog.setVerbosity(any()) } returns Unit

        try {
            DatadogWrapper.initialize(
                context = mockContext,
                clientToken = "pub-test-token",
                environment = "test",
                service = null,
                site = "us1",
                verbosity = "error",
                trackingConsent = "pending",
                nativeCrashReportEnabled = false
            )

            verify(exactly = 0) { NdkCrashReports.enable() }
            verify(exactly = 0) { NdkCrashReports.enable(any()) }
        } finally {
            unmockkStatic(Datadog::class)
            unmockkStatic(NdkCrashReports::class)
        }
    }

    // -- Set tracking consent --

    private fun withMockedDatadogSetTrackingConsent(block: () -> Unit) {
        mockkStatic(Datadog::class)
        every { Datadog.setTrackingConsent(any()) } returns Unit
        try {
            block()
        } finally {
            unmockkStatic(Datadog::class)
        }
    }

    @Test
    fun setTrackingConsent_granted_callsDatadogWithGranted() {
        withMockedDatadogSetTrackingConsent {
            DatadogWrapper.setTrackingConsent("granted")
            verify { Datadog.setTrackingConsent(TrackingConsent.GRANTED) }
        }
    }

    @Test
    fun setTrackingConsent_notGranted_callsDatadogWithNotGranted() {
        withMockedDatadogSetTrackingConsent {
            DatadogWrapper.setTrackingConsent("not_granted")
            verify { Datadog.setTrackingConsent(TrackingConsent.NOT_GRANTED) }
        }
    }

    @Test
    fun setTrackingConsent_pending_callsDatadogWithPending() {
        withMockedDatadogSetTrackingConsent {
            DatadogWrapper.setTrackingConsent("pending")
            verify { Datadog.setTrackingConsent(TrackingConsent.PENDING) }
        }
    }

    // -- Proxy configuration mapping --

    @Test
    fun mapProxyConfiguration_null_returnsNull() {
        assertEquals(null, DatadogWrapper.mapProxyConfiguration(null))
    }

    @Test
    fun mapProxyConfiguration_http_returnsHttpProxy() {
        val config = mapOf<String, Any>("type" to "http", "address" to "1.2.3.4", "port" to 8080)
        val result = DatadogWrapper.mapProxyConfiguration(config)

        checkNotNull(result)
        assertEquals(Proxy.Type.HTTP, result.first.type())
        assertEquals(InetSocketAddress("1.2.3.4", 8080), result.first.address())
        assertNull(result.second)
    }

    @Test
    fun mapProxyConfiguration_https_returnsHttpProxy() {
        val config = mapOf<String, Any>("type" to "https", "address" to "proxy.example.com", "port" to 443)
        val result = DatadogWrapper.mapProxyConfiguration(config)

        checkNotNull(result)
        assertEquals(Proxy.Type.HTTP, result.first.type())
        assertEquals(InetSocketAddress("proxy.example.com", 443), result.first.address())
    }

    @Test
    fun mapProxyConfiguration_socks_returnsSocksProxy() {
        val config = mapOf<String, Any>("type" to "socks", "address" to "socks.example.com", "port" to 1080)
        val result = DatadogWrapper.mapProxyConfiguration(config)

        checkNotNull(result)
        assertEquals(Proxy.Type.SOCKS, result.first.type())
        assertEquals(InetSocketAddress("socks.example.com", 1080), result.first.address())
    }

    @Test
    fun mapProxyConfiguration_withAuth_returnsAuthenticator() {
        val config = mapOf<String, Any>(
            "type" to "http",
            "address" to "1.2.3.4",
            "port" to 8080,
            "username" to "user",
            "password" to "pass"
        )
        val result = DatadogWrapper.mapProxyConfiguration(config)

        checkNotNull(result)
        assertNotNull(result.second)
    }

    @Test
    fun mapProxyConfiguration_withoutAuth_returnsNullAuthenticator() {
        val config = mapOf<String, Any>("type" to "http", "address" to "1.2.3.4", "port" to 8080)
        val result = DatadogWrapper.mapProxyConfiguration(config)

        checkNotNull(result)
        assertNull(result.second)
    }

    // -- Global attributes --

    @Test
    fun `addAttribute calls GlobalRumMonitor addAttribute`() {
        withMockedDatadog {
            val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
            mockkStatic(GlobalRumMonitor::class) {
                every { GlobalRumMonitor.get() } returns mockRumMonitor

                DatadogWrapper.addAttribute("plan", "premium")

                verify { mockRumMonitor.addAttribute("plan", "premium") }
            }
        }
    }

    @Test
    fun `addAttributes calls GlobalRumMonitor addAttribute for each entry`() {
        withMockedDatadog {
            val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
            mockkStatic(GlobalRumMonitor::class) {
                every { GlobalRumMonitor.get() } returns mockRumMonitor

                DatadogWrapper.addAttributes(mapOf("plan" to "premium", "version" to "2.0"))

                verify { mockRumMonitor.addAttribute("plan", "premium") }
                verify { mockRumMonitor.addAttribute("version", "2.0") }
            }
        }
    }

    @Test
    fun `removeAttribute calls GlobalRumMonitor removeAttribute`() {
        withMockedDatadog {
            val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
            mockkStatic(GlobalRumMonitor::class) {
                every { GlobalRumMonitor.get() } returns mockRumMonitor

                DatadogWrapper.removeAttribute("plan")

                verify { mockRumMonitor.removeAttribute("plan") }
            }
        }
    }

    @Test
    fun `removeAttributes calls GlobalRumMonitor removeAttribute for each key`() {
        withMockedDatadog {
            val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
            mockkStatic(GlobalRumMonitor::class) {
                every { GlobalRumMonitor.get() } returns mockRumMonitor

                DatadogWrapper.removeAttributes(listOf("plan", "version"))

                verify { mockRumMonitor.removeAttribute("plan") }
                verify { mockRumMonitor.removeAttribute("version") }
            }
        }
    }

    // -- User Info --

    @Test
    fun `setUserInfo calls Datadog setUserInfo`() {
        withMockedDatadog {
            every { Datadog.setUserInfo(any(), any(), any(), any()) } returns Unit

            DatadogWrapper.setUserInfo("user-123", "John", "john@example.com", mapOf("plan" to "premium"))

            verify { Datadog.setUserInfo("user-123", "John", "john@example.com", mapOf("plan" to "premium")) }
        }
    }

    @Test
    fun `addUserExtraInfo calls Datadog addUserProperties`() {
        withMockedDatadog {
            every { Datadog.addUserProperties(any()) } returns Unit

            DatadogWrapper.addUserExtraInfo(mapOf("plan" to "premium"))

            verify { Datadog.addUserProperties(mapOf("plan" to "premium")) }
        }
    }

    @Test
    fun `clearUserInfo calls Datadog clearUserInfo`() {
        withMockedDatadog {
            every { Datadog.clearUserInfo() } returns Unit

            DatadogWrapper.clearUserInfo()

            verify { Datadog.clearUserInfo() }
        }
    }

    // -- Account Info --

    @Test
    fun `setAccountInfo calls Datadog setAccountInfo`() {
        withMockedDatadog {
            every { Datadog.setAccountInfo(any(), any(), any()) } returns Unit

            DatadogWrapper.setAccountInfo("acct-456", "Acme Corp", mapOf("tier" to "enterprise"))

            verify { Datadog.setAccountInfo("acct-456", "Acme Corp", mapOf("tier" to "enterprise")) }
        }
    }

    @Test
    fun `addAccountExtraInfo calls Datadog addAccountExtraInfo`() {
        withMockedDatadog {
            every { Datadog.addAccountExtraInfo(any()) } returns Unit

            DatadogWrapper.addAccountExtraInfo(mapOf("tier" to "enterprise"))

            verify { Datadog.addAccountExtraInfo(mapOf("tier" to "enterprise")) }
        }
    }

    @Test
    fun `clearAccountInfo calls Datadog clearAccountInfo`() {
        withMockedDatadog {
            every { Datadog.clearAccountInfo() } returns Unit

            DatadogWrapper.clearAccountInfo()

            verify { Datadog.clearAccountInfo() }
        }
    }
}