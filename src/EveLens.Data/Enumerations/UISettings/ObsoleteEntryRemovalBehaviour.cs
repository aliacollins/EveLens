// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Enumerations.UISettings
{
    /// <summary>
    /// Describes the behaviour employed to remove Obsolete Entries from plans.
    /// </summary>
    public enum ObsoleteEntryRemovalBehaviour
    {
        /// <summary>
        /// Never remove entries from the plan, always ask the user.
        /// </summary>
        AlwaysAsk = 0,

        /// <summary>
        /// Only remove confirmed completed (by API) entries from the plan, ask about unconfirmed entries.
        /// </summary>
        RemoveConfirmed = 1,

        /// <summary>
        /// Always remove all entries automatically.
        /// </summary>
        RemoveAll = 2
    }
}