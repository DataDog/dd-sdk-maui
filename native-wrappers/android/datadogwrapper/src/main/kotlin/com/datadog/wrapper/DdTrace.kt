/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

package com.datadog.wrapper

import android.util.Log
import com.datadog.android.Datadog
import com.datadog.android.trace.DatadogTracing
import com.datadog.android.trace.GlobalDatadogTracer
import com.datadog.android.trace.Trace
import com.datadog.android.trace.TraceConfiguration
import com.datadog.android.trace.api.scope.DatadogScope
import com.datadog.android.trace.api.span.DatadogSpan
import com.datadog.android.trace.api.tracer.DatadogTracer
import java.util.concurrent.TimeUnit

class DdTrace {
    companion object {
        private var tracer: DatadogTracer? = null
        private val activeSpans: MutableMap<String, DatadogSpan> = mutableMapOf()
        private val activeScopes: MutableMap<String, DatadogScope> = mutableMapOf()
        private val lock = Any()

        @JvmStatic
        fun enableTrace(customEndpoint: String?) {
            val builder = TraceConfiguration.Builder()

            if (!customEndpoint.isNullOrBlank()) {
                builder.useCustomEndpoint(customEndpoint)
            }

            val traceConfig = builder.build()
            Trace.enable(traceConfig, Datadog.getInstance())

            val newTracer = DatadogTracing.newTracerBuilder(
                Datadog.getInstance()
            ).build()
            GlobalDatadogTracer.registerIfAbsent(newTracer)
            tracer = GlobalDatadogTracer.get()
        }

        private fun ensureTracer() {
            if (tracer == null) {
                Log.w(
                    "DatadogWrapper",
                    "DdTrace.enableTrace() must be called before tracing." +
                        " Call will be dropped."
                )
            }
        }

        // For testing: reset static state between tests
        internal fun resetForTesting() {
            synchronized(lock) {
                tracer = null
                activeSpans.clear()
                activeScopes.clear()
            }
        }

        @JvmStatic
        fun startSpan(
            operation: String,
            context: Map<String, Any?>,
            timestampMs: Long
        ): String = synchronized(lock) {
            ensureTracer()

            val currentTracer = tracer
                ?: return@synchronized ""

            val span = currentTracer.buildSpan(operation)
                .withStartTimestamp(
                    TimeUnit.MILLISECONDS.toMicros(timestampMs)
                )
                .start()

            // Activate span to establish parent-child nesting
            val scope = currentTracer.activateSpan(span)

            // Use the real span ID from the Datadog SDK context
            val spanId = span.context().spanId.toString()

            // Set context tags
            for ((key, value) in context) {
                span.setTag(key, value)
            }

            activeSpans[spanId] = span
            if (scope != null) {
                activeScopes[spanId] = scope
            }
            spanId
        }

        @JvmStatic
        fun finishSpan(
            spanId: String,
            context: Map<String, Any?>,
            timestampMs: Long
        ): Unit = synchronized(lock) {
            ensureTracer()

            // Close the scope first to restore previous active span
            val scope = activeScopes.remove(spanId)
            scope?.close()

            val span = activeSpans.remove(spanId)
            if (span == null) {
                Log.w(
                    "DatadogWrapper",
                    "DdTrace.finishSpan: No active span found" +
                        " for id $spanId."
                )
                return@synchronized
            }

            // Set additional context tags
            for ((key, value) in context) {
                span.setTag(key, value)
            }

            span.finish(TimeUnit.MILLISECONDS.toMicros(timestampMs))
        }
    }
}