/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using Datadog.Maui.Configuration;

namespace Datadog.Maui.AutoTracking
{
    /// <summary>
    /// Automatically tracks MAUI user interactions as RUM actions.
    /// Hooks into per-control events via Application.DescendantAdded.
    /// Supports Button, ImageButton, Switch, CheckBox, RadioButton, Picker,
    /// Stepper, DatePicker, and gesture recognizers (Tap, Swipe).
    /// </summary>
    internal class DdAutoActionTracker
    {
        private long _lastActionTimestampMs;
        private const long DebounceMs = 10;

        /// <summary>
        /// Start automatic action tracking by hooking into the application's visual tree.
        /// </summary>
        internal void Start(Application application)
        {
            application.DescendantAdded += OnDescendantAdded;
            application.DescendantRemoved += OnDescendantRemoved;

            // Walk existing visual tree to bind controls already present
            WalkExistingTree(application);

            InternalLog.Log("DdAutoActionTracker: Started automatic action tracking", SdkVerbosity.DEBUG);
        }

        /// <summary>
        /// Stop automatic action tracking.
        /// </summary>
        internal void Stop(Application application)
        {
            application.DescendantAdded -= OnDescendantAdded;
            application.DescendantRemoved -= OnDescendantRemoved;
            InternalLog.Log("DdAutoActionTracker: Stopped automatic action tracking", SdkVerbosity.DEBUG);
        }

        private void OnDescendantAdded(object? sender, ElementEventArgs e)
        {
            if (e.Element is not VisualElement ve) return;
            BindElement(ve);
        }

        private void OnDescendantRemoved(object? sender, ElementEventArgs e)
        {
            if (e.Element is not VisualElement ve) return;
            UnbindElement(ve);
        }

        private void BindElement(VisualElement element)
        {
            // Bind control-specific events
            switch (element)
            {
                case Button btn:
                    btn.Pressed += OnButtonPressed;
                    break;
                case ImageButton imgBtn:
                    imgBtn.Pressed += OnImageButtonPressed;
                    break;
                case Switch sw:
                    sw.Toggled += OnSwitchToggled;
                    break;
                case CheckBox cb:
                    cb.CheckedChanged += OnCheckBoxChanged;
                    break;
                case RadioButton rb:
                    rb.CheckedChanged += OnRadioButtonChanged;
                    break;
                case Picker pk:
                    pk.SelectedIndexChanged += OnPickerChanged;
                    break;
                case Stepper st:
                    st.ValueChanged += OnStepperChanged;
                    break;
                case DatePicker dp:
                    dp.DateSelected += OnDatePickerChanged;
                    break;
            }

            // Bind gesture recognizers (only available on View, not all VisualElements)
            if (element is View view)
            {
                foreach (var gesture in view.GestureRecognizers)
                {
                    BindGesture(view, gesture);
                }
            }
        }

        private void UnbindElement(VisualElement element)
        {
            switch (element)
            {
                case Button btn:
                    btn.Pressed -= OnButtonPressed;
                    break;
                case ImageButton imgBtn:
                    imgBtn.Pressed -= OnImageButtonPressed;
                    break;
                case Switch sw:
                    sw.Toggled -= OnSwitchToggled;
                    break;
                case CheckBox cb:
                    cb.CheckedChanged -= OnCheckBoxChanged;
                    break;
                case RadioButton rb:
                    rb.CheckedChanged -= OnRadioButtonChanged;
                    break;
                case Picker pk:
                    pk.SelectedIndexChanged -= OnPickerChanged;
                    break;
                case Stepper st:
                    st.ValueChanged -= OnStepperChanged;
                    break;
                case DatePicker dp:
                    dp.DateSelected -= OnDatePickerChanged;
                    break;
            }
            // Note: gesture recognizer event handlers are not easily unbound
            // because we use lambdas. This is acceptable — the element is being
            // removed from the tree, so the handlers will be garbage collected.
        }

        /// <summary>
        /// Bind to gesture events on a view.
        /// Tap and Swipe both fire on completion; an action that triggers navigation
        /// from a Tapped/Swiped handler will be bucketed under the destination view
        /// rather than the source view. Document this as a known limitation rather
        /// than mutating the user's GestureRecognizers collection to work around it.
        /// </summary>
        private void BindGesture(VisualElement owner, IGestureRecognizer gesture)
        {
            switch (gesture)
            {
                case TapGestureRecognizer tap:
                    tap.Tapped += (s, e) => TrackAction(owner, RumActionType.Tap);
                    break;
                case SwipeGestureRecognizer swipe:
                    swipe.Swiped += (s, e) => TrackAction(owner, RumActionType.Swipe);
                    break;
            }
        }

        // Control event handlers
        // Buttons track on Pressed (not Clicked) so the native AddAction call
        // happens before any Clicked-handler navigation can shift the active view.
        private void OnButtonPressed(object? s, EventArgs e)
        {
            if (s is Button b) TrackAction(b, RumActionType.Tap);
        }

        private void OnImageButtonPressed(object? s, EventArgs e)
        {
            if (s is ImageButton b) TrackAction(b, RumActionType.Tap);
        }

        private void OnSwitchToggled(object? s, ToggledEventArgs e)
        {
            if (s is Switch sw) TrackAction(sw, RumActionType.Tap);
        }

        private void OnCheckBoxChanged(object? s, CheckedChangedEventArgs e)
        {
            if (s is CheckBox cb) TrackAction(cb, RumActionType.Tap);
        }

        private void OnRadioButtonChanged(object? s, CheckedChangedEventArgs e)
        {
            if (s is RadioButton rb) TrackAction(rb, RumActionType.Tap);
        }

        private void OnPickerChanged(object? s, EventArgs e)
        {
            if (s is Picker pk) TrackAction(pk, RumActionType.Tap);
        }

        private void OnStepperChanged(object? s, ValueChangedEventArgs e)
        {
            if (s is Stepper st) TrackAction(st, RumActionType.Tap);
        }

        private void OnDatePickerChanged(object? s, DateChangedEventArgs e)
        {
            if (s is DatePicker dp) TrackAction(dp, RumActionType.Tap);
        }

        /// <summary>
        /// Track an action for the given element.
        /// Applies debounce, then delegates to DdRum.AddAction which handles ActionEventMapper.
        /// </summary>
        internal void TrackAction(VisualElement element, RumActionType type)
        {
            // Debounce: ignore actions within 10ms of the last one
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (now - _lastActionTimestampMs < DebounceMs) return;
            _lastActionTimestampMs = now;

            var targetName = ActionTargetResolver.ResolveName(element);
            var context = new Dictionary<string, object>
            {
                ["action.target.class"] = element.GetType().Name
            };

            InternalLog.Log($"DdAutoActionTracker: {type} on {targetName}", SdkVerbosity.DEBUG);
            DdRum.AddAction(type, targetName, context);
        }

        /// <summary>
        /// Walk the existing visual tree to bind controls already present before tracking started.
        /// </summary>
        private void WalkExistingTree(Application application)
        {
            foreach (var window in application.Windows)
            {
                if (window.Page != null)
                {
                    WalkElement(window.Page);
                }
            }
        }

        private void WalkElement(Element element)
        {
            if (element is VisualElement ve)
            {
                BindElement(ve);
            }

            foreach (var child in element.GetVisualTreeDescendants())
            {
                if (child is VisualElement childVe)
                {
                    BindElement(childVe);
                }
            }
        }
    }
}
