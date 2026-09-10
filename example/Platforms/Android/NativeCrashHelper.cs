/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.Runtime.InteropServices;
using Android.Runtime;

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
        // JavaThrower is compiled in via <AndroidJavaSource> only (no bindings project),
        // so there's no generated C# proxy type for it — invoke it via raw JNI instead.
        var javaThrowerClass = JNIEnv.FindClass("com/datadog/mauiexample/crash/JavaThrower");
        var level1MethodId = JNIEnv.GetStaticMethodID(javaThrowerClass, "level1", "()V");
        JNIEnv.CallStaticVoidMethod(javaThrowerClass, level1MethodId);
    }

    public static partial void TriggerNdkCrash()
    {
        // Call C-level abort() from libc, generating a SIGABRT
        // This is a genuine NDK/native crash picked up by NdkCrashReports
        abort();
    }
}
