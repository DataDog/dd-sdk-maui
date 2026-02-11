package com.datadog.wrapper

import com.datadog.android.log.Logger
import com.datadog.android.log.Logs
import com.datadog.android.log.LogsConfiguration

class DdLogs {
    companion object {
        private var logger: Logger? = null

        @JvmStatic
        fun enableLogs() {
            val logsConfig = LogsConfiguration.Builder().build()
            Logs.enable(logsConfig)
            logger = Logger.Builder()
                .setNetworkInfoEnabled(true)
                .setLogcatLogsEnabled(true)
                .setBundleWithTraceEnabled(true)
                .build()
        }

        @JvmStatic
        fun logDebug(message: String) {
            logger?.d(message)
        }

        @JvmStatic
        fun logInfo(message: String) {
            logger?.i(message)
        }

        @JvmStatic
        fun logWarn(message: String) {
            logger?.w(message)
        }

        @JvmStatic
        fun logError(message: String) {
            logger?.e(message)
        }

        @JvmStatic
        fun logWithAttributes(level: String, message: String, attributes: Map<String, String>) {
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
