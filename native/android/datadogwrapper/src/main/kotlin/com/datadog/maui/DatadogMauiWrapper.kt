package com.datadog.maui

import android.content.Context
import com.datadog.android.Datadog
import com.datadog.android.DatadogSite
import com.datadog.android.core.configuration.BatchSize
import com.datadog.android.core.configuration.Configuration
import com.datadog.android.core.configuration.UploadFrequency
import com.datadog.android.privacy.TrackingConsent

/**
 * Wrapper object providing JVM-static methods for .NET MAUI binding integration.
 *
 * This wrapper simplifies the Datadog Android SDK API for consumption from .NET,
 * handling enum conversions and providing a straightforward initialization interface.
 */
object DatadogMauiWrapper {

    private var initialized = false

    /**
     * Initializes the Datadog SDK with the provided configuration.
     *
     * @param context Android application context
     * @param clientToken Datadog client token
     * @param env Environment name (e.g., "production", "staging")
     * @param site Datadog site string ("US1", "US3", "US5", "EU1", "AP1", "AP2", "US1_FED")
     * @param service Optional service name
     * @param trackingConsent Tracking consent string ("GRANTED", "NOT_GRANTED", "PENDING")
     * @param batchSize Batch size string ("SMALL", "MEDIUM", "LARGE")
     * @param uploadFrequency Upload frequency string ("FREQUENT", "AVERAGE", "RARE")
     * @return true if initialization succeeded, false otherwise
     */
    @JvmStatic
    fun initialize(
        context: Context,
        clientToken: String,
        env: String,
        site: String,
        service: String?,
        trackingConsent: String,
        batchSize: String,
        uploadFrequency: String
    ): Boolean {
        if (initialized) {
            return false
        }

        try {
            val datadogSite = mapSite(site)
            val consent = mapTrackingConsent(trackingConsent)
            val batch = mapBatchSize(batchSize)
            val frequency = mapUploadFrequency(uploadFrequency)

            val configBuilder = Configuration.Builder(
                clientToken = clientToken,
                env = env,
                variant = "",
                service = service
            )
                .useSite(datadogSite)
                .setBatchSize(batch)
                .setUploadFrequency(frequency)

            val configuration = configBuilder.build()

            Datadog.initialize(context, configuration, consent)
            initialized = true
            return true
        } catch (e: Exception) {
            android.util.Log.e("DatadogMauiWrapper", "Failed to initialize Datadog SDK", e)
            return false
        }
    }

    /**
     * Checks if the Datadog SDK has been initialized.
     *
     * @return true if initialized, false otherwise
     */
    @JvmStatic
    fun isInitialized(): Boolean {
        return initialized && Datadog.isInitialized()
    }

    private fun mapSite(site: String): DatadogSite {
        return when (site.uppercase()) {
            "US1" -> DatadogSite.US1
            "US3" -> DatadogSite.US3
            "US5" -> DatadogSite.US5
            "EU1" -> DatadogSite.EU1
            "AP1" -> DatadogSite.AP1
            "AP2" -> DatadogSite.AP1 // AP2 not available in current SDK, fallback to AP1
            "US1_FED" -> DatadogSite.US1_FED
            else -> DatadogSite.US1
        }
    }

    private fun mapTrackingConsent(consent: String): TrackingConsent {
        return when (consent.uppercase()) {
            "GRANTED" -> TrackingConsent.GRANTED
            "NOT_GRANTED" -> TrackingConsent.NOT_GRANTED
            "PENDING" -> TrackingConsent.PENDING
            else -> TrackingConsent.PENDING
        }
    }

    private fun mapBatchSize(size: String): BatchSize {
        return when (size.uppercase()) {
            "SMALL" -> BatchSize.SMALL
            "MEDIUM" -> BatchSize.MEDIUM
            "LARGE" -> BatchSize.LARGE
            else -> BatchSize.MEDIUM
        }
    }

    private fun mapUploadFrequency(frequency: String): UploadFrequency {
        return when (frequency.uppercase()) {
            "FREQUENT" -> UploadFrequency.FREQUENT
            "AVERAGE" -> UploadFrequency.AVERAGE
            "RARE" -> UploadFrequency.RARE
            else -> UploadFrequency.AVERAGE
        }
    }
}
