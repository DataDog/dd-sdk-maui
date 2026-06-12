/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

namespace Datadog.Maui;

/// <summary>
/// Annotates a <see cref="Microsoft.Maui.Controls.Page"/> subclass with Datadog RUM view metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DdViewAttribute(string viewName) : Attribute
{
    /// <summary>
    /// When present, <c>DdAutoViewTracker</c> uses <see cref="ViewName"/> as the view name instead of
    /// deriving it from the Shell route or the page class name.
    /// </summary>
    public string ViewName { get; } = viewName;
}
