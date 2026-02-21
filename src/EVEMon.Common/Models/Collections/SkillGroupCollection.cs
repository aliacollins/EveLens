// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Linq;
using EVEMon.Common.Attributes;
using EVEMon.Common.Collections;
using EVEMon.Common.Data;

namespace EVEMon.Common.Models.Collections
{
    /// <summary>
    /// Represents a collection of a character's skills groups
    /// </summary>
    [EnforceUIThreadAffinity]
    public sealed class SkillGroupCollection : ReadonlyKeyedCollection<int, SkillGroup>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="character"></param>
        internal SkillGroupCollection(Character character)
        {
            foreach (SkillGroup group in StaticSkills.AllGroups
                .Select(srcGroup => new SkillGroup(character, srcGroup)))
            {
                Items[group.ID] = group;
            }
        }

        /// <summary>
        /// Gets the skill group with the provided name
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public SkillGroup this[int id] => GetByKey(id);
    }
}