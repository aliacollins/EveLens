// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.Common.Controls
{
    /// <summary>
    /// Defines the action that the parent should take upon notification of an API Error Troubleshooter resolution.
    /// </summary>
    public enum ResolutionAction
    {
        /// <summary>
        /// No action.
        /// </summary>
        None,

        /// <summary>
        /// Close the window.
        /// </summary>
        Close,

        /// <summary>
        /// Hide the troubleshooter.
        /// </summary>
        HideTroubleshooter
    }
}