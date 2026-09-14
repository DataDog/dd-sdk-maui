/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

package com.datadog.mauiexample.crash;

// Genuinely thrown from real JVM/ART bytecode (unlike the C#-constructed
// Java.Lang.RuntimeException path), so getStackTrace() carries real
// file/line-numbered StackTraceElement frames.
public class JavaThrower {
    public static void level1() {
        level2();
    }

    private static void level2() {
        level3();
    }

    private static void level3() {
        throw new RuntimeException("Genuine Java Exception");
    }
}
