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
        // Starts a Java thread that throws from real Java bytecode
        // (JavaThrower.level1 -> level2 -> level3). The exception never passes through
        // managed code, so it is a pure Java crash: reported only when
        // NativeCrashReportEnabled is true.
        // JavaThrower is compiled in via <AndroidJavaSource> only (no bindings project),
        // so there's no generated C# proxy type for it — invoke it via raw JNI instead.
        var javaThrowerClass = JNIEnv.FindClass("com/datadog/mauiexample/crash/JavaThrower");
        try
        {
            var crashMethodId = JNIEnv.GetStaticMethodID(javaThrowerClass, "crashOnJavaThread", "()V");
            JNIEnv.CallStaticVoidMethod(javaThrowerClass, crashMethodId);
        }
        finally
        {
            // JNIEnv.FindClass returns a global reference. Freeing it with DeleteLocalRef
            // makes CheckJNI abort the runtime in debuggable builds.
            JNIEnv.DeleteGlobalRef(javaThrowerClass);
        }
    }

    public static partial void TriggerNdkCrash()
    {
        // Call C-level abort() from libc, generating a SIGABRT
        // This is a genuine NDK/native crash picked up by NdkCrashReports
        abort();
    }
}
