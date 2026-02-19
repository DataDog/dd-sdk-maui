package com.datadog.wrapper

import android.util.Log
import com.datadog.android.log.Logger
import com.datadog.android.log.Logs
import com.datadog.android.log.LogsConfiguration

class DdLogs {
    companion object {
        private var logger: Logger? = null

        @JvmStatic
        fun enableLogs(customEndpoint: String?) {
            val builder = LogsConfiguration.Builder()

            // Set custom endpoint if provided and not blank
            if (!customEndpoint.isNullOrBlank()) {
                builder.useCustomEndpoint(customEndpoint)
            }

            val logsConfig = builder.build()
            Logs.enable(logsConfig)

            val loggerBuilder = Logger.Builder()
                .setNetworkInfoEnabled(true)
                .setLogcatLogsEnabled(true)
                .setBundleWithTraceEnabled(true)

            logger = loggerBuilder.build()
        }

        private fun ensureLogger() {
            if (logger == null) {
                Log.w("DatadogWrapper", "DdLogs.enableLogs() must be called before logging. Log will be dropped.")
            }
        }

        // For testing: reset static state between tests
        internal fun resetForTesting() {
            logger = null
        }

        @JvmStatic
        fun logDebug(message: String) {
            ensureLogger()
            logger?.d(message)
        }

        @JvmStatic
        fun logInfo(message: String) {
            ensureLogger()
            logger?.i(message)
        }

        @JvmStatic
        fun logWarn(message: String) {
            ensureLogger()
            logger?.w(message)
        }

        @JvmStatic
        fun logError(message: String) {
            ensureLogger()
            logger?.e(message)
        }

        @JvmStatic
        fun logWithAttributes(level: String, message: String, attributes: Map<String, String>) {
            ensureLogger()
            val logAttributes = attributes.mapValues<String, String, Any?> { it.value }
            when (level.lowercase()) {
                "debug" -> logger?.d(message, attributes = logAttributes)
                "info" -> logger?.i(message, attributes = logAttributes)
                "warn", "warning" -> logger?.w(message, attributes = logAttributes)
                "error" -> logger?.e(message, attributes = logAttributes)
                else -> logger?.i(message, attributes = logAttributes)
            }
        }
    }
}
