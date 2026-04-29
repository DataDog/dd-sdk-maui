/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

package com.datadog.wrapper

import com.datadog.android.api.SdkCore
import com.datadog.android.Datadog
import com.datadog.android.sessionreplay.ImagePrivacy
import com.datadog.android.sessionreplay.SessionReplay
import com.datadog.android.sessionreplay.SessionReplayConfiguration
import com.datadog.android.sessionreplay.TextAndInputPrivacy
import com.datadog.android.sessionreplay.TouchPrivacy
import io.mockk.every
import io.mockk.mockk
import io.mockk.mockkConstructor
import io.mockk.mockkStatic
import io.mockk.unmockkAll
import io.mockk.verify
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Before
import org.junit.Test

class DdSessionReplayTest {
    private val mockSessionReplayConfig = mockk<SessionReplayConfiguration>(relaxed = true)
    private val mockConfigBuilder = mockk<SessionReplayConfiguration.Builder>(relaxed = true)
    private val mockDatadogInstance = mockk<SdkCore>(relaxed = true)

    @Before
    fun setUp() {
        mockkStatic(SessionReplay::class)
        mockkStatic(Datadog::class)
        mockkConstructor(SessionReplayConfiguration.Builder::class)

        every { Datadog.getInstance() } returns mockDatadogInstance
        every { anyConstructed<SessionReplayConfiguration.Builder>().setTextAndInputPrivacy(any()) } returns mockConfigBuilder
        every { anyConstructed<SessionReplayConfiguration.Builder>().setImagePrivacy(any()) } returns mockConfigBuilder
        every { anyConstructed<SessionReplayConfiguration.Builder>().setTouchPrivacy(any()) } returns mockConfigBuilder
        every { anyConstructed<SessionReplayConfiguration.Builder>().useCustomEndpoint(any()) } returns mockConfigBuilder
        every { anyConstructed<SessionReplayConfiguration.Builder>().build() } returns mockSessionReplayConfig
        // Also mock on the returned builder for chained calls
        every { mockConfigBuilder.setTextAndInputPrivacy(any()) } returns mockConfigBuilder
        every { mockConfigBuilder.setImagePrivacy(any()) } returns mockConfigBuilder
        every { mockConfigBuilder.setTouchPrivacy(any()) } returns mockConfigBuilder
        every { mockConfigBuilder.useCustomEndpoint(any()) } returns mockConfigBuilder
        every { mockConfigBuilder.build() } returns mockSessionReplayConfig
        every { SessionReplay.enable(any(), any()) } returns Unit
    }

    @After
    fun tearDown() {
        unmockkAll()
    }

    @Test
    fun `enableSessionReplay calls SessionReplay enable`() {
        DdSessionReplay.enableSessionReplay(100.0, "mask_all", "mask_all", "hide", null)

        verify { SessionReplay.enable(any(), any()) }
    }

    @Test
    fun `enableSessionReplay passes privacy levels to builder`() {
        DdSessionReplay.enableSessionReplay(
            80.0, "mask_sensitive_inputs", "mask_none", "show", null
        )

        verify { anyConstructed<SessionReplayConfiguration.Builder>().setTextAndInputPrivacy(TextAndInputPrivacy.MASK_SENSITIVE_INPUTS) }
        verify { mockConfigBuilder.setImagePrivacy(ImagePrivacy.MASK_NONE) }
        verify { mockConfigBuilder.setTouchPrivacy(TouchPrivacy.SHOW) }
    }

    @Test
    fun `enableSessionReplay with custom endpoint sets endpoint`() {
        DdSessionReplay.enableSessionReplay(50.0, "mask_all", "mask_all", "hide", "https://sr.example.com")

        verify { mockConfigBuilder.useCustomEndpoint("https://sr.example.com") }
    }

    @Test
    fun `enableSessionReplay with null endpoint does not set endpoint`() {
        DdSessionReplay.enableSessionReplay(100.0, "mask_all", "mask_all", "hide", null)

        verify(exactly = 0) { mockConfigBuilder.useCustomEndpoint(any()) }
        verify(exactly = 0) { anyConstructed<SessionReplayConfiguration.Builder>().useCustomEndpoint(any()) }
    }

    // ── Mapping helpers ──────────────────────────────────

    @Test
    fun `mapTextAndInputPrivacy maps all values`() {
        assertEquals(TextAndInputPrivacy.MASK_ALL, DdSessionReplay.mapTextAndInputPrivacy("mask_all"))
        assertEquals(TextAndInputPrivacy.MASK_ALL_INPUTS, DdSessionReplay.mapTextAndInputPrivacy("mask_all_inputs"))
        assertEquals(TextAndInputPrivacy.MASK_SENSITIVE_INPUTS, DdSessionReplay.mapTextAndInputPrivacy("mask_sensitive_inputs"))
        assertEquals(TextAndInputPrivacy.MASK_ALL, DdSessionReplay.mapTextAndInputPrivacy("unknown"))
    }

    @Test
    fun `mapImagePrivacy maps all values`() {
        assertEquals(ImagePrivacy.MASK_ALL, DdSessionReplay.mapImagePrivacy("mask_all"))
        assertEquals(ImagePrivacy.MASK_NONE, DdSessionReplay.mapImagePrivacy("mask_none"))
        assertEquals(ImagePrivacy.MASK_ALL, DdSessionReplay.mapImagePrivacy("unknown"))
    }

    @Test
    fun `mapTouchPrivacy maps all values`() {
        assertEquals(TouchPrivacy.HIDE, DdSessionReplay.mapTouchPrivacy("hide"))
        assertEquals(TouchPrivacy.SHOW, DdSessionReplay.mapTouchPrivacy("show"))
        assertEquals(TouchPrivacy.HIDE, DdSessionReplay.mapTouchPrivacy("unknown"))
    }
}
