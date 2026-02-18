package com.datadog.wrapper

import android.content.Context
import android.util.Log
import com.datadog.android.Datadog
import com.datadog.android.DatadogSite
import com.datadog.android.core.configuration.BatchProcessingLevel
import com.datadog.android.core.configuration.BatchSize
import com.datadog.android.core.configuration.Configuration
import com.datadog.android.core.configuration.UploadFrequency
import com.datadog.android.privacy.TrackingConsent

class DatadogWrapper {
    companion object {
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
                val datadogSite = when (site.lowercase()) {
                    "us1" -> DatadogSite.US1
                    "us3" -> DatadogSite.US3
                    "us5" -> DatadogSite.US5
                    "eu1" -> DatadogSite.EU1
                    "ap1" -> DatadogSite.AP1
                    "ap2" -> DatadogSite.AP2
                    "us1_fed" -> DatadogSite.US1_FED
                    else -> DatadogSite.US1
                }

                val builder = Configuration.Builder(
                    clientToken = clientToken,
                    env = environment,
                    service = service
                )
                    .useSite(datadogSite)

                batchSize?.let {
                    builder.setBatchSize(when (it.lowercase()) {
                        "small" -> BatchSize.SMALL
                        "large" -> BatchSize.LARGE
                        else -> BatchSize.MEDIUM
                    })
                }

                uploadFrequency?.let {
                    builder.setUploadFrequency(when (it.lowercase()) {
                        "frequent" -> UploadFrequency.FREQUENT
                        "rare" -> UploadFrequency.RARE
                        else -> UploadFrequency.AVERAGE
                    })
                }

                batchProcessingLevel?.let {
                    builder.setBatchProcessingLevel(when (it.lowercase()) {
                        "low" -> BatchProcessingLevel.LOW
                        "high" -> BatchProcessingLevel.HIGH
                        else -> BatchProcessingLevel.MEDIUM
                    })
                }

                additionalConfiguration?.let {
                    builder.setAdditionalConfiguration(it)
                }

                val consent = when (trackingConsent.lowercase()) {
                    "granted" -> TrackingConsent.GRANTED
                    "not_granted" -> TrackingConsent.NOT_GRANTED
                    else -> TrackingConsent.PENDING
                }

                Datadog.initialize(context, builder.build(), consent)

                // Set SDK verbosity level
                Datadog.setVerbosity(when (verbosity.lowercase()) {
                    "debug" -> Log.DEBUG
                    "info" -> Log.INFO
                    "warn" -> Log.WARN
                    "error" -> Log.ERROR
                    else -> Log.ERROR
                })

                true
            } catch (e: Exception) {
                e.printStackTrace()
                false
            }
        }
    }
}
