/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using DatadogSdk.Maui.Configuration;

namespace DatadogSdk.Maui.AutoTracking
{
    /// <summary>
    /// Automatically tracks MAUI page navigations as RUM views.
    /// Subscribes to <see cref="Application.PageAppearing"/> — a single app-level
    /// event raised by the framework for any page appearing anywhere in the visual
    /// tree (Shell route changes, Navigation.PushAsync, modals).
    /// Relies on implicit view stop (a new StartView auto-stops the previous one
    /// on the native SDK).
    /// </summary>
    internal class DdAutoViewTracker
    {
        private readonly Func<Page, string?>? _viewNamePredicate;
        private readonly Func<Page, bool>? _viewTrackingPredicate;
        private string? _lastViewKey;
        private string? _lastViewName;

        // Resolved absolute destination route for the in-flight navigation, computed in
        // Shell.Navigating by combining the (possibly relative) Target.Location with the last
        // post-navigation location. Cleared on Shell.Navigated. Read by ResolveViewName at
        // PageAppearing time, since CurrentState.Location lags one step behind PageAppearing.
        private string? _pendingShellLocation;

        // Last resolved absolute location captured on Shell.Navigated. Used as the base for
        // resolving relative targets (".." / "DetailPage") against in the next Navigating.
        private string? _lastResolvedShellLocation;

        internal DdAutoViewTracker(
            Func<Page, string?>? viewNamePredicate,
            Func<Page, bool>? viewTrackingPredicate)
        {
            _viewNamePredicate = viewNamePredicate;
            _viewTrackingPredicate = viewTrackingPredicate;
        }

        /// <summary>
        /// Start automatic view tracking by hooking into the application's page lifecycle.
        /// </summary>
        internal void Start(Application application)
        {
            application.PageAppearing += OnPageAppearing;
            application.DescendantAdded += OnDescendantAdded;
            WalkExistingWindows(application);
            InternalLog.Log("DdAutoViewTracker: Started automatic view tracking", SdkVerbosity.DEBUG);
        }

        /// <summary>
        /// Stop automatic view tracking.
        /// </summary>
        internal void Stop(Application application)
        {
            application.PageAppearing -= OnPageAppearing;
            application.DescendantAdded -= OnDescendantAdded;
            InternalLog.Log("DdAutoViewTracker: Stopped automatic view tracking", SdkVerbosity.DEBUG);
        }

        private void OnDescendantAdded(object? sender, ElementEventArgs e)
        {
            switch (e.Element)
            {
                case Window window:
                    window.Resumed += OnWindowResumed;
                    window.Stopped += OnWindowStopped;
                    break;
                case Shell shell:
                    shell.Navigating += OnShellNavigating;
                    shell.Navigated += OnShellNavigated;
                    break;
            }
        }

        // Resolve the (possibly relative) Target against the last known absolute location.
        // This gives us the destination route at PageAppearing time, before Shell ticks
        // CurrentState.Location forward.
        private void OnShellNavigating(object? sender, ShellNavigatingEventArgs e) =>
            _pendingShellLocation = ResolveTarget(_lastResolvedShellLocation, e.Target?.Location?.ToString());

        private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
        {
            _pendingShellLocation = null;
            _lastResolvedShellLocation = e.Current?.Location?.ToString();
        }

        /// <summary>
        /// Resolve a Shell navigation target against a base location.
        /// Handles absolute paths ("//Foo/Bar"), back navigation (".." / "../Bar"),
        /// and relative paths ("Bar"). Returns null if the target is empty or
        /// if a relative target can't be resolved (no base).
        /// </summary>
        private static string? ResolveTarget(string? current, string? target)
        {
            if (string.IsNullOrEmpty(target))
            {
                return null;
            }

            if (target.StartsWith("//", StringComparison.Ordinal))
            {
                return target;
            }

            if (string.IsNullOrEmpty(current))
            {
                return null;
            }

            // Drop any query string on both the base and the target before splitting into
            // segments. Without this, a relative target gets appended after the query
            // (e.g. "Product?id=1" + "Reviews" → ".../Product?id=1/Reviews", truncated by
            // CleanRoute back to ".../Product"), and back-nav targets with query
            // parameters (e.g. "..?id=1") don't match the ".." segment check and are
            // treated as a literal route segment instead.
            string baseLocation = current.Split('?', 2)[0];
            string targetPath = target.Split('?', 2)[0];
            List<string> parts = [.. baseLocation.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries)];
            foreach (var segment in targetPath.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                if (segment == "..")
                {
                    if (parts.Count > 0)
                    {
                        parts.RemoveAt(parts.Count - 1);
                    }
                }
                else if (segment != ".")
                {
                    parts.Add(segment);
                }
            }

            return "//" + string.Join('/', parts);
        }

        private void OnPageAppearing(object? sender, Page page)
        {
            if (_viewTrackingPredicate != null && !_viewTrackingPredicate(page))
                return;

            var key = page.GetType().FullName ?? page.GetType().Name;
            var name = ResolveViewName(page);

            // Dedup: don't re-start the same view back-to-back. Compare on (key, name) so
            // Shell apps that reuse a Page class across distinct routes (e.g. ProductPage
            // opened with different ids) still produce separate RUM views — name carries
            // the resolved Shell route when available.
            if (key == _lastViewKey && name == _lastViewName) return;
            _lastViewKey = key;
            _lastViewName = name;

            InternalLog.Log($"DdAutoViewTracker: Page appearing {name} (key={key})", SdkVerbosity.DEBUG);
            DdRum.StartView(key, name);
        }

