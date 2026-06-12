/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using Datadog.Maui.AutoTracking;
using Xunit;

namespace Datadog.Maui.Tests;

/// <summary>
/// Unit tests for <see cref="DdAutoViewTracker.ResolveViewName"/>.
/// Covers the priority chain: ViewNamePredicate → [DdView] attribute → Page class name.
/// </summary>
public class DdAutoViewTrackerResolveNameTests
{
    [DdView("Annotated View")]
    private sealed class AnnotatedPage : ContentPage { }

    private sealed class UnannotatedPage : ContentPage { }

    [Fact]
    public void ResolveViewName_DdViewAttribute_ReturnsAttributeName()
    {
        var tracker = new DdAutoViewTracker(null, null);

        var name = tracker.ResolveViewName(new AnnotatedPage());

        Assert.Equal("Annotated View", name);
    }

    [Fact]
    public void ResolveViewName_NoDdViewAttribute_ReturnsPageClassName()
    {
        var tracker = new DdAutoViewTracker(null, null);

        var name = tracker.ResolveViewName(new UnannotatedPage());

        Assert.Equal(nameof(UnannotatedPage), name);
    }

    [Fact]
    public void ResolveViewName_ViewNamePredicateNonNull_WinsOverDdViewAttribute()
    {
        var tracker = new DdAutoViewTracker(_ => "From Predicate", null);

        var name = tracker.ResolveViewName(new AnnotatedPage());

        Assert.Equal("From Predicate", name);
    }

    [Fact]
    public void ResolveViewName_ViewNamePredicateReturnsNull_FallsBackToDdViewAttribute()
    {
        var tracker = new DdAutoViewTracker(_ => null, null);

        var name = tracker.ResolveViewName(new AnnotatedPage());

        Assert.Equal("Annotated View", name);
    }

    [Fact]
    public void ResolveViewName_ViewNamePredicateReturnsNull_NoAttribute_FallsBackToClassName()
    {
        var tracker = new DdAutoViewTracker(_ => null, null);

        var name = tracker.ResolveViewName(new UnannotatedPage());

        Assert.Equal(nameof(UnannotatedPage), name);
    }
}
