package com.datadog.wrapper

import android.util.Log
import com.datadog.android.Datadog
import com.datadog.android.rum.GlobalRumMonitor
import com.datadog.android.rum.Rum
import com.datadog.android.rum.RumActionType
import com.datadog.android.rum.RumAttributes
import com.datadog.android.rum.RumErrorSource
import com.datadog.android.rum.RumMonitor
import com.datadog.android.rum.RumConfiguration
import com.datadog.android.rum.RumResourceKind
import com.datadog.android.rum.RumResourceMethod
import com.datadog.android.rum.configuration.VitalsUpdateFrequency
import com.datadog.android.rum.tracking.ActivityViewTrackingStrategy
import io.mockk.every
import io.mockk.just
import io.mockk.mockk
import io.mockk.mockkConstructor
import io.mockk.mockkStatic
import io.mockk.runs
import io.mockk.unmockkAll
import io.mockk.verify
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
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
        mockkStatic(GlobalRumMonitor::class)
        mockkConstructor(RumConfiguration.Builder::class)

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

    // ── addError ─────────────────────────────────────────

    @Test
    fun `addError calls GlobalRumMonitor addErrorWithStacktrace`() {
        val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
        every { GlobalRumMonitor.get() } returns mockRumMonitor

        DdRum.addError(
            message = "Test error",
            source = "source",
            stacktrace = "at Foo.Bar()",
            context = mapOf("key" to "value"),
            timestampMs = 1234567890L
        )

        verify {
            mockRumMonitor.addErrorWithStacktrace(
                message = "Test error",
                source = RumErrorSource.SOURCE,
                stacktrace = "at Foo.Bar()",
                attributes = match { attrs ->
                    attrs["key"] == "value" &&
                    attrs["_dd.timestamp"] == 1234567890L &&
                    attrs["_dd.error.source_type"] == "maui"
                }
            )
        }
    }

    @Test
    fun `addError with zero timestamp does not add timestamp attribute`() {
        val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
        every { GlobalRumMonitor.get() } returns mockRumMonitor

        DdRum.addError(
            message = "Error",
            source = "source",
            stacktrace = "stack",
            context = emptyMap(),
            timestampMs = 0L
        )

        verify {
            mockRumMonitor.addErrorWithStacktrace(
                message = "Error",
                source = RumErrorSource.SOURCE,
                stacktrace = "stack",
                attributes = match { attrs ->
                    !attrs.containsKey("_dd.timestamp") &&
                    attrs["_dd.error.source_type"] == "maui"
                }
            )
        }
    }

    @Test
    fun `addError maps network source correctly`() {
        val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
        every { GlobalRumMonitor.get() } returns mockRumMonitor

        DdRum.addError(
            message = "Network error",
            source = "network",
            stacktrace = "stack",
            context = emptyMap(),
            timestampMs = 0L
        )

        verify {
            mockRumMonitor.addErrorWithStacktrace(
                message = "Network error",
                source = RumErrorSource.NETWORK,
                stacktrace = any(),
                attributes = any()
            )
        }
    }

    // ── Mapping helpers ──────────────────────────────────────

    @Test
    fun `mapErrorSource maps all values`() {
        assertEquals(RumErrorSource.SOURCE, DdRum.mapErrorSource("source"))
        assertEquals(RumErrorSource.NETWORK, DdRum.mapErrorSource("network"))
        assertEquals(RumErrorSource.CONSOLE, DdRum.mapErrorSource("console"))
        assertEquals(RumErrorSource.WEBVIEW, DdRum.mapErrorSource("webview"))
        assertEquals(RumErrorSource.CUSTOM, DdRum.mapErrorSource("custom"))
        // Unknown defaults to CUSTOM
        assertEquals(RumErrorSource.CUSTOM, DdRum.mapErrorSource("unknown"))
    }


    @Test
    fun `mapVitalsUpdateFrequency maps all values`() {
        assertEquals(VitalsUpdateFrequency.NEVER, DdRum.mapVitalsUpdateFrequency("never"))
        assertEquals(VitalsUpdateFrequency.RARE, DdRum.mapVitalsUpdateFrequency("rare"))
        assertEquals(VitalsUpdateFrequency.AVERAGE, DdRum.mapVitalsUpdateFrequency("average"))
        assertEquals(VitalsUpdateFrequency.FREQUENT, DdRum.mapVitalsUpdateFrequency("frequent"))
        assertEquals(VitalsUpdateFrequency.AVERAGE, DdRum.mapVitalsUpdateFrequency("unknown"))
    }

    @Test
    fun `mapActionType maps all values`() {
        assertEquals(RumActionType.TAP, DdRum.mapActionType("tap"))
        assertEquals(RumActionType.SCROLL, DdRum.mapActionType("scroll"))
        assertEquals(RumActionType.SWIPE, DdRum.mapActionType("swipe"))
        assertEquals(RumActionType.CLICK, DdRum.mapActionType("click"))
        assertEquals(RumActionType.BACK, DdRum.mapActionType("back"))
        assertEquals(RumActionType.CUSTOM, DdRum.mapActionType("custom"))
        // Unknown defaults to CUSTOM
        assertEquals(RumActionType.CUSTOM, DdRum.mapActionType("unknown"))
    }

    @Test
    fun `mapResourceMethod maps all values`() {
        assertEquals(RumResourceMethod.POST, DdRum.mapResourceMethod("post"))
        assertEquals(RumResourceMethod.PUT, DdRum.mapResourceMethod("put"))
        assertEquals(RumResourceMethod.DELETE, DdRum.mapResourceMethod("delete"))
        assertEquals(RumResourceMethod.HEAD, DdRum.mapResourceMethod("head"))
        assertEquals(RumResourceMethod.PATCH, DdRum.mapResourceMethod("patch"))
        assertEquals(RumResourceMethod.CONNECT, DdRum.mapResourceMethod("connect"))
        assertEquals(RumResourceMethod.TRACE, DdRum.mapResourceMethod("trace"))
        assertEquals(RumResourceMethod.OPTIONS, DdRum.mapResourceMethod("options"))
        assertEquals(RumResourceMethod.GET, DdRum.mapResourceMethod("get"))
        // Unknown defaults to GET
        assertEquals(RumResourceMethod.GET, DdRum.mapResourceMethod("unknown"))
    }

    @Test
    fun `mapResourceKind maps all values`() {
        assertEquals(RumResourceKind.XHR, DdRum.mapResourceKind("xhr"))
        assertEquals(RumResourceKind.NATIVE, DdRum.mapResourceKind("native"))
        assertEquals(RumResourceKind.FETCH, DdRum.mapResourceKind("fetch"))
        assertEquals(RumResourceKind.DOCUMENT, DdRum.mapResourceKind("document"))
        assertEquals(RumResourceKind.BEACON, DdRum.mapResourceKind("beacon"))
        assertEquals(RumResourceKind.IMAGE, DdRum.mapResourceKind("image"))
        assertEquals(RumResourceKind.FONT, DdRum.mapResourceKind("font"))
        assertEquals(RumResourceKind.CSS, DdRum.mapResourceKind("css"))
        assertEquals(RumResourceKind.MEDIA, DdRum.mapResourceKind("media"))
        assertEquals(RumResourceKind.JS, DdRum.mapResourceKind("js"))
        assertEquals(RumResourceKind.OTHER, DdRum.mapResourceKind("other"))
        // Unknown defaults to OTHER
        assertEquals(RumResourceKind.OTHER, DdRum.mapResourceKind("unknown"))
    }

    // ── startView ─────────────────────────────────────────

    @Test
    fun `startView calls GlobalRumMonitor startView`() {
        val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
        every { GlobalRumMonitor.get() } returns mockRumMonitor

        DdRum.startView("view-key", "ViewName", mapOf("key" to "value"), 1234567890L)

        verify {
            mockRumMonitor.startView(
                "view-key",
                "ViewName",
                match { attrs ->
                    attrs["key"] == "value" &&
                    attrs[RumAttributes.INTERNAL_TIMESTAMP] == 1234567890L
                }
            )
        }
    }

    @Test
    fun `startView with zero timestamp does not add timestamp attribute`() {
        val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
        every { GlobalRumMonitor.get() } returns mockRumMonitor

        DdRum.startView("view-key", "ViewName", emptyMap(), 0L)

        verify {
            mockRumMonitor.startView(
                "view-key",
                "ViewName",
                match { attrs -> !attrs.containsKey(RumAttributes.INTERNAL_TIMESTAMP) }
            )
        }
    }

    // ── stopView ──────────────────────────────────────────

    @Test
    fun `stopView calls GlobalRumMonitor stopView`() {
        val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
        every { GlobalRumMonitor.get() } returns mockRumMonitor

        DdRum.stopView("view-key", mapOf("key" to "value"), 1234567890L)

        verify {
            mockRumMonitor.stopView(
                "view-key",
                match { attrs ->
                    attrs["key"] == "value" &&
                    attrs[RumAttributes.INTERNAL_TIMESTAMP] == 1234567890L
                }
            )
        }
    }

    // ── startAction ───────────────────────────────────────

    @Test
    fun `startAction calls GlobalRumMonitor startAction`() {
        val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
        every { GlobalRumMonitor.get() } returns mockRumMonitor

        DdRum.startAction("tap", "ButtonTap", mapOf("key" to "value"), 1234567890L)

        verify {
            mockRumMonitor.startAction(
                RumActionType.TAP,
                "ButtonTap",
                match { attrs ->
                    attrs["key"] == "value" &&
                    attrs[RumAttributes.INTERNAL_TIMESTAMP] == 1234567890L
                }
            )
        }
    }

    // ── stopAction ────────────────────────────────────────

    @Test
    fun `stopAction calls GlobalRumMonitor stopAction`() {
        val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
        every { GlobalRumMonitor.get() } returns mockRumMonitor

        DdRum.stopAction("scroll", "ListScroll", mapOf("key" to "value"), 1234567890L)

        verify {
            mockRumMonitor.stopAction(
                RumActionType.SCROLL,
                "ListScroll",
                match { attrs ->
                    attrs["key"] == "value" &&
                    attrs[RumAttributes.INTERNAL_TIMESTAMP] == 1234567890L
                }
            )
        }
    }

    // ── addAction ─────────────────────────────────────────

    @Test
    fun `addAction calls GlobalRumMonitor addAction`() {
        val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
        every { GlobalRumMonitor.get() } returns mockRumMonitor

        DdRum.addAction("click", "ButtonClick", mapOf("key" to "value"), 1234567890L)

        verify {
            mockRumMonitor.addAction(
                RumActionType.CLICK,
                "ButtonClick",
                match { attrs ->
                    attrs["key"] == "value" &&
                    attrs[RumAttributes.INTERNAL_TIMESTAMP] == 1234567890L
                }
            )
        }
    }

    // ── startResource ─────────────────────────────────────

    @Test
    fun `startResource calls GlobalRumMonitor startResource`() {
        val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
        every { GlobalRumMonitor.get() } returns mockRumMonitor

        DdRum.startResource("res-key", "post", "https://example.com/api", mapOf("key" to "value"), 1234567890L)

        verify {
            mockRumMonitor.startResource(
                "res-key",
                RumResourceMethod.POST,
                "https://example.com/api",
                match { attrs ->
                    attrs["key"] == "value" &&
                    attrs[RumAttributes.INTERNAL_TIMESTAMP] == 1234567890L
                }
            )
        }
    }

    // ── stopResource ──────────────────────────────────────

    @Test
    fun `stopResource calls GlobalRumMonitor stopResource with size`() {
        val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
        every { GlobalRumMonitor.get() } returns mockRumMonitor

        DdRum.stopResource("res-key", 200, "xhr", 1024L, mapOf("key" to "value"), 1234567890L)

        verify {
            mockRumMonitor.stopResource(
                "res-key",
                200,
                1024L,
                RumResourceKind.XHR,
                match { attrs ->
                    attrs["key"] == "value" &&
                    attrs[RumAttributes.INTERNAL_TIMESTAMP] == 1234567890L
                }
            )
        }
    }

    @Test
    fun `stopResource with negative size passes null`() {
        val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
        every { GlobalRumMonitor.get() } returns mockRumMonitor

        DdRum.stopResource("res-key", 404, "fetch", -1L, emptyMap(), 0L)

        verify {
            mockRumMonitor.stopResource(
                "res-key",
                404,
                null,
                RumResourceKind.FETCH,
                any()
            )
        }
    }

    // ── addTiming ─────────────────────────────────────────

    @Test
    fun `addTiming calls GlobalRumMonitor addTiming`() {
        val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
        every { GlobalRumMonitor.get() } returns mockRumMonitor

        DdRum.addTiming("page_load")

        verify { mockRumMonitor.addTiming("page_load") }
    }

    // ── addViewLoadingTime ────────────────────────────────

    @Test
    fun `addViewLoadingTime calls GlobalRumMonitor addViewLoadingTime`() {
        val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
        every { GlobalRumMonitor.get() } returns mockRumMonitor

        DdRum.addViewLoadingTime(true)

        verify { mockRumMonitor.addViewLoadingTime(true) }
    }

    @Test
    fun `addViewLoadingTime with false calls GlobalRumMonitor addViewLoadingTime`() {
        val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
        every { GlobalRumMonitor.get() } returns mockRumMonitor

        DdRum.addViewLoadingTime(false)

        verify { mockRumMonitor.addViewLoadingTime(false) }
    }

    // ── stopSession ───────────────────────────────────────

    @Test
    fun `stopSession calls GlobalRumMonitor stopSession`() {
        val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
        every { GlobalRumMonitor.get() } returns mockRumMonitor

        DdRum.stopSession()

        verify { mockRumMonitor.stopSession() }
    }

    // ── addViewAttribute ──────────────────────────────────

    @Test
    fun `addViewAttribute calls GlobalRumMonitor addViewAttributes`() {
        val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
        every { GlobalRumMonitor.get() } returns mockRumMonitor

        DdRum.addViewAttribute("theme", "dark")

        verify { mockRumMonitor.addViewAttributes(mapOf("theme" to "dark")) }
    }

    // ── removeViewAttribute ───────────────────────────────

    @Test
    fun `removeViewAttribute calls GlobalRumMonitor removeViewAttributes`() {
        val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
        every { GlobalRumMonitor.get() } returns mockRumMonitor

        DdRum.removeViewAttribute("theme")

        verify { mockRumMonitor.removeViewAttributes(listOf("theme")) }
    }

    // ── addViewAttributes ─────────────────────────────────

    @Test
    fun `addViewAttributes calls GlobalRumMonitor addViewAttributes`() {
        val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
        every { GlobalRumMonitor.get() } returns mockRumMonitor

        val attrs = mapOf<String, Any?>("theme" to "dark", "locale" to "en")
        DdRum.addViewAttributes(attrs)

        verify { mockRumMonitor.addViewAttributes(attrs) }
    }

    // ── removeViewAttributes ──────────────────────────────

    @Test
    fun `removeViewAttributes calls GlobalRumMonitor removeViewAttributes`() {
        val mockRumMonitor = mockk<RumMonitor>(relaxed = true)
        every { GlobalRumMonitor.get() } returns mockRumMonitor

        DdRum.removeViewAttributes(listOf("theme", "locale"))

        verify { mockRumMonitor.removeViewAttributes(listOf("theme", "locale")) }
    }
}
