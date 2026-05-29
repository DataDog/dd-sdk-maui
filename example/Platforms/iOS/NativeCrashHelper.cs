/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.Runtime.InteropServices;
using ObjCRuntime;

namespace example;

public static partial class NativeCrashHelper
{
    [DllImport("__Internal")]
    private static extern void abort();

    public static partial void TriggerNativeCrash()
    {
        // Call C-level abort() which generates a SIGABRT - a true native crash
        abort();
    }
}
