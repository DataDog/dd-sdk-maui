/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

using DatadogSdk.Maui.Configuration;

namespace DatadogSdk.Maui.AutoTracking
{
    /// <summary>
    /// Automatically tracks MAUI page navigations as RUM views.
    /// Hooks into Shell.Navigated and Page.Appearing via Application.DescendantAdded.
    /// Relies on implicit view stop (new StartView auto-stops previous on the native SDK).
    /// </summary>
    internal class DdAutoViewTracker
    {
        private readonly Func<Page, string?>? _viewNamePredicate;
        private readonly Func<Page, bool>? _viewTrackingPredicate;
        private string? _lastViewKey;
        private string? _lastViewName;
        private bool _isShellApp;

        internal DdAutoViewTracker(
            Func<Page, string?>? viewNamePredicate,
            Func<Page, bool>? viewTrackingPredicate)
        {
            _viewNamePredicate = viewNamePredicate;
            _viewTrackingPredicate = viewTrackingPredicate;
        }

        /// <summary>
        /// Start automatic view tracking by hooking into the application's visual tree.
        /// </summary>
        internal void Start(Application application)
        {
            application.DescendantAdded += OnDescendantAdded;
            WalkExistingTree(application);
            InternalLog.Log("DdAutoViewTracker: Started automatic view tracking", SdkVerbosity.DEBUG);
        }

        /// <summary>
        /// Stop automatic view tracking.
        /// </summary>
        internal void Stop(Application application)
        {
            application.DescendantAdded -= OnDescendantAdded;
            InternalLog.Log("DdAutoViewTracker: Stopped automatic view tracking", SdkVerbosity.DEBUG);
        }

        private void OnDescendantAdded(object? sender, ElementEventArgs e)
        {
            switch (e.Element)
            {
                case Shell shell:
                    _isShellApp = true;
                    shell.Navigated += OnShellNavigated;
                    break;
                case Page page when !_isShellApp:
                    // Only track Page.Appearing for non-Shell apps
                    // In Shell apps, Shell.Navigated handles everything
                    page.Appearing += OnPageAppearing;
                    break;
                case Window window:
                    window.Resumed += OnWindowResumed;
                    window.Stopped += OnWindowStopped;
                    break;
            }
        }

        private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
        {
            var location = e.Current?.Location?.ToString() ?? "unknown";
            var page = Shell.Current?.CurrentPage;

            // Apply tracking predicate
            if (page != null && _viewTrackingPredicate != null && !_viewTrackingPredicate(page))
                return;

            var name = ResolveViewName(page, location);
            var key = location;

            // Dedup: don't re-start the same view
            if (key == _lastViewKey) return;
            _lastViewKey = key;
            _lastViewName = name;

            InternalLog.Log($"DdAutoViewTracker: Shell navigated to {name} (key={key})", SdkVerbosity.DEBUG);
            DdRum.StartView(key, name);
        }

        private void OnPageAppearing(object? sender, EventArgs e)
        {
            if (sender is not Page page) return;

            // Apply tracking predicate
            if (_viewTrackingPredicate != null && !_viewTrackingPredicate(page))
                return;

            var name = ResolveViewName(page, null);
            var key = page.GetType().FullName ?? page.GetType().Name;

            // Dedup
            if (key == _lastViewKey) return;
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
        /// Priority: custom predicate → Shell route → Page class name → "Unknown"
        /// </summary>
        private string ResolveViewName(Page? page, string? shellRoute)
        {
            // 1. Custom predicate
            if (page != null && _viewNamePredicate != null)
            {
                var custom = _viewNamePredicate(page);
                if (custom != null) return custom;
            }

            // 2. Shell route (cleaned up — remove leading slashes and query params)
            //    Skip internal MAUI-generated routes (e.g. "D_FAULT_NavDetailPage6")
            //    which appear when pages are pushed via Navigation.PushAsync inside a Shell app.
            if (shellRoute != null)
            {
                var cleaned = shellRoute.TrimStart('/').Split('?')[0];
                if (!string.IsNullOrEmpty(cleaned) && !IsInternalRoute(cleaned))
                    return cleaned;
            }

            // 3. Page class name
            if (page != null)
                return page.GetType().Name;

            // 4. Fallback
            return "Unknown";
        }

        /// <summary>
        /// Detects MAUI-generated internal route segments (e.g. "D_FAULT_", "IMPL_")
        /// that appear when pages are pushed via Navigation.PushAsync inside a Shell app.
        /// </summary>
        private static bool IsInternalRoute(string route)
        {
            // Check each segment — routes can be "MainPage/D_FAULT_NavDetailPage6"
            foreach (var segment in route.Split('/'))
            {
                if (segment.StartsWith("D_FAULT_", StringComparison.Ordinal) ||
                    segment.StartsWith("IMPL_", StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Walk the existing visual tree on init to attach to any Shell/Page/Window already present.
        /// </summary>
        private void WalkExistingTree(Application application)
        {
            foreach (var window in application.Windows)
            {
                window.Resumed += OnWindowResumed;
                window.Stopped += OnWindowStopped;

                if (window.Page is Shell shell)
                {
                    _isShellApp = true;
                    shell.Navigated += OnShellNavigated;
                }
                else if (window.Page is Page page)
                {
                    page.Appearing += OnPageAppearing;
                }
            }
        }

        // For testing / debugging
        internal string? LastViewKey => _lastViewKey;
    }
}
