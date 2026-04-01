package com.datadog.wrapper

import android.util.Log
import com.datadog.android.Datadog
import com.datadog.android.rum.GlobalRumMonitor
import com.datadog.android.rum.Rum
import com.datadog.android.rum.RumConfiguration
import com.datadog.android.rum.RumErrorSource
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
    }
}
