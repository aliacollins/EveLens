// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Data;
using EveLens.Common.Enumerations;
using EveLens.Common.Models;

namespace EveLens.Common.Helpers
{
    /// <summary>
    /// This class holds the responsibility to sort enumerations of plan entries.
    /// </summary>
    public sealed class PlanEntrySorter
    {
        private readonly PlanEntrySort m_sort;
        private readonly bool m_reverseOrder;
        private readonly bool m_groupByPriority;
        private readonly IEnumerable<PlanEntry> m_entries;
        private readonly BaseCharacter m_character;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="character"></param>
        /// <param name="entries"></param>
        /// <param name="sort"></param>
        /// <param name="reverseOrder"></param>
        /// <param name="groupByPriority"></param>
        internal PlanEntrySorter(BaseCharacter character, IEnumerable<PlanEntry> entries, PlanEntrySort sort, bool reverseOrder,
            bool groupByPriority)
        {
            m_sort = sort;
            m_entries = entries;
            m_reverseOrder = reverseOrder;
            m_groupByPriority = groupByPriority;
            m_character = character;
        }

        /// <summary>
        /// Performs the sort.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<PlanEntry> Sort()
        {
            int initialCount = m_entries.Count();

            // Apply first pass (priorities grouping)
            // We split the entries into multiple priority groups if that selection is made
            List<PlanScratchpad> groupedPlan = new List<PlanScratchpad>();
            CharacterScratchpad scratchpad = new CharacterScratchpad(m_character);

            if (m_groupByPriority)
            {
                groupedPlan.AddRange(
                    m_entries.GroupBy(x => x.Priority).OrderBy(x => x.Key).Select(group => new PlanScratchpad(scratchpad, group)));
            }
            else
                groupedPlan.Add(new PlanScratchpad(scratchpad, m_entries));

            // Apply second pass (sorts)
            // We sort every group, and merge them once they're sorted
            List<PlanEntry> list = new List<PlanEntry>();

            foreach (PlanScratchpad group in groupedPlan)
            {
                group.UpdateStatistics(scratchpad, false, false);
                group.SimpleSort(m_sort, m_reverseOrder);
                list.AddRange(group);
            }

            // Fix prerequisites order
            FixPrerequisitesOrder(list);

            // Check we didn't mess up anything
            if (initialCount != list.Count)
                throw new UnauthorizedAccessException("The sort algorithm messed up and deleted items");

            // Return
            return list;
        }

        /// <summary>
        /// Ensures the prerequisites order is correct.
        /// </summary>
        /// <param name="list"></param>
        private static void FixPrerequisitesOrder(ICollection<PlanEntry> list)
        {
            // Gather prerequisites/postrequisites relationships and use them to connect nodes
            Dictionary<PlanEntry, List<PlanEntry>> dependencies = new Dictionary<PlanEntry, List<PlanEntry>>();
            foreach (PlanEntry entry in list)
            {
                dependencies[entry] = new List<PlanEntry>(list.Where(x => entry.IsDependentOf(x)));
            }

            // Insert entries, preferring same-attribute runs to keep grouping intact
            LinkedList<PlanEntry> entriesToAdd = new LinkedList<PlanEntry>(list);
            SkillLevelSet<PlanEntry> set = new SkillLevelSet<PlanEntry>();
            list.Clear();

            EveAttribute lastAttr = EveAttribute.None;

            while (entriesToAdd.Count != 0)
            {
                // Find all entries whose prerequisites are satisfied
                var ready = entriesToAdd
                    .Where(x => dependencies[x].All(y => set[y.Skill, y.Level] != null))
                    .ToList();

                // Prefer entries from the same primary attribute as the last added skill
                PlanEntry item = ready.FirstOrDefault(x => x.Skill.PrimaryAttribute == lastAttr)
                    ?? ready.First();

                set[item.Skill, item.Level] = item;
                entriesToAdd.Remove(item);
                list.Add(item);
                lastAttr = item.Skill.PrimaryAttribute;
            }
        }


        #region Simple sort operators

        /// <summary>
        /// Compares by name.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns></returns>
        public static int CompareByName(PlanEntry x, PlanEntry y)
        {
            int nameDiff = string.CompareOrdinal(x.Skill.Name, y.Skill.Name);
            return nameDiff != 0 ? nameDiff : Convert.ToInt32(x.Level - y.Level);
        }

        /// <summary>
        /// Compares by cost.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns></returns>
        public static int CompareByCost(PlanEntry x, PlanEntry y)
        {
            long xCost = x.Level == 1 && !x.CharacterSkill.IsOwned ? x.Skill.Cost : 0;
            long yCost = y.Level == 1 && !x.CharacterSkill.IsOwned ? y.Skill.Cost : 0;
            return xCost.CompareTo(yCost);
        }

