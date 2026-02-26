// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Enumerations.UISettings
{
    /// <summary>
    /// Represents the behaviour when closing the main form
    /// </summary>
    public enum CloseBehaviour
    {
        /// <summary>
        /// Exit the application
        /// </summary>
        Exit = 0,

        /// <summary>
        /// Minimize to the system tray
        /// </summary>
        MinimizeToTray = 1,

        /// <summary>
        /// Minimize to the task bar
        /// </summary>
        MinimizeToTaskbar = 2
    }
}