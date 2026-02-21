// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EVEMon.Common.Attributes;

namespace EVEMon.Common.Models
{
    /// <summary>
    /// Represents a plan's invalid entry.
    /// </summary>
    [EnforceUIThreadAffinity]
    public sealed class InvalidPlanEntry
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public InvalidPlanEntry()
        {
            Acknowledged = false;
        }

        /// <summary>
        /// Name of the skill that can not be identified.
        /// </summary>
        public string SkillName { get; set; }

        /// <summary>
        /// Planned level.
        /// </summary>
        public long PlannedLevel { get; set; }

        /// <summary>
        /// Has the user been notified that this entry has been marked as invalid.
        /// </summary>
        public bool Acknowledged { get; set; }
    }
}