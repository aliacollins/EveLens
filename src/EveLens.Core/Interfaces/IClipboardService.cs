// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Core.Interfaces
{
    /// <summary>
    /// Platform-agnostic clipboard access service.
    /// Replaces direct calls to <c>System.Windows.Forms.Clipboard</c>.
    /// </summary>
    /// <remarks>
    /// Production (WinForms): <c>WinFormsClipboardService</c> in <c>EveLens.Common</c>.
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
