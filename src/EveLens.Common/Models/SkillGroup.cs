// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Linq;
using EveLens.Common.Attributes;
using EveLens.Common.Collections;
using EveLens.Common.Data;

namespace EveLens.Common.Models
{
    /// <summary>
    /// Represents a skills group
    /// </summary>
    [EnforceUIThreadAffinity]
    public sealed class SkillGroup : ReadonlyKeyedCollection<string, Skill>
    {
        private static SkillGroup s_unknownSkillGroup;


        #region Constructors

        /// <summary>
        /// Constructor for an unknown skill group.
        /// </summary>
        private SkillGroup()
        {
            StaticData = StaticSkillGroup.UnknownStaticSkillGroup;
        }

        /// <summary>
        /// Constructor, only used by SkillCollection.
        /// </summary>
        /// <param name="character"></param>
        /// <param name="src"></param>
        internal SkillGroup(Character character, StaticSkillGroup src)
        {
            StaticData = src;

            foreach (StaticSkill srcSkill in src)
            {
                Items[srcSkill.Name] = new Skill(character, this, srcSkill);
            }
        }

        /// <summary>
        /// Constructor, used to build an non-character SkillGroup, only used by SkillCollection.
        /// </summary>
        /// <param name="src">The source.</param>
        internal SkillGroup(StaticSkillGroup src)
            : this(null, src)
        {
        }
        
        #endregion


        #region Public Properties

        /// <summary>
        /// Gets the unknown skill group.
        /// </summary>
        /// <value>
        /// The unknown skill group.
        /// </value>
        public static SkillGroup UnknownSkillGroup => s_unknownSkillGroup ?? (s_unknownSkillGroup = new SkillGroup());

        /// <summary>
        /// Gets the static data associated with this group
        /// </summary>
        public StaticSkillGroup StaticData { get; }

        /// <summary>
        /// Gets the group's ID
        /// </summary>
        public int ID => StaticData.ID;

        /// <summary>
        /// Gets the group's name
        /// </summary>
        public string Name => StaticData.Name;

        /// <summary>
        /// Gets the skill with the provided name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Skill this[string name] => GetByKey(name);

        /// <summary>
        /// Gets a skill by its name
        /// </summary>
        /// <param name="skillName"></param>
        /// <returns></returns>
        public bool Contains(string skillName) => Items.ContainsKey(skillName);

        /// <summary>
        /// Gets the total number of SP in this group
        /// </summary>
        public long TotalSP => Items.Values.Sum(gs => gs.SkillPoints);

        #endregion
    }
}