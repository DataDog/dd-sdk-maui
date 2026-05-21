/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

package com.datadog.wrapper

import com.datadog.android.Datadog
import com.datadog.android._InternalProxy
import com.datadog.android.event.EventMapper
import com.datadog.android.rum.RumConfiguration
import com.datadog.android.rum._RumInternalProxy
import com.datadog.android.telemetry.model.TelemetryConfigurationEvent
import java.util.concurrent.ConcurrentHashMap

/**
 * Bridge for SDK-internal telemetry. Forwards wrapper-level errors, debug
 * messages, and configuration metadata into the Android SDK's telemetry pipeline,
 * so they end up alongside native telemetry in the Datadog backend.
 *
 * Configuration fields are accumulated as the C# layer reports them at SDK
 * init / RUM enable / Trace enable. The accumulated fields are applied to the
 * `TelemetryConfigurationEvent` via a mapper installed on `RumConfiguration.Builder`
 * during `DdRum.enableRum`. This mirrors how dd-sdk-flutter and dd-sdk-reactnative
 * augment the native config event with cross-platform metadata.
 *
 * All entry points are no-ops if Datadog is not initialized.
 */
class DdTelemetry {
    companion object {

        // Dependencies - injectable for testing
        private var telemetryProvider: TelemetryProvider = RealTelemetryProvider()

        // Accumulator for cross-platform configuration fields.
        // Keys here match the keys used in the C# `InternalTelemetry.ReportConfiguration`
        // call site. The mapper below reads from this map at event-emission time.
        private val pendingConfiguration = ConcurrentHashMap<String, Any>()

        @JvmStatic
        fun setTelemetryProvider(provider: TelemetryProvider) {
            telemetryProvider = provider
        }

        // Reset to production dependencies (for test cleanup)
        @JvmStatic
        fun resetDependencies() {
            telemetryProvider = RealTelemetryProvider()
            pendingConfiguration.clear()
        }

        // -- Error / Debug --

        @JvmStatic
        @JvmOverloads
        fun error(message: String, kind: String? = null, stack: String? = null) {
            val proxy = telemetryProvider.telemetryProxy ?: return
            proxy.error(message, stack, kind)
        }

        @JvmStatic
        fun errorWithThrowable(message: String, throwable: Throwable?) {
            val proxy = telemetryProvider.telemetryProxy ?: return
            proxy.error(message, throwable)
        }

        @JvmStatic
        fun debug(message: String) {
            val proxy = telemetryProvider.telemetryProxy ?: return
            proxy.debug(message)
        }

        // -- Configuration telemetry --

        /**
         * Accumulates cross-platform configuration fields. Applied to emitted
         * `TelemetryConfigurationEvent` events by the mapper installed in
         * [installConfigurationMapper]. Fields not in the recognized key set
         * are stored but ignored at emission time.
         */
        @JvmStatic
        fun reportConfiguration(fields: Map<String, Any?>) {
            for ((key, value) in fields) {
                if (value != null) {
                    pendingConfiguration[key] = value
                }
            }
        }

        /**
         * Installs the telemetry-configuration mapper on the given builder.
         * Must be called from `DdRum.enableRum` before `.build()`. Without this
         * hook, accumulated configuration fields aren't applied to events.
         */
        @JvmStatic
        fun installConfigurationMapper(builder: RumConfiguration.Builder): RumConfiguration.Builder {
            return _RumInternalProxy.setTelemetryConfigurationEventMapper(
                builder,
                object : EventMapper<TelemetryConfigurationEvent> {
                    override fun map(event: TelemetryConfigurationEvent): TelemetryConfigurationEvent {
                        applyPendingConfiguration(event.telemetry.configuration)
                        return event
                    }
                }
            )
        }

        private fun applyPendingConfiguration(config: TelemetryConfigurationEvent.Configuration) {
            // Strings
            (pendingConfiguration["mauiVersion"] as? String)?.let { config.mauiVersion = it }
            (pendingConfiguration["initializationType"] as? String)?.let { config.initializationType = it }
            (pendingConfiguration["tracerAPI"] as? String)?.let { config.tracerApi = it }

            // Booleans
            (pendingConfiguration["trackErrors"] as? Boolean)?.let { config.trackErrors = it }
            (pendingConfiguration["trackNativeErrors"] as? Boolean)?.let { config.trackNativeErrors = it }
            (pendingConfiguration["trackUserInteractions"] as? Boolean)?.let { config.trackUserInteractions = it }
            (pendingConfiguration["trackResources"] as? Boolean)?.let { config.trackResources = it }
            (pendingConfiguration["trackBackgroundEvents"] as? Boolean)?.let { config.trackBackgroundEvents = it }
            (pendingConfiguration["trackFrustrations"] as? Boolean)?.let { config.trackFrustrations = it }
            (pendingConfiguration["trackNativeViews"] as? Boolean)?.let { config.trackNativeViews = it }
            (pendingConfiguration["trackViewsManually"] as? Boolean)?.let { config.trackViewsManually = it }
            (pendingConfiguration["trackLongTask"] as? Boolean)?.let { config.trackLongTask = it }
            (pendingConfiguration["trackNativeLongTasks"] as? Boolean)?.let { config.trackNativeLongTasks = it }
            (pendingConfiguration["useFirstPartyHosts"] as? Boolean)?.let { config.useFirstPartyHosts = it }
            (pendingConfiguration["useProxy"] as? Boolean)?.let { config.useProxy = it }
        }
    }

    /**
     * Provider of the `_TelemetryProxy` handle. Allows test injection.
     */
    interface TelemetryProvider {
        val telemetryProxy: _InternalProxy._TelemetryProxy?
    }

    private class RealTelemetryProvider : TelemetryProvider {
        override val telemetryProxy: _InternalProxy._TelemetryProxy?
            get() = if (Datadog.isInitialized()) {
                Datadog._internalProxy()._telemetry
            } else {
                null
            }
    }
}
