// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Data;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.Interfaces;
using EveLens.Common.Models;

namespace EveLens.Common.ViewModels
{
    // Reorder partial: move up/down, drag-drop, remapping.
    public sealed partial class PlanEditorViewModel
    {
        #region Move Operations

        /// <summary>
        /// Moves selected entries up by one position in the display plan.
        /// Selected indices refer to positions in the display plan entry list.
        /// </summary>
        public void MoveSelectedUp(IReadOnlyList<int> selectedIndices)
        {
            if (_displayPlan == null || _plan == null || selectedIndices == null || selectedIndices.Count == 0)
                return;

            List<PlanEntry> items = _displayPlan.ToList();

            // Skip the head (already at top)
            int index = 0;
            while (index < items.Count && selectedIndices.Contains(index))
            {
                index++;
            }

            // Move up the following selected items
            while (index < items.Count)
            {
                if (selectedIndices.Contains(index))
                {
                    PlanEntry item = items[index];
                    items.RemoveAt(index);
                    items.Insert(index - 1, item);
                }
                index++;
            }

            RebuildPlanFromReorder(items);
        }

        /// <summary>
        /// Moves selected entries down by one position in the display plan.
        /// </summary>
        public void MoveSelectedDown(IReadOnlyList<int> selectedIndices)
        {
            if (_displayPlan == null || _plan == null || selectedIndices == null || selectedIndices.Count == 0)
                return;

            List<PlanEntry> items = _displayPlan.ToList();

            // Skip the tail (already at bottom)
            int index = items.Count - 1;
            while (index >= 0 && selectedIndices.Contains(index))
            {
                index--;
            }

            // Move down the preceding selected items
            while (index >= 0)
            {
                if (selectedIndices.Contains(index))
                {
                    PlanEntry item = items[index];
                    items.RemoveAt(index);
                    items.Insert(index + 1, item);
                }
                index--;
            }

            RebuildPlanFromReorder(items);
        }

        /// <summary>
        /// Moves the entry at the specified display index to the top of the plan.
        /// </summary>
        public void MoveToTop(int selectedIndex)
        {
            if (_displayPlan == null || _plan == null)
                return;

            List<PlanEntry> items = _displayPlan.ToList();
            if (selectedIndex < 0 || selectedIndex >= items.Count)
                return;

            PlanEntry item = items[selectedIndex];
            items.RemoveAt(selectedIndex);
            items.Insert(0, item);

            RebuildPlanFromReorder(items);
        }

        /// <summary>
        /// Commits a full reorder of the plan from a new ordered list of entries.
        /// This replaces the old RebuildPlanFromListViewOrder method.
        /// </summary>
        public void CommitReorder(IReadOnlyList<PlanEntry> newOrder)
        {
            if (_plan == null || newOrder == null)
                return;

            RebuildPlanFromReorder(newOrder);
        }

        #endregion


        #region Drag-Drop Support

        /// <summary>
        /// Determines the next unplanned level for a skill. Returns a new PlanEntry
        /// for that level, or null if the skill is fully planned/trained to level 5.
        /// </summary>
        public PlanEntry? PrepareSkillDrop(StaticSkill skill)
        {
            if (_plan == null || skill == null)
                return null;

            long newLevel = _plan.GetPlannedLevel(skill) + 1;

            // Check if the character already has a higher level
            long characterLevel = _plan.Character.GetSkillLevel(skill);
            if (characterLevel >= newLevel)
                newLevel = characterLevel + 1;

            if (newLevel > 5)
                return null;

            return new PlanEntry(_plan, skill, newLevel);
        }

        /// <summary>
        /// Inserts a plan entry at the specified display index and rebuilds the plan.
        /// </summary>
        public void InsertEntryAtIndex(PlanEntry entry, int displayIndex)
        {
            if (_displayPlan == null || _plan == null || entry == null)
                return;

            List<PlanEntry> items = _displayPlan.ToList();

            if (displayIndex < 0)
                displayIndex = 0;
            if (displayIndex > items.Count)
                displayIndex = items.Count;

            items.Insert(displayIndex, entry);
            RebuildPlanFromReorder(items);
        }

        #endregion


        #region Remapping Points

        /// <summary>
        /// Toggles a remapping point on the specified display entry.
        /// If the entry has a remapping point, it is removed. Otherwise, one is added.
        /// </summary>
        public void ToggleRemapping(PlanEntry displayEntry)
        {
            if (_plan == null || displayEntry == null)
                return;

            PlanEntry? originalEntry = GetOriginalEntry(displayEntry);
            if (originalEntry == null)
                return;

            originalEntry.Remapping = originalEntry.Remapping != null ? null : new RemappingPoint();
        }

        /// <summary>
        /// Removes a remapping point from the entry that follows the given display entry.
        /// Used when the user selects a remapping point item in the list.
        /// </summary>
        public void RemoveRemappingBefore(PlanEntry entryAfterRemapping)
        {
            if (_plan == null || entryAfterRemapping == null)
                return;

            PlanEntry? originalEntry = GetOriginalEntry(entryAfterRemapping);
            if (originalEntry != null)
                originalEntry.Remapping = null;
        }

        #endregion


        #region Private Helpers

        /// <summary>
        /// From a display plan entry, retrieves the corresponding entry in the base plan.
        /// </summary>
        private PlanEntry? GetOriginalEntry(ISkillLevel displayEntry)
        {
            return _plan?.GetEntry(displayEntry.Skill, displayEntry.Level);
        }

        /// <summary>
        /// Rebuilds the base plan from the given ordered list of entries.
        /// Clears sorting preferences since the plan is now in manual order.
        /// </summary>
        private void RebuildPlanFromReorder(IEnumerable<PlanEntry> entries)
        {
            // Since the list is manually reordered, disable sorting
            _plan!.SortingPreferences.Order = ThreeStateSortOrder.None;
            _plan.SortingPreferences.GroupByPriority = false;

            _sortOrder = ThreeStateSortOrder.None;
            _groupByPriority = false;
            OnPropertyChanged(nameof(SortOrder));
            OnPropertyChanged(nameof(GroupByPriority));

            _plan.RebuildPlanFrom(entries);
        }

        #endregion
    }
}
