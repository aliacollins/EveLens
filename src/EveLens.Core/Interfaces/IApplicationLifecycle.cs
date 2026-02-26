// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Core.Interfaces
{
    /// <summary>
    /// Platform-agnostic application lifecycle control.
    /// Replaces direct calls to <c>System.Windows.Forms.Application.Exit()</c>
    /// and <c>Application.Restart()</c>.
    /// </summary>
    /// <remarks>
    /// Production (WinForms): <c>WinFormsApplicationLifecycle</c> in <c>EveLens.Common</c>.
    /// Production (Avalonia): Will delegate to Avalonia application shutdown.
    /// Testing: Record calls without actually exiting.
    /// </remarks>
    public interface IApplicationLifecycle
    {
        /// <summary>
        /// Exits the application gracefully.
        /// </summary>
        void Exit();

        /// <summary>
        /// Restarts the application.
        /// </summary>
        void Restart();
    }
}
