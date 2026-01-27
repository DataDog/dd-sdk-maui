package com.datadog.wrapper

import android.content.Context
import com.datadog.android.Datadog
import com.datadog.android.DatadogSite
import com.datadog.android.core.configuration.Configuration
import com.datadog.android.privacy.TrackingConsent

class DatadogWrapper {
    companion object {
        @JvmStatic
        fun initialize(
            context: Context,
            clientToken: String,
            environment: String,
            service: String,
            site: String = "us1"
        ): Boolean {
            return try {
                val datadogSite = when (site.lowercase()) {
                    "us1" -> DatadogSite.US1
                    "us3" -> DatadogSite.US3
                    "us5" -> DatadogSite.US5
                    "eu1" -> DatadogSite.EU1
                    "ap1" -> DatadogSite.AP1
                    "us1_fed" -> DatadogSite.US1_FED
                    else -> DatadogSite.US1
                }

                val configuration = Configuration.Builder(
                    clientToken = clientToken,
                    env = environment,
                    service = service
                )
                    .useSite(datadogSite)
                    .build()

                Datadog.initialize(context, configuration, TrackingConsent.GRANTED)
                true
            } catch (e: Exception) {
                e.printStackTrace()
                false
            }
        }
    }
}
