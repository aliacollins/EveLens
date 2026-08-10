// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Enumerations.UISettings
{
    /// <summary>
    /// How a downloaded update is applied (Discussion #100). Transparency first: the user is
    /// ASKED on the first downloaded update rather than EveLens silently choosing for them,
    /// and the choice remains editable in Settings > Data.
    /// </summary>
    public enum AutoInstallUpdates
    {
        /// <summary>
        /// The user has not chosen yet — the first downloaded update triggers a one-time
        /// dialog explaining both options.
        /// </summary>
        NotAsked = 0,

        /// <summary>
        /// Install silently when the app exits; next launch runs the new version.
        /// </summary>
        Automatic = 1,

        /// <summary>
        /// Show a notification and let the user apply the update manually.
        /// </summary>
        NotifyOnly = 2
    }
}
