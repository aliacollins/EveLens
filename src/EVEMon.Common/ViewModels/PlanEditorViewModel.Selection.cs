// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Extensions;
using EVEMon.Common.Models;

namespace EVEMon.Common.ViewModels
{
    /// <summary>
    /// Immutable snapshot of computed statistics for a set of plan entries.
    /// Used for both full-plan stats and selection stats.
    /// </summary>
    public readonly record struct PlanEntryStats(
        int UniqueSkillsCount,
        int NotKnownSkillsCount,
        long BooksCost,
        long NotKnownBooksCost,
        long TotalSkillPoints,
        TimeSpan TrainingTime)
    {
        public static readonly PlanEntryStats Empty = default;

        public static PlanEntryStats Compute(IReadOnlyList<PlanEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return Empty;

            return new PlanEntryStats(
                entries.GetUniqueSkillsCount(),
                entries.GetNotKnownSkillsCount(),
                entries.GetTotalBooksCost(),
                entries.GetNotKnownSkillBooksCost(),
                entries.GetTotalSkillPoints(),
                entries.Aggregate(TimeSpan.Zero, (current, entry) => current.Add(entry.TrainingTime)));
        }
    }

    // Selection partial: UI pushes selection, VM computes stats.
    public sealed partial class PlanEditorViewModel
    {
        private IReadOnlyList<PlanEntry> _selectedEntries = Array.Empty<PlanEntry>();
        private PlanEntryStats _selectionStats;

        #region Selection State

        /// <summary>
        /// Gets the currently selected plan entries. Set by the UI via <see cref="SetSelection"/>.
        /// </summary>
        public IReadOnlyList<PlanEntry> SelectedEntries => _selectedEntries;

        /// <summary>
        /// Gets whether any entries are selected.
        /// </summary>
        public bool HasSelection => _selectedEntries.Count > 0;

        /// <summary>
        /// Gets whether exactly one entry is selected.
        /// </summary>
        public bool HasSingleSelection => _selectedEntries.Count == 1;

        /// <summary>
        /// Sets the current selection from the UI and recomputes selection statistics.
        /// </summary>
        public void SetSelection(IReadOnlyList<PlanEntry> selectedEntries)
        {
            _selectedEntries = selectedEntries ?? Array.Empty<PlanEntry>();
            _selectionStats = PlanEntryStats.Compute(_selectedEntries);
            OnPropertyChanged(nameof(SelectedEntries));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(HasSingleSelection));
            OnPropertyChanged(nameof(SelectionStats));
        }

        #endregion


        #region Selection Statistics

        /// <summary>
        /// Gets the computed statistics for the current selection.
        /// </summary>
        public PlanEntryStats SelectionStats => _selectionStats;

        #endregion


        #region Movement Checks

        /// <summary>
        /// Returns whether the selected indices can be moved up in the display plan.
        /// </summary>
        public bool CanMoveUp(IReadOnlyList<int> selectedIndices)
        {
            if (selectedIndices == null || selectedIndices.Count == 0)
                return false;

            return selectedIndices[0] > 0;
        }

        /// <summary>
        /// Returns whether the selected indices can be moved down in the display plan.
        /// </summary>
        public bool CanMoveDown(IReadOnlyList<int> selectedIndices)
        {
            if (selectedIndices == null || selectedIndices.Count == 0 || _displayPlan == null)
                return false;

            return selectedIndices[selectedIndices.Count - 1] < _displayPlan.Count - 1;
        }

        #endregion
    }
}
