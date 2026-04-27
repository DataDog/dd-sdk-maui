namespace DatadogSdk.Maui.Configuration
{
    /// <summary>
    /// Privacy level for text and input fields in Session Replay recordings.
    /// </summary>
    public enum TextAndInputPrivacy
    {
        /// <summary>All text and input fields are masked.</summary>
        MaskAll,
        /// <summary>Only input fields are masked; static text remains visible.</summary>
        MaskAllInputs,
        /// <summary>Only sensitive inputs (passwords, emails, phone numbers) are masked.</summary>
        MaskSensitiveInputs
    }
}
