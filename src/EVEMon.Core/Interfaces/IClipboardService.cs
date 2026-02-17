namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Platform-agnostic clipboard access service.
    /// Replaces direct calls to <c>System.Windows.Forms.Clipboard</c>.
    /// </summary>
    /// <remarks>
    /// Production (WinForms): <c>WinFormsClipboardService</c> in <c>EVEMon.Common</c>.
    /// Production (Avalonia): Will delegate to Avalonia clipboard APIs.
    /// Testing: Store text in a field for assertion.
    /// </remarks>
    public interface IClipboardService
    {
        /// <summary>
        /// Sets the clipboard text content.
        /// </summary>
        /// <param name="text">The text to place on the clipboard.</param>
        void SetText(string text);

        /// <summary>
        /// Gets the current clipboard text content.
        /// </summary>
        /// <returns>The clipboard text, or null if the clipboard is empty or does not contain text.</returns>
        string? GetText();
    }
}
