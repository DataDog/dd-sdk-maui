package com.datadog.wrapper

import android.content.Context
import com.datadog.android.Datadog
import com.datadog.android.api.SdkCore
import com.datadog.android.privacy.TrackingConsent
import io.mockk.*
import org.junit.After
import org.junit.Assert.*
import org.junit.Before
import org.junit.Test

class DatadogWrapperTest {

    private val mockContext = mockk<Context>(relaxed = true)
    private val mockSdkCore = mockk<SdkCore>(relaxed = true)

    @Before
    fun setUp() {
        mockkStatic(Datadog::class)
        every { Datadog.initialize(any(), any(), any()) } returns mockSdkCore
        every { Datadog.setVerbosity(any()) } returns Unit
    }

    @After
    fun tearDown() {
        unmockkAll()
    }

    @Test
    fun `initialize with minimal config returns true`() {
        val result = DatadogWrapper.initialize(
            context = mockContext,
            clientToken = "test-token",
            environment = "test",
            service = null
        )
        assertTrue(result)
    }

    @Test
    fun `initialize with all parameters returns true`() {
        val result = DatadogWrapper.initialize(
            context = mockContext,
            clientToken = "test-token",
            environment = "production",
            service = "my-service",
            site = "eu1",
            verbosity = "debug",
            trackingConsent = "pending",
            batchSize = "large",
            uploadFrequency = "frequent",
            batchProcessingLevel = "high",
            additionalConfiguration = mapOf("key" to "value")
        )
        assertTrue(result)
    }

    @Test
    fun `initialize maps granted tracking consent`() {
        DatadogWrapper.initialize(
            context = mockContext,
            clientToken = "test-token",
            environment = "test",
            service = null,
            trackingConsent = "granted"
        )
        verify { Datadog.initialize(any(), any(), TrackingConsent.GRANTED) }
    }

    @Test
    fun `initialize maps not_granted tracking consent`() {
        DatadogWrapper.initialize(
            context = mockContext,
            clientToken = "test-token",
            environment = "test",
            service = null,
            trackingConsent = "not_granted"
        )
        verify { Datadog.initialize(any(), any(), TrackingConsent.NOT_GRANTED) }
    }

    @Test
    fun `initialize maps pending tracking consent`() {
        DatadogWrapper.initialize(
            context = mockContext,
            clientToken = "test-token",
            environment = "test",
            service = null,
            trackingConsent = "pending"
        )
        verify { Datadog.initialize(any(), any(), TrackingConsent.PENDING) }
    }

    @Test
    fun `initialize defaults to granted for unknown consent`() {
        DatadogWrapper.initialize(
            context = mockContext,
            clientToken = "test-token",
            environment = "test",
            service = null,
            trackingConsent = "invalid_value"
        )
        verify { Datadog.initialize(any(), any(), TrackingConsent.GRANTED) }
    }
}
