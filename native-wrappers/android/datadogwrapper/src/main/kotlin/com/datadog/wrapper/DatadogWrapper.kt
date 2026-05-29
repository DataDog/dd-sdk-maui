/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

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
import com.datadog.android.ndk.NdkCrashReports
import com.datadog.android.privacy.TrackingConsent
import com.datadog.android.rum.GlobalRumMonitor
import com.datadog.android.trace.TracingHeaderType
import java.net.InetSocketAddress
import java.util.Locale
import java.net.Proxy
import okhttp3.Authenticator
import okhttp3.Credentials
import okhttp3.Request
import okhttp3.Response
import okhttp3.Route

class DatadogWrapper {
    companion object {

        private const val TAG = "DatadogWrapper"
        private const val PROXY_AUTHORIZATION_REQUIRED_STATUS_CODE = 407

        // -- Mapping helpers --

        @JvmStatic
        fun mapSite(site: String): DatadogSite = when (site.lowercase(Locale.US)) {
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
        fun mapTrackingConsent(consent: String): TrackingConsent = when (consent.lowercase(Locale.US)) {
            "granted" -> TrackingConsent.GRANTED
            "not_granted" -> TrackingConsent.NOT_GRANTED
            else -> TrackingConsent.PENDING
        }

        @JvmStatic
        fun mapVerbosity(verbosity: String): Int = when (verbosity.lowercase(Locale.US)) {
            "debug" -> Log.DEBUG
            "info" -> Log.INFO
            "warn" -> Log.WARN
            "error" -> Log.ERROR
            else -> Log.ERROR
        }

        @JvmStatic
        fun mapBatchSize(batchSize: String): BatchSize = when (batchSize.lowercase(Locale.US)) {
            "small" -> BatchSize.SMALL
            "large" -> BatchSize.LARGE
            else -> BatchSize.MEDIUM
        }

        @JvmStatic
        fun mapUploadFrequency(uploadFrequency: String): UploadFrequency = when (uploadFrequency.lowercase(Locale.US)) {
            "frequent" -> UploadFrequency.FREQUENT
            "rare" -> UploadFrequency.RARE
            else -> UploadFrequency.AVERAGE
        }

        @JvmStatic
        fun mapBatchProcessingLevel(level: String): BatchProcessingLevel = when (level.lowercase(Locale.US)) {
            "low" -> BatchProcessingLevel.LOW
            "high" -> BatchProcessingLevel.HIGH
            else -> BatchProcessingLevel.MEDIUM
        }

        @JvmStatic
        fun mapProxyConfiguration(config: Map<String, Any>?): Pair<Proxy, Authenticator?>? {
            if (config == null) return null

            val type = (config["type"] as? String)?.lowercase(Locale.US) ?: return null
            val address = config["address"] as? String ?: return null
            val port = (config["port"] as? Number)?.toInt() ?: return null

            val proxyType = when (type) {
                "http", "https" -> Proxy.Type.HTTP
                "socks" -> Proxy.Type.SOCKS
                else -> return null
            }

            val proxy = Proxy(proxyType, InetSocketAddress(address, port))

            val username = config["username"] as? String
            val password = config["password"] as? String

            val authenticator: Authenticator? = if (username != null && password != null) {
                @Suppress("DEPRECATION_ERROR")
                Authenticator { _: Route?, response: Response ->
                    val proxyAuthorization = response.code() == PROXY_AUTHORIZATION_REQUIRED_STATUS_CODE

                    if (!proxyAuthorization) {
                        Log.w(
                            TAG,
                            "Unexpected response code=${response.code()}" +
                                " received during proxy authentication request."
                        )
                        return@Authenticator null
                    }

                    val challenges = response.challenges()
                    for (challenge in challenges) {
                        val scheme = challenge.scheme()
                        if ("Basic".equals(scheme, ignoreCase = true) ||
                            "OkHttp-Preemptive".equals(scheme, ignoreCase = true)
                        ) {
                            val credential = Credentials.basic(
                                username,
                                password,
                                challenge.charset()
                            )
                            return@Authenticator response.request().newBuilder()
                                .header("Proxy-Authorization", credential)
                                .build()
                        }
                    }

                    Log.w(
                        TAG,
                        "No known challenges are satisfied during proxy authentication request."
                    )
                    null
                }
            } else {
                null
            }

            return Pair(proxy, authenticator)
        }

        // -- Initialization --

        @JvmStatic
        fun mapTracingHeaderType(type: String): TracingHeaderType = when (type.lowercase(Locale.US)) {
            "b3" -> TracingHeaderType.B3
            "b3multi" -> TracingHeaderType.B3MULTI
            "tracecontext" -> TracingHeaderType.TRACECONTEXT
            else -> TracingHeaderType.DATADOG
        }

        // Parses a flat dictionary of host -> comma-separated header types
        // into the format expected by the Datadog SDK.
        @JvmStatic
        fun parseFirstPartyHosts(hosts: Map<String, Any?>): Map<String, Set<TracingHeaderType>> {
            val result = mutableMapOf<String, Set<TracingHeaderType>>()
            for ((host, value) in hosts) {
                val headerTypesStr = value?.toString() ?: continue
                val types = headerTypesStr.split(",")
                    .map { it.trim() }
                    .filter { it.isNotEmpty() }
                    .map { mapTracingHeaderType(it) }
                    .toSet()
                if (types.isNotEmpty()) {
                    result[host] = types
                }
            }
            return result
        }

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
            proxyConfiguration: Map<String, Any>? = null,
            firstPartyHosts: Map<String, Any?>? = null,
            nativeCrashReportEnabled: Boolean = false,
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

                proxyConfiguration?.let { proxyConfig ->
                    mapProxyConfiguration(proxyConfig)?.let { (proxy, authenticator) ->
                        builder.setProxy(proxy, authenticator)
                    }
                }

                if (additionalConfiguration?.get("_dd.needsClearTextHttp") == true) {
                    _InternalProxy.allowClearTextHttp(builder)
                }

                // Configure first-party hosts for distributed tracing
                if (firstPartyHosts != null) {
                    val hosts = parseFirstPartyHosts(firstPartyHosts)
                    if (hosts.isNotEmpty()) {
                        builder.setFirstPartyHostsWithHeaderType(hosts)
                    }
                }

                builder.setCrashReportsEnabled(nativeCrashReportEnabled)

                Datadog.initialize(context, builder.build(), mapTrackingConsent(trackingConsent))

                Datadog.setVerbosity(mapVerbosity(verbosity))

                if (nativeCrashReportEnabled) {
                    NdkCrashReports.enable()
                }

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

        // -- Flush --

        /**
         * Force the SDK to upload all buffered data immediately.
         *
         * Note: dd-sdk-android exposes flush via the internal proxy. This
         * also shuts down the upload executors, so the SDK should not be
         * used after calling flush() in the same process — it is intended
         * for end-of-test or end-of-process draining.
         */
        @JvmStatic
        fun flush() {
            Datadog._internalProxy().flushAndShutdownExecutors()
        }
    }
}
