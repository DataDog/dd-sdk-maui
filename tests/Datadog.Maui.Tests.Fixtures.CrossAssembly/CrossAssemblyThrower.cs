/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System.Runtime.CompilerServices;

namespace Datadog.Maui.Tests.Fixtures.CrossAssembly;

/// <summary>
/// NoInlining keeps this frame distinct in the stack trace instead of being folded into
/// the caller — without it, the JIT is free to inline this trivial method and the resulting
/// trace would never actually cross the assembly boundary we're trying to test.
/// </summary>
public static class CrossAssemblyThrower
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowFromThisAssembly()
    {
        throw new InvalidOperationException("Thrown from the cross-assembly test fixture");
    }
}