        /// <summary>
        /// Compares by training time.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns></returns>
        public static int CompareByTrainingTime(PlanEntry x, PlanEntry y) => x.TrainingTime.CompareTo(y.TrainingTime);

        /// <summary>
        /// Compares by training time natural.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns></returns>
        public static int CompareByTrainingTimeNatural(PlanEntry x, PlanEntry y)
            => x.NaturalTrainingTime.CompareTo(y.NaturalTrainingTime);

        /// <summary>
        /// Compares by SP per hour.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns></returns>
        public static int CompareBySPPerHour(PlanEntry x, PlanEntry y) => x.SpPerHour - y.SpPerHour;

        /// <summary>
        /// Compares by primary attribute.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns></returns>
        public static int CompareByPrimaryAttribute(PlanEntry x, PlanEntry y)
            => (int)x.Skill.PrimaryAttribute - (int)y.Skill.PrimaryAttribute;

        /// <summary>
        /// Compares by secondary attribute.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns></returns>
        public static int CompareBySecondaryAttribute(PlanEntry x, PlanEntry y)
            => (int)x.Skill.SecondaryAttribute - (int)y.Skill.SecondaryAttribute;

        /// <summary>
        /// Compares by priority.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns></returns>
        public static int CompareByPriority(PlanEntry x, PlanEntry y) => x.Priority - y.Priority;

        /// <summary>
        /// Compares by plan group.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns></returns>
        public static int CompareByPlanGroup(PlanEntry x, PlanEntry y)
            => string.Compare(x.PlanGroupsDescription, y.PlanGroupsDescription, StringComparison.CurrentCulture);

        /// <summary>
        /// Compares by plan type.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns></returns>
        public static int CompareByPlanType(PlanEntry x, PlanEntry y) => (int)x.Type - (int)y.Type;

        /// <summary>
        /// Compares by notes.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns></returns>
        public static int CompareByNotes(PlanEntry x, PlanEntry y)
            => string.Compare(x.Notes, y.Notes, StringComparison.CurrentCulture);

        /// <summary>
        /// Compares by time difference.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns></returns>
        public static int CompareByTimeDifference(PlanEntry x, PlanEntry y)
        {
            TimeSpan xDuration = x.TrainingTime - x.OldTrainingTime;
            TimeSpan yDuration = y.TrainingTime - y.OldTrainingTime;
            return xDuration.CompareTo(yDuration);
        }

        /// <summary>
        /// Compares by percent completed.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns></returns>
        public static int CompareByPercentCompleted(PlanEntry x, PlanEntry y)
        {
            float xRatio = x.CharacterSkill.FractionCompleted;
            float yRatio = y.CharacterSkill.FractionCompleted;
            return xRatio.CompareTo(yRatio);
        }

        /// <summary>
        /// Compares by rank.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns></returns>
        public static int CompareByRank(PlanEntry x, PlanEntry y) => Convert.ToInt32(x.Skill.Rank - y.Skill.Rank);

        /// <summary>
        /// Compares by skill points required.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns></returns>
        public static int CompareBySkillPointsRequired(PlanEntry x, PlanEntry y)
            => x.SkillPointsRequired.CompareTo(y.SkillPointsRequired);

        /// <summary>
        /// Compares by clone state required.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns></returns>
        public static int CompareByOmegaRequired(PlanEntry x, PlanEntry y)
            => x.OmegaRequired.CompareTo(y.OmegaRequired);

        /// <summary>
        /// Compares by skill group duration.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="entries">The entries.</param>
        /// <param name="skillGroupsDurations">The skill groups durations.</param>
        /// <returns></returns>
        public static int CompareBySkillGroupDuration(PlanEntry x, PlanEntry y, IEnumerable<PlanEntry> entries,
            Dictionary<StaticSkillGroup, TimeSpan> skillGroupsDurations)
        {
            TimeSpan xDuration = GetSkillGroupDuration(x.Skill.Group, entries, skillGroupsDurations);
            TimeSpan yDuration = GetSkillGroupDuration(y.Skill.Group, entries, skillGroupsDurations);
            return xDuration.CompareTo(yDuration);
        }

        /// <summary>
        /// Gets the duration of the skill group.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="entries">The entries.</param>
        /// <param name="skillGroupsDurations">The skill groups durations.</param>
        /// <returns></returns>
        private static TimeSpan GetSkillGroupDuration(StaticSkillGroup group, IEnumerable<PlanEntry> entries,
            IDictionary<StaticSkillGroup, TimeSpan> skillGroupsDurations)
        {
            if (skillGroupsDurations.ContainsKey(group))
                return skillGroupsDurations[group];

            TimeSpan time = entries.Where(x => x.Skill.Group == group).Aggregate(TimeSpan.Zero,
                (current, entry) =>
                    current + entry.TrainingTime);
            skillGroupsDurations[group] = time;

            return skillGroupsDurations[group];
        }

        #endregion
    }
}
