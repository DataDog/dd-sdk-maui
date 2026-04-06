package com.datadog.wrapper

import android.util.Log
import com.datadog.android.Datadog
import com.datadog.android.ndk.NdkCrashReports
import com.datadog.android.rum.Rum
import com.datadog.android.rum.RumConfiguration
import com.datadog.android.rum.configuration.VitalsUpdateFrequency
import com.datadog.android.rum.tracking.ActivityViewTrackingStrategy
import io.mockk.*
import org.junit.After
import org.junit.Assert.*
import org.junit.Before
import org.junit.Test

class DdRumTest {
    private val mockRumConfig = mockk<RumConfiguration>(relaxed = true)
    private val mockRumConfigBuilder = mockk<RumConfiguration.Builder>(relaxed = true)
    private val mockDatadogInstance = mockk<com.datadog.android.api.SdkCore>(relaxed = true)

    @Before
    fun setUp() {
        DdRum.resetForTesting()

        mockkStatic(Rum::class)
        mockkStatic(Datadog::class)
        mockkStatic(NdkCrashReports::class)
        mockkConstructor(RumConfiguration.Builder::class)

        every { NdkCrashReports.enable() } just runs
        every { NdkCrashReports.enable(any()) } just runs

        every { Datadog.getInstance() } returns mockDatadogInstance
        every { anyConstructed<RumConfiguration.Builder>().setSessionSampleRate(any()) } returns mockRumConfigBuilder
        every { anyConstructed<RumConfiguration.Builder>().setTelemetrySampleRate(any()) } returns mockRumConfigBuilder
        every { anyConstructed<RumConfiguration.Builder>().setVitalsUpdateFrequency(any()) } returns mockRumConfigBuilder
        every { anyConstructed<RumConfiguration.Builder>().useViewTrackingStrategy(any()) } returns mockRumConfigBuilder
        every { anyConstructed<RumConfiguration.Builder>().trackUserInteractions() } returns mockRumConfigBuilder
        every { anyConstructed<RumConfiguration.Builder>().trackLongTasks(any()) } returns mockRumConfigBuilder
        every { anyConstructed<RumConfiguration.Builder>().trackFrustrations(any()) } returns mockRumConfigBuilder
        every { anyConstructed<RumConfiguration.Builder>().trackBackgroundEvents(any()) } returns mockRumConfigBuilder
        every { anyConstructed<RumConfiguration.Builder>().trackNonFatalAnrs(any()) } returns mockRumConfigBuilder
        every { anyConstructed<RumConfiguration.Builder>().useCustomEndpoint(any()) } returns mockRumConfigBuilder
        every { anyConstructed<RumConfiguration.Builder>().build() } returns mockRumConfig
        every { Rum.enable(any(), any()) } returns Unit
    }

    @After
    fun tearDown() {
        unmockkAll()
    }

    // ── enableRum basic ──────────────────────────────────────

    @Test
    fun `enableRum with valid applicationId calls Rum enable`() {
        DdRum.enableRum(mapOf("applicationId" to "test-app-id"))

        verify { Rum.enable(any(), any()) }
    }

    @Test
    fun `enableRum with missing applicationId does not call Rum enable`() {
        mockkStatic(Log::class)
        every { Log.w(any<String>(), any<String>()) } returns 0

        DdRum.enableRum(mapOf("sessionSampleRate" to 80.0))

        verify(exactly = 0) { Rum.enable(any(), any()) }
        verify { Log.w("DatadogWrapper", match<String> { it.contains("applicationId is required") }) }
    }

    @Test
    fun `enableRum with empty applicationId does not call Rum enable`() {
        mockkStatic(Log::class)
        every { Log.w(any<String>(), any<String>()) } returns 0

        DdRum.enableRum(mapOf("applicationId" to ""))

        verify(exactly = 0) { Rum.enable(any(), any()) }
    }

    // ── Sample rates ─────────────────────────────────────────

    @Test
    fun `enableRum sets sessionSampleRate`() {
        DdRum.enableRum(mapOf(
            "applicationId" to "app-id",
            "sessionSampleRate" to 75.0
        ))

        verify { anyConstructed<RumConfiguration.Builder>().setSessionSampleRate(75.0f) }
    }

    @Test
    fun `enableRum sets telemetrySampleRate`() {
        DdRum.enableRum(mapOf(
            "applicationId" to "app-id",
            "telemetrySampleRate" to 10.0
        ))

        verify { anyConstructed<RumConfiguration.Builder>().setTelemetrySampleRate(10.0f) }
    }

    // ── Vitals update frequency ──────────────────────────────

    @Test
    fun `enableRum sets vitalsUpdateFrequency`() {
        DdRum.enableRum(mapOf(
            "applicationId" to "app-id",
            "vitalsUpdateFrequency" to "rare"
        ))

        verify { anyConstructed<RumConfiguration.Builder>().setVitalsUpdateFrequency(VitalsUpdateFrequency.RARE) }
    }

    // ── View and interaction tracking ────────────────────────

    @Test
    fun `enableRum nativeViewTracking true sets view tracking strategy`() {
        DdRum.enableRum(mapOf(
            "applicationId" to "app-id",
            "nativeViewTracking" to true
        ))

        verify { anyConstructed<RumConfiguration.Builder>().useViewTrackingStrategy(any<ActivityViewTrackingStrategy>()) }
    }

