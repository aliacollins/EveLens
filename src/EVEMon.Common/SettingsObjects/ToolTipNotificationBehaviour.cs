// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details


namespace EVEMon.Common.SettingsObjects
{
    /// <summary>
    /// Represents the behavior of the tool tip notifications (alerts for skills completion, etc)
    /// </summary>
    public enum ToolTipNotificationBehaviour
    {
        /// <summary>
        /// Never notify
        /// </summary>
        Never = 0,

        /// <summary>
        /// Notify only once
        /// </summary>
        Once = 1,

        /// <summary>
        /// Every minute, the warning is repeated until the user clicks the tooltip
        /// </summary>
        RepeatUntilClicked = 2,
    }
}