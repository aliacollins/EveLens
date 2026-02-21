// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Enumeration of character sort criteria.
    /// </summary>
    public enum CharacterSortCriteria
    {
        /// <summary>
        /// Characters are sorted by their names
        /// </summary>
        Name = 0,

        /// <summary>
        /// Characters are sorted by their training completion time or, when not in training, their names.
        /// </summary>
        TrainingCompletion = 1,
    };
}