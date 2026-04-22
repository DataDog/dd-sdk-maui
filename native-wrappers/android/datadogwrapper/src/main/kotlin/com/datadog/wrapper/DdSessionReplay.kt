/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

package com.datadog.wrapper

import android.util.Log
import com.datadog.android.Datadog
import com.datadog.android.sessionreplay.SessionReplay
import com.datadog.android.sessionreplay.SessionReplayConfiguration
import com.datadog.android.sessionreplay.ImagePrivacy
import com.datadog.android.sessionreplay.TextAndInputPrivacy
import com.datadog.android.sessionreplay.TouchPrivacy

class DdSessionReplay {
    companion object {

        @JvmStatic
        fun mapTextAndInputPrivacy(level: String): TextAndInputPrivacy =
            when (level.lowercase()) {
                "mask_all_inputs" -> TextAndInputPrivacy.MASK_ALL_INPUTS
                "mask_sensitive_inputs" -> TextAndInputPrivacy.MASK_SENSITIVE_INPUTS
                else -> TextAndInputPrivacy.MASK_ALL
            }

        @JvmStatic
        fun mapImagePrivacy(level: String): ImagePrivacy =
            when (level.lowercase()) {
                "mask_none" -> ImagePrivacy.MASK_NONE
                else -> ImagePrivacy.MASK_ALL
            }

        @JvmStatic
        fun mapTouchPrivacy(level: String): TouchPrivacy =
            when (level.lowercase()) {
                "show" -> TouchPrivacy.SHOW
                else -> TouchPrivacy.HIDE
            }

        @JvmStatic
        fun enableSessionReplay(
            replaySampleRate: Double,
            textAndInputPrivacy: String,
            imagePrivacy: String,
            touchPrivacy: String,
            customEndpoint: String?
        ) {
            try {
                val builder = SessionReplayConfiguration.Builder(replaySampleRate.toFloat())
                    .setTextAndInputPrivacy(mapTextAndInputPrivacy(textAndInputPrivacy))
                    .setImagePrivacy(mapImagePrivacy(imagePrivacy))
                    .setTouchPrivacy(mapTouchPrivacy(touchPrivacy))

                if (!customEndpoint.isNullOrBlank()) {
                    builder.useCustomEndpoint(customEndpoint)
                }

                val config = builder.build()
                SessionReplay.enable(config, Datadog.getInstance())
            } catch (e: Exception) {
                Log.e("DatadogWrapper", "DdSessionReplay.enableSessionReplay failed", e)
            }
        }
    }
}