        private void OnWindowResumed(object? sender, EventArgs e)
        {
            // Restart the last view when the app comes back to foreground
            if (_lastViewKey != null && _lastViewName != null)
            {
                InternalLog.Log($"DdAutoViewTracker: App resumed, restarting view {_lastViewName}", SdkVerbosity.DEBUG);
                DdRum.StartView(_lastViewKey, _lastViewName);
            }
        }

        private void OnWindowStopped(object? sender, EventArgs e)
        {
            // Stop the current view when the app goes to background
            if (_lastViewKey != null)
            {
                InternalLog.Log($"DdAutoViewTracker: App stopped, stopping view {_lastViewKey}", SdkVerbosity.DEBUG);
                DdRum.StopView(_lastViewKey);
            }
        }

        /// <summary>
        /// Resolve a human-readable view name.
        /// Priority: custom predicate → Shell route (when applicable) → Page class name.
        /// Internal MAUI-generated routes (e.g. "D_FAULT_NavDetailPage6", which appear
        /// when pages are pushed via Navigation.PushAsync inside a Shell app) are skipped
        /// so we fall back to the cleaner page class name.
        /// </summary>
        private string ResolveViewName(Page page)
        {
            if (_viewNamePredicate != null)
            {
                var custom = _viewNamePredicate(page);
                if (custom != null) return custom;
            }

            if (Shell.Current?.CurrentPage == page)
            {
                // _pendingShellLocation is the resolved absolute destination of the in-flight
                // navigation, computed in OnShellNavigating. We don't fall back to
                // CurrentState.Location because it always lags PageAppearing by one nav step.
                string? pendingClean = CleanRoute(_pendingShellLocation);
                if (pendingClean != null)
                {
                    return pendingClean;
                }
            }

            return page.GetType().Name;
        }

        /// <summary>
        /// Strip leading slashes and query strings from a Shell location, and reject
        /// internal MAUI-generated routes. Returns null if the result isn't a usable
        /// view name.
        /// </summary>
        private static string? CleanRoute(string? location)
        {
            if (string.IsNullOrEmpty(location))
            {
                return null;
            }

            string cleaned = location.TrimStart('/').Split('?')[0];
            if (string.IsNullOrEmpty(cleaned) || IsInternalRoute(cleaned))
            {
                return null;
            }

            return cleaned;
        }

        private static bool IsInternalRoute(string route)
        {
            foreach (var segment in route.Split('/'))
            {
                if (segment.StartsWith("D_FAULT_", StringComparison.Ordinal) ||
                    segment.StartsWith("IMPL_", StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private void WalkExistingWindows(Application application)
        {
            foreach (var window in application.Windows)
            {
                window.Resumed += OnWindowResumed;
                window.Stopped += OnWindowStopped;

                if (window.Page is Shell shell)
                {
                    shell.Navigating += OnShellNavigating;
                    shell.Navigated += OnShellNavigated;
                    // Seed the base for relative-target resolution. The initial Shell.Navigated
                    // may or may not fire before our first Navigating, so we capture whatever
                    // the Shell already considers its current route.
                    _lastResolvedShellLocation ??= shell.CurrentState?.Location?.ToString();
                }

                // If a page is already on screen at attach time, MAUI has already raised
                // Application.PageAppearing for it and we missed the event. Emit a
                // synthetic one so the initial view isn't silently lost — this happens
                // under the MauiAppBuilder pattern, where UseDatadogRum attaches the
                // tracker from a lifecycle event that fires a few ms after the first
                // page becomes visible. OnPageAppearing dedupes on (key, name), so any
                // race where the real event also fires right after attach produces a
                // single StartView, not two.
                var currentPage = ResolveCurrentVisiblePage(window);
                if (currentPage != null)
                {
                    InternalLog.Log(
                        "DdAutoViewTracker: Tracker attached after first page already visible; emitting synthetic view for current page.",
                        SdkVerbosity.DEBUG);
                    OnPageAppearing(application, currentPage);
                }
            }
        }

        /// <summary>
        /// Resolve the page that is currently visible to the user in
        /// <paramref name="window"/>. Prefers the topmost modal — that's what
        /// MAUI's <see cref="Application.PageAppearing"/> would raise for if a
        /// modal is showing. Falls back to drilling through container pages
        /// (<see cref="Shell"/> / <see cref="NavigationPage"/> /
        /// <see cref="TabbedPage"/> / <see cref="FlyoutPage"/>) so the
        /// resolved page is the user-visible content page, not the container.
        /// </summary>
        internal static Page? ResolveCurrentVisiblePage(Window window)
        {
            var modalTop = window.Navigation?.ModalStack?.LastOrDefault();
            if (modalTop != null)
            {
                return DrillIntoContainer(modalTop);
            }
            return window.Page is null ? null : DrillIntoContainer(window.Page);
        }

        internal static Page DrillIntoContainer(Page page) => page switch
        {
            Shell shell => shell.CurrentPage ?? page,
            NavigationPage nav => nav.CurrentPage ?? page,
            TabbedPage tabbed => tabbed.CurrentPage ?? page,
            FlyoutPage flyout => DrillIntoContainer(flyout.Detail ?? page),
            _ => page,
        };

        // For testing / debugging
        internal string? LastViewKey => _lastViewKey;
    }
}
