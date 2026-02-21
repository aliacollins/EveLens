// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Platform-agnostic screen/display information service.
    /// Replaces direct access to <c>System.Windows.Forms.Screen</c>.
    /// </summary>
    /// <remarks>
    /// Production (WinForms): <c>WinFormsScreenInfo</c> in <c>EVEMon.Common</c>.
    /// Production (Avalonia): Will delegate to Avalonia screen APIs.
    /// Testing: Return fixed values for deterministic layout tests.
    /// </remarks>
    public interface IScreenInfo
    {
        /// <summary>
        /// Gets the primary screen's working area (excluding taskbar).
        /// </summary>
        (int X, int Y, int Width, int Height) PrimaryWorkingArea { get; }

        /// <summary>
        /// Gets the bounds of all connected screens.
        /// </summary>
        IReadOnlyList<(int X, int Y, int Width, int Height)> AllScreenBounds { get; }
    }
}
