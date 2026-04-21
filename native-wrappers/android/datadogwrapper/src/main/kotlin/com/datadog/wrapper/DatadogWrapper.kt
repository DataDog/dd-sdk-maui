package com.datadog.wrapper

import android.content.Context
import android.util.Log
import com.datadog.android.Datadog
import com.datadog.android.DatadogSite
import com.datadog.android._InternalProxy
import com.datadog.android.core.configuration.BatchProcessingLevel
import com.datadog.android.core.configuration.BatchSize
import com.datadog.android.core.configuration.Configuration
import com.datadog.android.core.configuration.UploadFrequency
import com.datadog.android.privacy.TrackingConsent
import com.datadog.android.rum.GlobalRumMonitor

class DatadogWrapper {
    companion object {

        // -- Mapping helpers --

        @JvmStatic
        fun mapSite(site: String): DatadogSite = when (site.lowercase()) {
            "us1" -> DatadogSite.US1
            "us3" -> DatadogSite.US3
            "us5" -> DatadogSite.US5
            "eu1" -> DatadogSite.EU1
            "ap1" -> DatadogSite.AP1
            "ap2" -> DatadogSite.AP2
            "us1_fed" -> DatadogSite.US1_FED
            else -> DatadogSite.US1
        }

        @JvmStatic
        fun mapTrackingConsent(consent: String): TrackingConsent = when (consent.lowercase()) {
            "granted" -> TrackingConsent.GRANTED
            "not_granted" -> TrackingConsent.NOT_GRANTED
            else -> TrackingConsent.PENDING
        }

        @JvmStatic
        fun mapVerbosity(verbosity: String): Int = when (verbosity.lowercase()) {
            "debug" -> Log.DEBUG
            "info" -> Log.INFO
            "warn" -> Log.WARN
            "error" -> Log.ERROR
            else -> Log.ERROR
        }

        @JvmStatic
        fun mapBatchSize(batchSize: String): BatchSize = when (batchSize.lowercase()) {
            "small" -> BatchSize.SMALL
            "large" -> BatchSize.LARGE
            else -> BatchSize.MEDIUM
        }

        @JvmStatic
        fun mapUploadFrequency(uploadFrequency: String): UploadFrequency = when (uploadFrequency.lowercase()) {
            "frequent" -> UploadFrequency.FREQUENT
            "rare" -> UploadFrequency.RARE
            else -> UploadFrequency.AVERAGE
        }

        @JvmStatic
        fun mapBatchProcessingLevel(level: String): BatchProcessingLevel = when (level.lowercase()) {
            "low" -> BatchProcessingLevel.LOW
            "high" -> BatchProcessingLevel.HIGH
            else -> BatchProcessingLevel.MEDIUM
        }

        // -- Initialization --

        @JvmStatic
        fun initialize(
            context: Context,
            clientToken: String,
            environment: String,
            service: String?,
            site: String = "us1",
            verbosity: String = "error",
            trackingConsent: String = "pending",
            batchSize: String? = null,
            uploadFrequency: String? = null,
            batchProcessingLevel: String? = null,
            additionalConfiguration: Map<String, Any>? = null
        ): Boolean {
            return try {
                val builder = Configuration.Builder(
                    clientToken = clientToken,
                    env = environment,
                    service = service
                )
                    .useSite(mapSite(site))

                batchSize?.let {
                    builder.setBatchSize(mapBatchSize(it))
                }

                uploadFrequency?.let {
                    builder.setUploadFrequency(mapUploadFrequency(it))
                }

                batchProcessingLevel?.let {
                    builder.setBatchProcessingLevel(mapBatchProcessingLevel(it))
                }

                additionalConfiguration?.let {
                    builder.setAdditionalConfiguration(it)
                }

                if (additionalConfiguration?.get("_dd.needsClearTextHttp") == true) {
                    _InternalProxy.allowClearTextHttp(builder)
                }

                Datadog.initialize(context, builder.build(), mapTrackingConsent(trackingConsent))

                Datadog.setVerbosity(mapVerbosity(verbosity))

                true
            } catch (e: Exception) {
                e.printStackTrace()
                false
            }
        }

        // -- Tracking Consent --

        @JvmStatic
        fun setTrackingConsent(consent: String) {
            Datadog.setTrackingConsent(mapTrackingConsent(consent))
        }

        // -- Global Attributes --

        @JvmStatic
        fun addAttribute(key: String, value: Any?) {
            GlobalRumMonitor.get().addAttribute(key, value)
        }

        @JvmStatic
        fun addAttributes(attributes: Map<String, Any?>) {
            val monitor = GlobalRumMonitor.get()
            for ((key, value) in attributes) {
                monitor.addAttribute(key, value)
            }
        }

        @JvmStatic
        fun removeAttribute(key: String) {
            GlobalRumMonitor.get().removeAttribute(key)
        }

        @JvmStatic
        fun removeAttributes(keys: List<String>) {
            val monitor = GlobalRumMonitor.get()
            for (key in keys) {
                monitor.removeAttribute(key)
            }
        }

        // -- User Info --

        @JvmStatic
        fun setUserInfo(id: String, name: String?, email: String?, extraInfo: Map<String, Any?>) {
            Datadog.setUserInfo(id, name, email, extraInfo)
        }

        @JvmStatic
        fun addUserExtraInfo(extraInfo: Map<String, Any?>) {
            Datadog.addUserProperties(extraInfo)
        }

        @JvmStatic
        fun clearUserInfo() {
            Datadog.clearUserInfo()
        }

        // -- Account Info --

        @JvmStatic
        fun setAccountInfo(id: String, name: String?, extraInfo: Map<String, Any?>) {
            Datadog.setAccountInfo(id, name, extraInfo)
        }

        @JvmStatic
        fun addAccountExtraInfo(extraInfo: Map<String, Any?>) {
            Datadog.addAccountExtraInfo(extraInfo)
        }

        @JvmStatic
        fun clearAccountInfo() {
            Datadog.clearAccountInfo()
        }
    }
}
