namespace DatadogSdk.Maui.Configuration
{
    /// <summary>
    /// Privacy level for images in Session Replay recordings.
    /// </summary>
    public enum ImagePrivacy
    {
        /// <summary>All images are replaced with placeholders.</summary>
        MaskAll,
        /// <summary>All images are displayed without masking.</summary>
        MaskNone
    }
}
