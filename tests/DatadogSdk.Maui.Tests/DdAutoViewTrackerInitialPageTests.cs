/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

using DatadogSdk.Maui.AutoTracking;
using Xunit;

namespace DatadogSdk.Maui.Tests;

/// <summary>
/// Tests for the page-resolution helpers that drive the synthetic-view-emit
/// path in <see cref="DdAutoViewTracker"/>. The synthetic emit runs when the
/// tracker attaches after a page is already visible — the normal situation
/// under the MauiAppBuilder pattern, where UseDatadogRum hooks the tracker
/// up from a post-launch lifecycle event. Without this resolution, a
/// single-page flow produces a RUM session with zero views.
///
/// End-to-end coverage (real Application + Window + Page) lives in the
/// integration framework (<c>test_add_error</c>); the bare unit-test host
/// can't construct a MAUI <c>Application</c> with windows because
/// <c>Application.SystemResources</c> requires platform services.
/// </summary>
public class DdAutoViewTrackerInitialPageTests
{
    private sealed class TestRootPage : ContentPage { }
    private sealed class TestDetailPage : ContentPage { }

    [Fact]
    public void DrillIntoContainer_ContentPage_ReturnsItself()
    {
        var page = new TestRootPage();
        Assert.Same(page, DdAutoViewTracker.DrillIntoContainer(page));
    }

    [Fact]
    public void DrillIntoContainer_NavigationPage_ReturnsCurrentPage()
    {
        var inner = new TestRootPage();
        var nav = new NavigationPage(inner);
        Assert.Same(inner, DdAutoViewTracker.DrillIntoContainer(nav));
    }

    // TabbedPage / Shell construction in the bare test host crashes inside
    // TemplatedItemsList → BindableObject.get_Dispatcher because no MAUI
    // platform dispatcher is registered. The DrillIntoContainer cases for
    // those container types are covered indirectly by the integration
    // framework's test_add_error scenario; the unit tests here cover the
    // page types that do construct cleanly without a host.

    [Fact]
    public void DrillIntoContainer_FlyoutPage_DrillsIntoDetail()
    {
        var detail = new TestDetailPage();
        var flyout = new FlyoutPage
        {
            Flyout = new TestRootPage { Title = "menu" },
            Detail = detail,
        };

        Assert.Same(detail, DdAutoViewTracker.DrillIntoContainer(flyout));
    }

    [Fact]
    public void DrillIntoContainer_FlyoutPage_DrillsThroughNestedNavigationDetail()
    {
        var deepest = new TestDetailPage();
        var flyout = new FlyoutPage
        {
            Flyout = new TestRootPage { Title = "menu" },
            Detail = new NavigationPage(deepest),
        };

        // Detail is a NavigationPage hosting deepest — should recurse to deepest.
        Assert.Same(deepest, DdAutoViewTracker.DrillIntoContainer(flyout));
    }
}
