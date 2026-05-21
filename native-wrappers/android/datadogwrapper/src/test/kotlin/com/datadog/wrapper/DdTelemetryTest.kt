/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

package com.datadog.wrapper

import com.datadog.android._InternalProxy
import com.datadog.android.event.EventMapper
import com.datadog.android.rum.RumConfiguration
import com.datadog.android.rum._RumInternalProxy
import com.datadog.android.telemetry.model.TelemetryConfigurationEvent
import io.mockk.every
import io.mockk.mockk
import io.mockk.mockkObject
import io.mockk.slot
import io.mockk.unmockkAll
import io.mockk.verify
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Before
import org.junit.Test

class DdTelemetryTest {
    private val mockProxy = mockk<_InternalProxy._TelemetryProxy>(relaxed = true)

    @Before
    fun setUp() {
        DdTelemetry.setTelemetryProvider(object : DdTelemetry.TelemetryProvider {
            override val telemetryProxy: _InternalProxy._TelemetryProxy? = mockProxy
        })
    }

    @After
    fun tearDown() {
        DdTelemetry.resetDependencies()
        unmockkAll()
    }

    // ── Error / Debug ────────────────

    @Test
    fun `error forwards to telemetry proxy`() {
        DdTelemetry.error("boom", kind = "WrapperError", stack = "at foo:1")

        verify { mockProxy.error("boom", "at foo:1", "WrapperError") }
    }

    @Test
    fun `error with throwable forwards both`() {
        val throwable = RuntimeException("oops")

        DdTelemetry.errorWithThrowable("boom", throwable)

        verify { mockProxy.error("boom", throwable) }
    }

    @Test
    fun `debug forwards to telemetry proxy`() {
        DdTelemetry.debug("hello")

        verify { mockProxy.debug("hello") }
    }

    @Test
    fun `error is no-op when proxy unavailable`() {
        DdTelemetry.setTelemetryProvider(object : DdTelemetry.TelemetryProvider {
            override val telemetryProxy: _InternalProxy._TelemetryProxy? = null
        })

        // Should not throw — Datadog not initialized is a valid state.
        DdTelemetry.error("boom")
        DdTelemetry.errorWithThrowable("boom", RuntimeException("oops"))
        DdTelemetry.debug("hello")
    }

    // ── Configuration mapper ────────

    @Test
    fun `installConfigurationMapper applies pending fields to emitted event`() {
        // Capture the mapper installed via _RumInternalProxy
        mockkObject(_RumInternalProxy.Companion)
        val mapperSlot = slot<EventMapper<TelemetryConfigurationEvent>>()
        val mockBuilder = mockk<RumConfiguration.Builder>()
        every {
            _RumInternalProxy.setTelemetryConfigurationEventMapper(mockBuilder, capture(mapperSlot))
        } returns mockBuilder

        DdTelemetry.reportConfiguration(
            mapOf(
                "mauiVersion" to "9.0.0",
                "trackErrors" to true,
                "trackUserInteractions" to false,
                "useProxy" to true,
                "initializationType" to "manual"
            )
        )
        DdTelemetry.installConfigurationMapper(mockBuilder)

        // Build a minimally populated TelemetryConfigurationEvent and run the mapper.
        val event = mockk<TelemetryConfigurationEvent>(relaxed = true)
        val config = TelemetryConfigurationEvent.Configuration()
        every { event.telemetry.configuration } returns config

        val result = mapperSlot.captured.map(event)

        assertEquals(event, result)
        assertEquals("9.0.0", config.mauiVersion)
        assertEquals(true, config.trackErrors)
        assertEquals(false, config.trackUserInteractions)
        assertEquals(true, config.useProxy)
        assertEquals("manual", config.initializationType)
    }

    @Test
    fun `reportConfiguration ignores null-valued fields`() {
        mockkObject(_RumInternalProxy.Companion)
        val mapperSlot = slot<EventMapper<TelemetryConfigurationEvent>>()
        val mockBuilder = mockk<RumConfiguration.Builder>()
        every {
            _RumInternalProxy.setTelemetryConfigurationEventMapper(mockBuilder, capture(mapperSlot))
        } returns mockBuilder

        DdTelemetry.reportConfiguration(mapOf("mauiVersion" to "10.0.0"))
        DdTelemetry.reportConfiguration(mapOf("mauiVersion" to null, "trackErrors" to true))
        DdTelemetry.installConfigurationMapper(mockBuilder)

        val event = mockk<TelemetryConfigurationEvent>(relaxed = true)
        val config = TelemetryConfigurationEvent.Configuration()
        every { event.telemetry.configuration } returns config

        mapperSlot.captured.map(event)

        // First non-null value retained — null doesn't overwrite.
        assertEquals("10.0.0", config.mauiVersion)
        assertEquals(true, config.trackErrors)
    }
}
