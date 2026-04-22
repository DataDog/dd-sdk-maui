namespace DatadogSdk.Maui.AutoTracking
{
    /// <summary>
    /// Resolves a human-readable target name from a MAUI VisualElement.
    /// Priority: AutomationId → StyleId (x:Name) → type name.
    /// </summary>
    internal static class ActionTargetResolver
    {
        /// <summary>
        /// Resolve a target name for the given element.
        /// </summary>
        /// <param name="element">The visual element to resolve a name for.</param>
        /// <returns>A human-readable target name.</returns>
        internal static string ResolveName(VisualElement element)
        {
            // 1. AutomationId (developer-assigned, stable across releases)
            if (!string.IsNullOrEmpty(element.AutomationId))
                return element.AutomationId;

            // 2. StyleId (XAML x:Name, available in debug, may be stripped in release)
            if (!string.IsNullOrEmpty(element.StyleId))
                return element.StyleId;

            // 3. Type name fallback
            return element.GetType().Name;
        }
    }
}
