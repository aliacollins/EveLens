// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EveLens.Common.Collections.Global;
using EveLens.Common.Constants;
using EveLens.Common.Extensions;
using EveLens.Common.Serialization.Datafiles;
using EveLens.Core;

namespace EveLens.Common.Data
{
    /// <summary>
    /// Represents the list of all static skills.
    /// </summary>
    public static class StaticSkills
    {
        private static int s_arrayIndicesCount;
        private static StaticSkill[] s_skills;
        private static readonly Dictionary<long, StaticSkill> s_skillsByID = new Dictionary<long, StaticSkill>();
        private static readonly Dictionary<string, StaticSkill> s_skillsByName = new Dictionary<string, StaticSkill>();
        private static readonly Dictionary<int, StaticSkillGroup> s_skillGroupsByID = new Dictionary<int, StaticSkillGroup>();


        #region Initialization

        /// <summary>
        /// Initialize static skills.
        /// </summary>
        public static void Load()
        {
            SkillsDatafile datafile = Util.DeserializeDatafile<SkillsDatafile>(DatafileConstants.SkillsDatafile,
                Util.LoadXslt(ServiceLocator.ResourceProvider.DatafilesXSLT));

            // Fetch deserialized data
            s_arrayIndicesCount = 0;
            List<Collection<SerializableSkillPrerequisite>> prereqs = new List<Collection<SerializableSkillPrerequisite>>();
            foreach (SerializableSkillGroup srcGroup in datafile.SkillGroups)
            {
                StaticSkillGroup group = new StaticSkillGroup(srcGroup, ref s_arrayIndicesCount);
                s_skillGroupsByID[@group.ID] = @group;

                // Store skills
                foreach (StaticSkill skill in @group)
                {
                    s_skillsByID[skill.ID] = skill;
                    s_skillsByName[skill.Name] = skill;
                }

                // Store prereqs
                prereqs.AddRange(srcGroup.Skills.Select(serialSkill => serialSkill.SkillPrerequisites));
            }

            // Complete initialization
            s_skills = new StaticSkill[s_arrayIndicesCount];
            foreach (StaticSkill staticSkill in s_skillsByID.Values)
            {
                staticSkill.CompleteInitialization(prereqs[staticSkill.ArrayIndex]);
                s_skills[staticSkill.ArrayIndex] = staticSkill;
            }

            GlobalDatafileCollection.OnDatafileLoaded();
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// Gets the total number of zero-based indices given to skills (for optimization purposes, it allows the use of arrays for computations).
        /// </summary>
        public static int ArrayIndicesCount => s_arrayIndicesCount;

        /// <summary>
        /// Gets the list of groups.
        /// </summary>
        public static IEnumerable<StaticSkillGroup> AllGroups => s_skillGroupsByID.Values;

        /// <summary>
        /// Gets the list of groups.
        /// </summary>
        public static IEnumerable<StaticSkill> AllSkills => s_skillGroupsByID.Values.SelectMany(group => group);

        #endregion


        #region Public Finders

        /// <summary>
        /// Gets a skill by its id or its name.
        /// </summary>
        /// <param name="src">The source.</param>
        /// <returns>The static skill</returns>
        /// <remarks>
        /// This method exists for backwards compatibility
        /// with settings that don't contain the skill's id.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">src</exception>
        public static StaticSkill GetSkill(this SerializableSkillPrerequisite src)
        {
            src.ThrowIfNull(nameof(src));

            return GetSkillByID(src.ID) ?? GetSkillByName(src.Name) ?? StaticSkill.UnknownStaticSkill;
        }

        /// <summary>
        /// Gets the name of the skill.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>The skill name or <see cref="EveLensConstants.UnknownText"/> if the is no such skill in our data.</returns>
        public static string GetSkillName(int id)
        {
            if (id == 0)
                return string.Empty;

            StaticSkill skill = GetSkillByID(id);
            return skill?.Name ?? EveLensConstants.UnknownText;
        }

        /// <summary>
        /// Gets a skill by its name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static StaticSkill GetSkillByName(string name)
        {
            StaticSkill skill;
            s_skillsByName.TryGetValue(name, out skill);
            return skill;
        }

        /// <summary>
        /// Gets a skill by its identifier.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static StaticSkill GetSkillByID(long id)
        {
            StaticSkill skill;
            s_skillsByID.TryGetValue(id, out skill);
            return skill;
        }

        /// <summary>
        /// Gets a skill by its array index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static StaticSkill GetSkillByArrayIndex(int index) => s_skills[index];

        /// <summary>
        /// Gets a group by its name.
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        public static StaticSkillGroup GetSkillGroupByID(int groupId)
        {
            StaticSkillGroup group;
            s_skillGroupsByID.TryGetValue(groupId, out group);
            return group;
        }

        #endregion


        #region Reverse Lookups

        private static Dictionary<long, List<StaticSkillLevel>>? s_dependentSkills;
        private static Dictionary<long, List<Item>>? s_itemsByRequiredSkill;

        /// <summary>
        /// Gets all skills that directly require the given skill as a prerequisite.
        /// Returns (skill, required level of the given skill) pairs.
        /// </summary>
        public static IReadOnlyList<StaticSkillLevel> GetDependentSkills(StaticSkill skill)
        {
            EnsureDependentSkillsBuilt();
            return s_dependentSkills!.TryGetValue(skill.ID, out var list)
                ? list
                : Array.Empty<StaticSkillLevel>();
        }

        private static void EnsureDependentSkillsBuilt()
        {
            if (s_dependentSkills != null)
                return;

            var index = new Dictionary<long, List<StaticSkillLevel>>();
            foreach (var skill in AllSkills)
            {
                foreach (var prereq in skill.Prerequisites)
                {
                    if (!index.TryGetValue(prereq.Skill.ID, out var list))
                    {
                        list = new List<StaticSkillLevel>();
                        index[prereq.Skill.ID] = list;
                    }
                    list.Add(new StaticSkillLevel(skill, prereq.Level));
                }
            }
            s_dependentSkills = index;
        }

        /// <summary>
        /// Gets all items/ships that require the given skill (at any level).
        /// </summary>
        public static IReadOnlyList<Item> GetItemsRequiringSkill(StaticSkill skill)
        {
            EnsureItemsBySkillBuilt();
            return s_itemsByRequiredSkill!.TryGetValue(skill.ID, out var list)
                ? list
                : (IReadOnlyList<Item>)Array.Empty<Item>();
        }

        private static void EnsureItemsBySkillBuilt()
        {
            if (s_itemsByRequiredSkill != null)
                return;

            var index = new Dictionary<long, List<Item>>();
            foreach (var item in StaticItems.AllItems)
            {
                foreach (var prereq in item.Prerequisites)
                {
                    if (!index.TryGetValue(prereq.Skill.ID, out var list))
                    {
                        list = new List<Item>();
                        index[prereq.Skill.ID] = list;
                    }
                    list.Add(item);
                }
            }
            s_itemsByRequiredSkill = index;
        }

        #endregion
    }
}