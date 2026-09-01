/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.Runtime.InteropServices;

namespace example;

public static partial class NativeCrashHelper
{
    [DllImport("c")]
    private static extern void abort();

    public static partial void TriggerNativeCrash()
    {
        // Calls into real Java bytecode (JavaThrower.level1 -> level2 -> level3),
        // so the resulting exception carries a genuine JVM-populated stack trace
        // (file/line info), unlike a C#-constructed Java.Lang.Throwable.
        Com.Datadog.Mauiexample.Crash.JavaThrower.Level1();
    }

    public static partial void TriggerNdkCrash()
    {
        // Call C-level abort() from libc, generating a SIGABRT
        // This is a genuine NDK/native crash picked up by NdkCrashReports
        abort();
    }
}
