// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Threading.Tasks;

namespace EveLens.Core.Interfaces
{
    /// <summary>
    /// Platform-agnostic clipboard access service.
    /// Replaces direct calls to <c>System.Windows.Forms.Clipboard</c>.
    /// </summary>
    /// <remarks>
    /// Production (Avalonia): Delegates to Avalonia clipboard APIs.
    /// Testing: Store text in a field for assertion.
    /// Prefer async methods on Linux/macOS to avoid deadlocks — sync methods
    /// block the UI thread on platforms where clipboard access requires the event loop.
    /// </remarks>
    public interface IClipboardService
    {
        /// <summary>
        /// Sets the clipboard text content. May deadlock on Linux/macOS if called from UI thread.
        /// Prefer <see cref="SetTextAsync"/> for cross-platform safety.
        /// </summary>
        void SetText(string text);

        /// <summary>
        /// Gets the current clipboard text content. May deadlock on Linux/macOS if called from UI thread.
        /// Prefer <see cref="GetTextAsync"/> for cross-platform safety.
        /// </summary>
        string? GetText();

        /// <summary>
        /// Asynchronously sets the clipboard text content. Safe on all platforms.
        /// </summary>
        Task SetTextAsync(string text);

        /// <summary>
        /// Asynchronously gets the current clipboard text content. Safe on all platforms.
        /// </summary>
        Task<string?> GetTextAsync();
    }
}
