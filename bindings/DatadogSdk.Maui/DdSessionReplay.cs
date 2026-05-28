/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

#if ANDROID
using NativeDdSessionReplay = DatadogSdk.Android.Binding.DdSessionReplay;
#elif IOS
using Foundation;
using NativeDdSessionReplay = DatadogSdk.iOS.Binding.DdSessionReplay;
#endif

using DatadogSdk.Maui.Configuration;

namespace DatadogSdk.Maui
{
    public static class DdSessionReplay
    {
        internal interface ISessionReplayBridge
        {
            void Enable(double replaySampleRate, string textAndInputPrivacy,
                        string imagePrivacy, string touchPrivacy, string? customEndpoint);
        }

        internal static ISessionReplayBridge? testBridge;

        /// <summary>
        /// Enable the Session Replay module with the provided configuration.
        /// Must be called after DdSdk.Initialize() and DdRum.Enable().
        /// </summary>
        /// <param name="configuration">Configuration for the Session Replay module.</param>
        public static void Enable(SessionReplayConfiguration configuration)
        {
            InternalLog.Log($"DdSessionReplay.Enable called with sampleRate: {configuration.ReplaySampleRate}", SdkVerbosity.DEBUG);

            var textAndInput = ConvertTextAndInputPrivacy(configuration.TextAndInputPrivacyLevel);
            var image = ConvertImagePrivacy(configuration.ImagePrivacyLevel);
            var touch = ConvertTouchPrivacy(configuration.TouchPrivacyLevel);

            if (testBridge is not null)
            {
                testBridge.Enable(configuration.ReplaySampleRate, textAndInput, image, touch,
                                  configuration.CustomEndpoint);
                return;
            }

#if ANDROID
            NativeDdSessionReplay.EnableSessionReplay(configuration.ReplaySampleRate,
                textAndInput, image, touch, configuration.CustomEndpoint);
#elif IOS
            NativeDdSessionReplay.EnableSessionReplay(configuration.ReplaySampleRate,
                textAndInput, image, touch, configuration.CustomEndpoint);
#endif

            InternalLog.Log("DdSessionReplay.Enable completed", SdkVerbosity.DEBUG);
        }

        private static string ConvertTextAndInputPrivacy(TextAndInputPrivacy level) => level switch
        {
            TextAndInputPrivacy.MaskAll => "mask_all",
            TextAndInputPrivacy.MaskAllInputs => "mask_all_inputs",
            TextAndInputPrivacy.MaskSensitiveInputs => "mask_sensitive_inputs",
            _ => "mask_all"
        };

        private static string ConvertImagePrivacy(ImagePrivacy level) => level switch
        {
            ImagePrivacy.MaskAll => "mask_all",
            ImagePrivacy.MaskNone => "mask_none",
            _ => "mask_all"
        };

        private static string ConvertTouchPrivacy(TouchPrivacy level) => level switch
        {
            TouchPrivacy.Hide => "hide",
            TouchPrivacy.Show => "show",
            _ => "hide"
        };
    }
}
