package com.datadog.wrapper

import android.util.Log
import com.datadog.android.Datadog
import com.datadog.android.rum.GlobalRumMonitor
import com.datadog.android.rum.Rum
import com.datadog.android.rum.RumConfiguration
import com.datadog.android.rum.RumActionType
import com.datadog.android.rum.RumAttributes
import com.datadog.android.rum.RumErrorSource
import com.datadog.android.rum.RumResourceKind
import com.datadog.android.rum.RumResourceMethod
import com.datadog.android.rum.tracking.ActivityViewTrackingStrategy
import com.datadog.android.rum.configuration.VitalsUpdateFrequency

class DdRum {
    companion object {
        // Stored for future resource tracking configuration
        var initialResourceThreshold: Double? = null
            private set

        // For testing: reset static state between tests
        internal fun resetForTesting() {
            initialResourceThreshold = null
        }

        // -- Mapping helpers --

        @JvmStatic
        fun mapVitalsUpdateFrequency(frequency: String): VitalsUpdateFrequency =
            when (frequency.lowercase()) {
                "never" -> VitalsUpdateFrequency.NEVER
                "rare" -> VitalsUpdateFrequency.RARE
                "average" -> VitalsUpdateFrequency.AVERAGE
                "frequent" -> VitalsUpdateFrequency.FREQUENT
                else -> VitalsUpdateFrequency.AVERAGE
            }

        @JvmStatic
        fun mapErrorSource(source: String): RumErrorSource =
            when (source.lowercase()) {
                "network" -> RumErrorSource.NETWORK
                "source" -> RumErrorSource.SOURCE
                "console" -> RumErrorSource.CONSOLE
                "webview" -> RumErrorSource.WEBVIEW
                else -> RumErrorSource.CUSTOM
            }

        @JvmStatic
        fun mapActionType(type: String): RumActionType =
            when (type.lowercase()) {
                "tap" -> RumActionType.TAP
                "scroll" -> RumActionType.SCROLL
                "swipe" -> RumActionType.SWIPE
                "click" -> RumActionType.CLICK
                "back" -> RumActionType.BACK
                else -> RumActionType.CUSTOM
            }

        @JvmStatic
        fun mapResourceMethod(method: String): RumResourceMethod =
            when (method.lowercase()) {
                "post" -> RumResourceMethod.POST
                "put" -> RumResourceMethod.PUT
                "delete" -> RumResourceMethod.DELETE
                "head" -> RumResourceMethod.HEAD
                "patch" -> RumResourceMethod.PATCH
                "connect" -> RumResourceMethod.CONNECT
                "trace" -> RumResourceMethod.TRACE
                "options" -> RumResourceMethod.OPTIONS
                else -> RumResourceMethod.GET
            }

        @JvmStatic
        fun mapResourceKind(kind: String): RumResourceKind =
            when (kind.lowercase()) {
                "xhr" -> RumResourceKind.XHR
                "native" -> RumResourceKind.NATIVE
                "fetch" -> RumResourceKind.FETCH
                "document" -> RumResourceKind.DOCUMENT
                "beacon" -> RumResourceKind.BEACON
                "image" -> RumResourceKind.IMAGE
                "font" -> RumResourceKind.FONT
                "css" -> RumResourceKind.CSS
                "media" -> RumResourceKind.MEDIA
                "js" -> RumResourceKind.JS
                else -> RumResourceKind.OTHER
            }

        // -- Enable --

        @JvmStatic
        fun enableRum(configuration: Map<String, Any?>) {
            try {
                val applicationId = configuration["applicationId"] as? String
                if (applicationId.isNullOrEmpty()) {
                    Log.w("DatadogWrapper", "DdRum.enableRum: applicationId is required.")
                    return
                }

                val builder = RumConfiguration.Builder(applicationId)

                // Session sample rate
                (configuration["sessionSampleRate"] as? Number)?.let {
                    builder.setSessionSampleRate(it.toFloat())
                }

                // Telemetry sample rate
                (configuration["telemetrySampleRate"] as? Number)?.let {
                    builder.setTelemetrySampleRate(it.toFloat())
                }

                // Vitals update frequency
                (configuration["vitalsUpdateFrequency"] as? String)?.let {
                    builder.setVitalsUpdateFrequency(mapVitalsUpdateFrequency(it))
                }

                // Native view tracking
                if (configuration["nativeViewTracking"] == true) {
                    builder.useViewTrackingStrategy(ActivityViewTrackingStrategy(false))
                }

                // Native interaction tracking
                if (configuration["nativeInteractionTracking"] == true) {
                    builder.trackUserInteractions()
                }

                // Long task threshold
                (configuration["nativeLongTaskThresholdMs"] as? Number)?.let {
                    builder.trackLongTasks(it.toLong())
                }

                // Track frustrations
                (configuration["trackFrustrations"] as? Boolean)?.let {
                    builder.trackFrustrations(it)
                }

                // Track background events
                (configuration["trackBackgroundEvents"] as? Boolean)?.let {
                    builder.trackBackgroundEvents(it)
                }

                // Track non-fatal ANRs (Android only)
                (configuration["trackNonFatalAnrs"] as? Boolean)?.let {
                    builder.trackNonFatalAnrs(it)
                }

                // Custom endpoint
                (configuration["customEndpoint"] as? String)?.let {
                    if (it.isNotBlank()) {
                        builder.useCustomEndpoint(it)
                    }
                }

                // Initial resource threshold (stored for resource tracking configuration)
                initialResourceThreshold = (configuration["initialResourceThreshold"] as? Number)?.toDouble()

                val rumConfig = builder.build()
                Rum.enable(rumConfig, Datadog.getInstance())
            } catch (e: Exception) {
                Log.e("DatadogWrapper", "DdRum.enableRum failed", e)
            }
        }

        // -- Add Error --

        @JvmStatic
        fun addError(
            message: String,
            source: String,
            stacktrace: String,
            context: Map<String, Any?>,
            timestampMs: Long
        ) {
            try {
                val attributes = context.toMutableMap()
                if (timestampMs > 0) {
                    attributes["_dd.timestamp"] = timestampMs
                }
                attributes["_dd.error.source_type"] = "maui"

                GlobalRumMonitor.get().addErrorWithStacktrace(
                    message = message,
                    source = mapErrorSource(source),
                    stacktrace = stacktrace,
                    attributes = attributes
                )
            } catch (e: Exception) {
                Log.e("DatadogWrapper", "DdRum.addError failed", e)
            }
        }

        // -- Views --

        @JvmStatic
        fun startView(key: String, name: String, context: Map<String, Any?>, timestampMs: Long) {
            try {
                val attributes = context.toMutableMap()
                if (timestampMs > 0) attributes[RumAttributes.INTERNAL_TIMESTAMP] = timestampMs
                GlobalRumMonitor.get().startView(key, name, attributes)
            } catch (e: Exception) {
                Log.e("DatadogWrapper", "DdRum.startView failed", e)
            }
        }

        @JvmStatic
        fun stopView(key: String, context: Map<String, Any?>, timestampMs: Long) {
            try {
                val attributes = context.toMutableMap()
                if (timestampMs > 0) attributes[RumAttributes.INTERNAL_TIMESTAMP] = timestampMs
                GlobalRumMonitor.get().stopView(key, attributes)
            } catch (e: Exception) {
                Log.e("DatadogWrapper", "DdRum.stopView failed", e)
            }
        }

        // -- Actions --

        @JvmStatic
        fun startAction(type: String, name: String, context: Map<String, Any?>, timestampMs: Long) {
            try {
                val attributes = context.toMutableMap()
                if (timestampMs > 0) attributes[RumAttributes.INTERNAL_TIMESTAMP] = timestampMs
                GlobalRumMonitor.get().startAction(mapActionType(type), name, attributes)
            } catch (e: Exception) {
                Log.e("DatadogWrapper", "DdRum.startAction failed", e)
            }
        }

        @JvmStatic
        fun stopAction(type: String, name: String, context: Map<String, Any?>, timestampMs: Long) {
            try {
                val attributes = context.toMutableMap()
                if (timestampMs > 0) attributes[RumAttributes.INTERNAL_TIMESTAMP] = timestampMs
                GlobalRumMonitor.get().stopAction(mapActionType(type), name, attributes)
            } catch (e: Exception) {
                Log.e("DatadogWrapper", "DdRum.stopAction failed", e)
            }
        }

        @JvmStatic
        fun addAction(type: String, name: String, context: Map<String, Any?>, timestampMs: Long) {
            try {
                val attributes = context.toMutableMap()
                if (timestampMs > 0) attributes[RumAttributes.INTERNAL_TIMESTAMP] = timestampMs
                GlobalRumMonitor.get().addAction(mapActionType(type), name, attributes)
            } catch (e: Exception) {
                Log.e("DatadogWrapper", "DdRum.addAction failed", e)
            }
        }

        // -- Resources --

        @JvmStatic
        fun startResource(key: String, method: String, url: String, context: Map<String, Any?>, timestampMs: Long) {
            try {
                val attributes = context.toMutableMap()
                if (timestampMs > 0) attributes[RumAttributes.INTERNAL_TIMESTAMP] = timestampMs
                GlobalRumMonitor.get().startResource(key, mapResourceMethod(method), url, attributes)
            } catch (e: Exception) {
                Log.e("DatadogWrapper", "DdRum.startResource failed", e)
            }
        }

        @JvmStatic
        fun stopResource(key: String, statusCode: Int, kind: String, size: Long, context: Map<String, Any?>, timestampMs: Long) {
            try {
                val attributes = context.toMutableMap()
                if (timestampMs > 0) attributes[RumAttributes.INTERNAL_TIMESTAMP] = timestampMs
                val resourceSize = if (size >= 0) size else null
                GlobalRumMonitor.get().stopResource(key, statusCode, resourceSize, mapResourceKind(kind), attributes)
            } catch (e: Exception) {
                Log.e("DatadogWrapper", "DdRum.stopResource failed", e)
            }
        }

        // -- Timing --

        @JvmStatic
        fun addTiming(name: String) {
            try {
                GlobalRumMonitor.get().addTiming(name)
            } catch (e: Exception) {
                Log.e("DatadogWrapper", "DdRum.addTiming failed", e)
            }
        }

        @JvmStatic
        fun addViewLoadingTime(overwrite: Boolean) {
            try {
                GlobalRumMonitor.get().addViewLoadingTime(overwrite)
            } catch (e: Exception) {
                Log.e("DatadogWrapper", "DdRum.addViewLoadingTime failed", e)
            }
        }

        // -- Session --

        @JvmStatic
        fun stopSession() {
            try {
                GlobalRumMonitor.get().stopSession()
            } catch (e: Exception) {
                Log.e("DatadogWrapper", "DdRum.stopSession failed", e)
            }
        }

        // -- View Attributes --

        @JvmStatic
        fun addViewAttribute(key: String, value: Any?) {
            try {
                GlobalRumMonitor.get().addViewAttributes(mapOf(key to value))
            } catch (e: Exception) {
                Log.e("DatadogWrapper", "DdRum.addViewAttribute failed", e)
            }
        }

        @JvmStatic
        fun removeViewAttribute(key: String) {
            try {
                GlobalRumMonitor.get().removeViewAttributes(listOf(key))
            } catch (e: Exception) {
                Log.e("DatadogWrapper", "DdRum.removeViewAttribute failed", e)
            }
        }

        @JvmStatic
        fun addViewAttributes(attributes: Map<String, Any?>) {
            try {
                GlobalRumMonitor.get().addViewAttributes(attributes)
            } catch (e: Exception) {
                Log.e("DatadogWrapper", "DdRum.addViewAttributes failed", e)
            }
        }

        @JvmStatic
        fun removeViewAttributes(keys: List<String>) {
            try {
                GlobalRumMonitor.get().removeViewAttributes(keys)
            } catch (e: Exception) {
                Log.e("DatadogWrapper", "DdRum.removeViewAttributes failed", e)
            }
        }
    }
}
