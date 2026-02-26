// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Enumerations.UISettings
{
    /// <summary>
    /// Represents the behaviour for the system tray icon
    /// </summary>
    public enum SystemTrayBehaviour
    {
        /// <summary>
        /// The tray icon is always hidden
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// The tray icon is visible when the main window is minimized
        /// </summary>
        ShowWhenMinimized = 1,

        /// <summary>
        /// The tray icon is always visible
        /// </summary>
        AlwaysVisible = 2
    }
}