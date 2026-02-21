// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EVEMon.Common.Models;

namespace EVEMon.Common.CustomEventArgs
{
    public sealed class IndustryJobsEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="industryJobs">The industry jobs.</param>
        public IndustryJobsEventArgs(Character character, IEnumerable<IndustryJob> industryJobs)
        {
            Character = character;
            CompletedJobs = industryJobs.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets the character related to this event.
        /// </summary>
        public Character Character { get; }

        /// <summary>
        /// Gets the industry jobs related to this event.
        /// </summary>
        public ReadOnlyCollection<IndustryJob> CompletedJobs { get; }
    }
}