// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Represents the type of a plan operation.
    /// </summary>
    public enum PlanOperations
    {
        /// <summary>
        /// None, there is nothing to do.
        /// </summary>
        None,

        /// <summary>
        /// The operation is an addition.
        /// </summary>
        Addition,

        /// <summary>
        /// The operation is a suppression.
        /// </summary>
        Suppression
    }
}