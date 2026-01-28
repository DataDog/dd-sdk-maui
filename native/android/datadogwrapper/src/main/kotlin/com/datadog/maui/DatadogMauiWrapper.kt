package com.datadog.maui

import android.content.Context
import com.datadog.android.Datadog
import com.datadog.android.DatadogSite
import com.datadog.android.core.configuration.BatchSize
import com.datadog.android.core.configuration.Configuration
import com.datadog.android.core.configuration.UploadFrequency
import com.datadog.android.privacy.TrackingConsent
import com.datadog.android.rum.GlobalRumMonitor
import com.datadog.android.rum.Rum
import com.datadog.android.rum.RumActionType
import com.datadog.android.rum.RumConfiguration
import com.datadog.android.rum.RumErrorSource
import com.datadog.android.rum.RumResourceKind
import com.datadog.android.rum.RumResourceMethod
import com.datadog.android.ndk.NdkCrashReports

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
            android.util.Log.i("DatadogMauiWrapper", "Initialize called with site=$site, clientToken=${clientToken.take(10)}...")
            val datadogSite = mapSite(site)
            android.util.Log.i("DatadogMauiWrapper", "Mapped site to: $datadogSite")
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

            // Enable verbose logging to debug upload issues
            Datadog.setVerbosity(android.util.Log.VERBOSE)

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

    /**
     * Enables native NDK crash reporting.
     * Must be called after SDK initialization.
     *
     * @return true if crash reporting was enabled successfully, false otherwise
     */
    @JvmStatic
    fun enableCrashReporting(): Boolean {
        if (!initialized) {
            android.util.Log.e("DatadogMauiWrapper", "Cannot enable crash reporting: SDK not initialized")
            return false
        }
        return try {
            NdkCrashReports.enable()
            android.util.Log.i("DatadogMauiWrapper", "Crash reporting enabled")
            true
        } catch (e: Exception) {
            android.util.Log.e("DatadogMauiWrapper", "Failed to enable crash reporting", e)
            false
        }
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
            "STAGING" -> DatadogSite.STAGING
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

    // ==================== RUM Methods ====================

    private var rumEnabled = false

    /**
     * Enables RUM (Real User Monitoring) with the provided application ID.
     * Must be called after SDK initialization.
     *
     * @param applicationId The RUM application ID from Datadog
     * @param sampleRate The sample rate for RUM sessions (0.0 to 100.0)
     * @return true if RUM was enabled successfully, false otherwise
     */
    @JvmStatic
    fun enableRum(applicationId: String, sampleRate: Float): Boolean {
        if (!initialized) {
            android.util.Log.e("DatadogMauiWrapper", "Cannot enable RUM: SDK not initialized")
            return false
        }
        if (rumEnabled) {
            android.util.Log.w("DatadogMauiWrapper", "RUM is already enabled")
            return true
        }

        return try {
            val rumConfig = RumConfiguration.Builder(applicationId)
                .setSessionSampleRate(sampleRate)
                .trackUserInteractions()
                .trackLongTasks()
                .build()

            Rum.enable(rumConfig)
            rumEnabled = true
            true
        } catch (e: Exception) {
            android.util.Log.e("DatadogMauiWrapper", "Failed to enable RUM", e)
            false
        }
    }

    /**
     * Checks if RUM is enabled.
     *
     * @return true if RUM is enabled, false otherwise
     */
    @JvmStatic
    fun isRumEnabled(): Boolean = rumEnabled

    /**
     * Starts tracking a view.
     *
     * @param key Unique identifier for the view
     * @param name Human-readable name for the view
     * @param attributes Optional custom attributes
     */
    @JvmStatic
    fun startView(key: String, name: String, attributes: Map<String, Any>?) {
        if (!rumEnabled) return
        GlobalRumMonitor.get().startView(key, name, attributes ?: emptyMap())
    }

    /**
     * Stops tracking a view.
     *
     * @param key Unique identifier for the view
     * @param attributes Optional custom attributes
     */
    @JvmStatic
    fun stopView(key: String, attributes: Map<String, Any>?) {
        if (!rumEnabled) return
        GlobalRumMonitor.get().stopView(key, attributes ?: emptyMap())
    }

    /**
     * Adds a user action event.
     *
     * @param type Action type string ("TAP", "CLICK", "SCROLL", "SWIPE", "CUSTOM")
     * @param name Human-readable name for the action
     * @param attributes Optional custom attributes
     */
    @JvmStatic
    fun addAction(type: String, name: String, attributes: Map<String, Any>?) {
        if (!rumEnabled) return
        val actionType = mapActionType(type)
        GlobalRumMonitor.get().addAction(actionType, name, attributes ?: emptyMap())
    }

    /**
     * Adds an error event.
     *
     * @param message Error message
     * @param source Error source string ("SOURCE", "NETWORK", "WEBVIEW", "CONSOLE", "CUSTOM")
     * @param stackTrace Optional stack trace
     * @param attributes Optional custom attributes
     */
    @JvmStatic
    fun addError(message: String, source: String, stackTrace: String?, attributes: Map<String, Any>?) {
        if (!rumEnabled) return
        val errorSource = mapErrorSource(source)
        GlobalRumMonitor.get().addErrorWithStacktrace(
            message,
            errorSource,
            stackTrace ?: "",
            attributes ?: emptyMap()
        )
    }

    /**
     * Starts tracking a resource request.
     *
     * @param key Unique identifier for the resource
     * @param httpMethod HTTP method (e.g., "GET", "POST")
     * @param url The URL of the resource
     * @param attributes Optional custom attributes
     */
    @JvmStatic
    fun startResource(key: String, httpMethod: String, url: String, attributes: Map<String, Any>?) {
        if (!rumEnabled) return
        val method = mapHttpMethod(httpMethod)
        GlobalRumMonitor.get().startResource(key, method, url, attributes ?: emptyMap())
    }

    /**
     * Stops tracking a resource request that completed successfully.
     *
     * @param key Unique identifier for the resource
     * @param statusCode HTTP status code
     * @param size Response size in bytes (-1 if unknown)
     * @param attributes Optional custom attributes
     */
    @JvmStatic
    fun stopResource(key: String, statusCode: Int, size: Long, attributes: Map<String, Any>?) {
        if (!rumEnabled) return
        val kind = inferResourceKind(statusCode)
        val sizeValue = if (size >= 0) size else null
        GlobalRumMonitor.get().stopResource(key, statusCode, sizeValue, kind, attributes ?: emptyMap())
    }

    /**
     * Stops tracking a resource request that failed with an error.
     *
     * @param key Unique identifier for the resource
     * @param message Error message
     * @param attributes Optional custom attributes
     */
    @JvmStatic
    fun stopResourceWithError(key: String, message: String, attributes: Map<String, Any>?) {
        if (!rumEnabled) return
        GlobalRumMonitor.get().stopResourceWithError(
            key,
            null, // statusCode
            message,
            RumErrorSource.NETWORK,
            Exception(message), // throwable
            attributes ?: emptyMap()
        )
    }

    private fun mapActionType(type: String): RumActionType {
        return when (type.uppercase()) {
            "TAP" -> RumActionType.TAP
            "CLICK" -> RumActionType.CLICK
            "SCROLL" -> RumActionType.SCROLL
            "SWIPE" -> RumActionType.SWIPE
            "CUSTOM" -> RumActionType.CUSTOM
            else -> RumActionType.CUSTOM
        }
    }

    private fun mapErrorSource(source: String): RumErrorSource {
        return when (source.uppercase()) {
            "SOURCE" -> RumErrorSource.SOURCE
            "NETWORK" -> RumErrorSource.NETWORK
            "WEBVIEW" -> RumErrorSource.WEBVIEW
            "CONSOLE" -> RumErrorSource.CONSOLE
            "CUSTOM" -> RumErrorSource.SOURCE // CUSTOM maps to SOURCE as closest match
            else -> RumErrorSource.SOURCE
        }
    }

    private fun mapHttpMethod(method: String): RumResourceMethod {
        return when (method.uppercase()) {
            "GET" -> RumResourceMethod.GET
            "POST" -> RumResourceMethod.POST
            "PUT" -> RumResourceMethod.PUT
            "DELETE" -> RumResourceMethod.DELETE
            "PATCH" -> RumResourceMethod.PATCH
            "HEAD" -> RumResourceMethod.HEAD
            "OPTIONS" -> RumResourceMethod.OPTIONS
            "TRACE" -> RumResourceMethod.TRACE
            "CONNECT" -> RumResourceMethod.CONNECT
            else -> RumResourceMethod.GET
        }
    }

    private fun inferResourceKind(statusCode: Int): RumResourceKind {
        // Default to OTHER for simplicity; can be refined based on content-type in the future
        return RumResourceKind.OTHER
    }
}
