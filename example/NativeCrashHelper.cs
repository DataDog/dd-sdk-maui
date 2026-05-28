/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

namespace example;

public static partial class NativeCrashHelper
{
    public static partial void TriggerNativeCrash();
#if ANDROID
    public static partial void TriggerNdkCrash();
#endif
}
