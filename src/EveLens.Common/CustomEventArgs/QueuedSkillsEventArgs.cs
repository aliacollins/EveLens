// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EveLens.Common.Models;

namespace EveLens.Common.CustomEventArgs
{
    public sealed class QueuedSkillsEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="queuedSkills">The queued skills.</param>
        public QueuedSkillsEventArgs(Character character, IEnumerable<QueuedSkill> queuedSkills)
        {
            Character = character;
            CompletedSkills = new List<QueuedSkill>(queuedSkills).AsReadOnly();
        }

        /// <summary>
        /// Gets the character related to this event.
        /// </summary>
        public Character Character { get; }

        /// <summary>
        /// Gets the queued skills related to this event.
        /// </summary>
        public ReadOnlyCollection<QueuedSkill> CompletedSkills { get; }
    }
}