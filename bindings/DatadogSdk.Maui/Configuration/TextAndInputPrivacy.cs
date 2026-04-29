/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

namespace DatadogSdk.Maui.Configuration
{
    /// <summary>
    /// Privacy level for text and input fields in Session Replay recordings.
    /// </summary>
    public enum TextAndInputPrivacy
    {
        /// <summary>All text and input fields are masked.</summary>
        MaskAll,
        /// <summary>Only input fields are masked; static text remains visible.</summary>
        MaskAllInputs,
        /// <summary>Only sensitive inputs (passwords, emails, phone numbers) are masked.</summary>
        MaskSensitiveInputs
    }
}
