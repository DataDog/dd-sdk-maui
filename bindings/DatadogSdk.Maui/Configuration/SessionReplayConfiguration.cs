namespace DatadogSdk.Maui.Configuration
{
    /// <summary>
    /// Configuration for the Datadog Session Replay module.
    /// </summary>
    public class SessionReplayConfiguration
    {
        /// <summary>
        /// The percentage of sessions to record (0.0 to 100.0).
        /// Default is 100.0 (all sessions).
        /// </summary>
        public double ReplaySampleRate { get; set; } = 100.0;

        /// <summary>
        /// Privacy level for text and input fields. Default is MaskAll.
        /// </summary>
        public TextAndInputPrivacy TextAndInputPrivacyLevel { get; set; } = TextAndInputPrivacy.MaskAll;

        /// <summary>
        /// Privacy level for images. Default is MaskAll.
        /// </summary>
        public ImagePrivacy ImagePrivacyLevel { get; set; } = ImagePrivacy.MaskAll;

        /// <summary>
        /// Privacy level for touch interactions. Default is Hide.
        /// </summary>
        public TouchPrivacy TouchPrivacyLevel { get; set; } = TouchPrivacy.Hide;

        /// <summary>
        /// Custom server endpoint where Session Replay data is sent.
        /// If not provided, data is sent to the standard Datadog endpoint based on your site configuration.
        /// </summary>
        public string? CustomEndpoint { get; set; }
    }
}
