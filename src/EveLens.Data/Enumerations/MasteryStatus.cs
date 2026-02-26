// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Enumerations
{
    /// <summary>
    /// Represents a mastery's status from a character's point of view.
    /// </summary>
    public enum MasteryStatus
    {
        /// <summary>
        /// The mastery is trained by the char, all prerequisites are met.
        /// </summary>
        Trained,

        /// <summary>
        /// The mastery is not trained yet but at least one prerequisite is satisfied
        /// </summary>
        PartiallyTrained,

        /// <summary>
        /// The mastery is not trained and none of its prerequisites are satisfied
        /// </summary>
        Untrained
    }
}