    @Test
    fun `enableRum nativeViewTracking false does not set view tracking`() {
        DdRum.enableRum(mapOf(
            "applicationId" to "app-id",
            "nativeViewTracking" to false
        ))

        verify(exactly = 0) { anyConstructed<RumConfiguration.Builder>().useViewTrackingStrategy(any()) }
    }

    @Test
    fun `enableRum nativeInteractionTracking true calls trackUserInteractions`() {
        DdRum.enableRum(mapOf(
            "applicationId" to "app-id",
            "nativeInteractionTracking" to true
        ))

        verify { anyConstructed<RumConfiguration.Builder>().trackUserInteractions() }
    }

    @Test
    fun `enableRum nativeInteractionTracking false does not call trackUserInteractions`() {
        DdRum.enableRum(mapOf(
            "applicationId" to "app-id",
            "nativeInteractionTracking" to false
        ))

        verify(exactly = 0) { anyConstructed<RumConfiguration.Builder>().trackUserInteractions() }
    }

    // ── Long task threshold ──────────────────────────────────

    @Test
    fun `enableRum sets nativeLongTaskThresholdMs`() {
        DdRum.enableRum(mapOf(
            "applicationId" to "app-id",
            "nativeLongTaskThresholdMs" to 500.0
        ))

        verify { anyConstructed<RumConfiguration.Builder>().trackLongTasks(500L) }
    }

    // ── Tracking flags ───────────────────────────────────────

    @Test
    fun `enableRum sets trackFrustrations`() {
        DdRum.enableRum(mapOf(
            "applicationId" to "app-id",
            "trackFrustrations" to false
        ))

        verify { anyConstructed<RumConfiguration.Builder>().trackFrustrations(false) }
    }

    @Test
    fun `enableRum sets trackBackgroundEvents`() {
        DdRum.enableRum(mapOf(
            "applicationId" to "app-id",
            "trackBackgroundEvents" to true
        ))

        verify { anyConstructed<RumConfiguration.Builder>().trackBackgroundEvents(true) }
    }

    @Test
    fun `enableRum sets trackNonFatalAnrs when provided`() {
        DdRum.enableRum(mapOf(
            "applicationId" to "app-id",
            "trackNonFatalAnrs" to true
        ))

        verify { anyConstructed<RumConfiguration.Builder>().trackNonFatalAnrs(true) }
    }

    @Test
    fun `enableRum does not set trackNonFatalAnrs when null`() {
        DdRum.enableRum(mapOf(
            "applicationId" to "app-id"
        ))

        verify(exactly = 0) { anyConstructed<RumConfiguration.Builder>().trackNonFatalAnrs(any()) }
    }

    // ── Custom endpoint ──────────────────────────────────────

    @Test
    fun `enableRum sets customEndpoint`() {
        DdRum.enableRum(mapOf(
            "applicationId" to "app-id",
            "customEndpoint" to "https://rum.example.com"
        ))

        verify { anyConstructed<RumConfiguration.Builder>().useCustomEndpoint("https://rum.example.com") }
    }

    @Test
    fun `enableRum empty customEndpoint does not set endpoint`() {
        DdRum.enableRum(mapOf(
            "applicationId" to "app-id",
            "customEndpoint" to ""
        ))

        verify(exactly = 0) { anyConstructed<RumConfiguration.Builder>().useCustomEndpoint(any()) }
    }

    // ── initialResourceThreshold storage ────────────────────

    @Test
    fun `enableRum stores initialResourceThreshold`() {
        DdRum.enableRum(mapOf(
            "applicationId" to "app-id",
            "initialResourceThreshold" to 0.5
        ))

        assertEquals(0.5, DdRum.initialResourceThreshold)
    }

    @Test
    fun `enableRum initialResourceThreshold defaults null`() {
        DdRum.enableRum(mapOf(
            "applicationId" to "app-id"
        ))

        assertNull(DdRum.initialResourceThreshold)
    }

    // ── nativeCrashReportEnabled storage ─────────────────────

    @Test
    fun `enableRum stores nativeCrashReportEnabled true and enables NdkCrashReports`() {
        DdRum.enableRum(mapOf(
            "applicationId" to "app-id",
            "nativeCrashReportEnabled" to true
        ))

        assertTrue(DdRum.nativeCrashReportEnabled)
        verify { NdkCrashReports.enable(any()) }
    }

    @Test
    fun `enableRum nativeCrashReportEnabled false does not enable NdkCrashReports`() {
        DdRum.enableRum(mapOf(
            "applicationId" to "app-id"
        ))

        assertFalse(DdRum.nativeCrashReportEnabled)
        verify(exactly = 0) { NdkCrashReports.enable(any()) }
    }

    // ── Mapping helpers ──────────────────────────────────────

    @Test
    fun `mapVitalsUpdateFrequency maps all values`() {
        assertEquals(VitalsUpdateFrequency.NEVER, DdRum.mapVitalsUpdateFrequency("never"))
        assertEquals(VitalsUpdateFrequency.RARE, DdRum.mapVitalsUpdateFrequency("rare"))
        assertEquals(VitalsUpdateFrequency.AVERAGE, DdRum.mapVitalsUpdateFrequency("average"))
        assertEquals(VitalsUpdateFrequency.FREQUENT, DdRum.mapVitalsUpdateFrequency("frequent"))
        assertEquals(VitalsUpdateFrequency.AVERAGE, DdRum.mapVitalsUpdateFrequency("unknown"))
    }

}
