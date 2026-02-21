// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Events;
using EVEMon.Common.Helpers;
using EVEMon.Common.Interfaces;
using EVEMon.Common.Models;
using EVEMon.Common.SettingsObjects;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels
{
    /// <summary>
    /// ViewModel for the skill plan editor. Manages the plan display,
    /// sorting (three-state), statistics, implant sets, and pluggable support.
    /// </summary>
    public sealed partial class PlanEditorViewModel : CharacterViewModelBase
    {
        private Plan? _plan;
        private PlanScratchpad? _displayPlan;
        private PlanEntrySort _sortCriteria;
        private ThreeStateSortOrder _sortOrder;
        private bool _groupByPriority;
        private IPlanOrderPluggable? _pluggable;
        private PlanEntryStats _planStats;
        private int _entryCount;

        public PlanEditorViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            SubscribeEvents();
        }

        public PlanEditorViewModel() : base()
        {
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            Subscribe<PlanChangedEvent>(OnPlanChanged);
            Subscribe<SettingsChangedEvent>(_ => OnSettingsOrPricesChanged());
            Subscribe<CharactersBatchUpdatedEvent>(e => OnCharacterOrQueueUpdated(e.Characters));
            Subscribe<SkillQueuesBatchUpdatedEvent>(e => OnCharacterOrQueueUpdated(e.Characters));
            Subscribe<CharacterImplantSetCollectionChangedEvent>(OnImplantSetCollectionChanged);
            Subscribe<ItemPricesUpdatedEvent>(_ => OnSettingsOrPricesChanged());
        }

        #region Properties

        /// <summary>
        /// Gets or sets the currently displayed plan.
        /// Setting this rebuilds the display plan and statistics.
        /// </summary>
        public Plan? Plan
        {
            get => _plan;
            set
            {
                if (!SetProperty(ref _plan, value))
                    return;

                if (_plan != null)
                {
                    _displayPlan = new PlanScratchpad(_plan.Character);
                    _sortCriteria = _plan.SortingPreferences.Criteria;
                    _sortOrder = _plan.SortingPreferences.Order;
                    _groupByPriority = _plan.SortingPreferences.GroupByPriority;
                    OnPropertyChanged(nameof(DisplayPlan));
                    OnPropertyChanged(nameof(SortCriteria));
                    OnPropertyChanged(nameof(SortOrder));
                    OnPropertyChanged(nameof(GroupByPriority));
                    UpdateDisplayPlan();
                }
            }
        }

        /// <summary>
        /// Gets the display plan (PlanScratchpad) used for rendering sorted/filtered entries.
        /// </summary>
        public PlanScratchpad? DisplayPlan => _displayPlan;

        /// <summary>
        /// Gets or sets the sort criteria.
        /// </summary>
        public PlanEntrySort SortCriteria
        {
            get => _sortCriteria;
            set => SetProperty(ref _sortCriteria, value);
        }

        /// <summary>
        /// Gets or sets the three-state sort order: None, Ascending, Descending.
        /// </summary>
        public ThreeStateSortOrder SortOrder
        {
            get => _sortOrder;
            set => SetProperty(ref _sortOrder, value);
        }

        /// <summary>
        /// Gets or sets whether entries are grouped by priority.
        /// </summary>
        public bool GroupByPriority
        {
            get => _groupByPriority;
            set
            {
                if (!SetProperty(ref _groupByPriority, value))
                    return;

                if (_plan != null)
                {
                    _plan.SortingPreferences.GroupByPriority = value;
                    UpdateDisplayPlan();
                }
            }
        }

        /// <summary>
        /// Gets or sets the pluggable (e.g., implants calculator or attributes optimizer).
        /// </summary>
        public IPlanOrderPluggable? Pluggable
        {
            get => _pluggable;
            set => SetProperty(ref _pluggable, value);
        }

        #endregion


        #region Computed Statistics

        /// <summary>
        /// Gets the computed statistics for the entire plan.
        /// </summary>
        public PlanEntryStats PlanStats => _planStats;

        /// <summary>
        /// Gets the total number of plan entries.
        /// </summary>
        public int EntryCount
        {
            get => _entryCount;
            private set => SetProperty(ref _entryCount, value);
        }

        /// <summary>
        /// Gets whether the plan contains obsolete entries (skills already trained).
        /// </summary>
        public bool ContainsObsoleteEntries => _plan?.ContainsObsoleteEntries ?? false;

        /// <summary>
        /// Gets whether the plan contains invalid entries.
        /// </summary>
        public bool ContainsInvalidEntries => _plan?.ContainsInvalidEntries ?? false;

        /// <summary>
        /// Gets the index of the chosen implant set (0-based).
        /// </summary>
        public int ChosenImplantSetIndex
        {
            get
            {
                if (_plan?.ChosenImplantSet == null || Character == null)
                    return 0;

                Character? character = Character as Character;
                if (character == null)
                    return 0;

                int index = 0;
                foreach (ImplantSet set in character.ImplantSets)
                {
                    if (set == _plan.ChosenImplantSet)
                        return index;
                    index++;
                }
                return 0;
            }
        }

        #endregion


        #region Sort

        /// <summary>
        /// Toggles sort on a column using three-state cycle: None -> Ascending -> Descending -> None.
        /// </summary>
        public void ToggleSortColumn(PlanEntrySort criteria)
        {
            if (_plan == null)
                return;

            if (criteria != PlanEntrySort.None)
            {
                if (_sortCriteria == criteria)
                {
                    switch (_sortOrder)
                    {
                        case ThreeStateSortOrder.None:
                            SortOrder = ThreeStateSortOrder.Ascending;
                            break;
                        case ThreeStateSortOrder.Ascending:
                            SortOrder = ThreeStateSortOrder.Descending;
                            break;
                        case ThreeStateSortOrder.Descending:
                            SortOrder = ThreeStateSortOrder.None;
                            break;
                    }
                }
                else
                {
                    SortOrder = ThreeStateSortOrder.Ascending;
                }
            }

            SortCriteria = criteria;

            // Sync to plan
            _plan.SortingPreferences.Criteria = _sortCriteria;
            _plan.SortingPreferences.Order = _sortOrder;

            UpdateDisplayPlan();
        }

        /// <summary>
        /// Sets the chosen implant set by index.
        /// </summary>
        public void SetImplantSet(int index)
        {
            if (_plan == null)
                return;

            Character? character = Character as Character;
            if (character == null)
                return;

            var sets = character.ImplantSets.ToList();
            if (index < 0 || index >= sets.Count)
                return;

            _plan.ChosenImplantSet = sets[index];
            if (_displayPlan != null)
                _displayPlan.ChosenImplantSet = _plan.ChosenImplantSet;

            OnPropertyChanged(nameof(ChosenImplantSetIndex));
            UpdateStatistics();
        }

        /// <summary>
        /// Clears obsolete entries using the specified policy.
        /// </summary>
        public void ClearObsoleteEntries(ObsoleteRemovalPolicy policy)
        {
            _plan?.CleanObsoleteEntries(policy);
            UpdateDisplayPlan();
        }

        #endregion


        #region Display Plan Update

        /// <summary>
        /// Rebuilds the display plan from the current plan, applies sorting, and updates statistics.
        /// </summary>
        public void UpdateDisplayPlan()
        {
            if (_plan == null || _displayPlan == null)
                return;

            _displayPlan.RebuildPlanFrom(_plan, true);

            // Share remapping points and booster points
            PlanEntry[] srcEntries = _plan.ToArray();
            PlanEntry[] destEntries = _displayPlan.ToArray();
            for (int i = 0; i < srcEntries.Length && i < destEntries.Length; i++)
            {
                destEntries[i].Remapping = srcEntries[i].Remapping;
                destEntries[i].BoosterPoint = srcEntries[i].BoosterPoint;
            }

            // Apply sort
            _displayPlan.Sort(_plan.SortingPreferences);

            UpdateStatistics();

            OnPropertyChanged(nameof(DisplayPlan));
            OnPropertyChanged(nameof(ContainsObsoleteEntries));
            OnPropertyChanged(nameof(ContainsInvalidEntries));
        }

        /// <summary>
        /// Updates training time statistics on the display plan.
        /// </summary>
        public void UpdateStatistics()
        {
            if (_displayPlan == null || _plan == null)
                return;

            if (_pluggable != null)
            {
                _pluggable.UpdateStatistics(_displayPlan, out _);
            }
            else
            {
                CharacterScratchpad scratchpad = new CharacterScratchpad(_plan.Character);
                if (_plan.ChosenImplantSet != null)
                    scratchpad = scratchpad.After(_plan.ChosenImplantSet);

                if (_plan.HasBoosterSimulation)
                {
                    int boosterBonus = _plan.SimulatedBoosterBonus;
                    scratchpad.Memory.BoosterBonus = boosterBonus;
                    scratchpad.Charisma.BoosterBonus = boosterBonus;
                    scratchpad.Intelligence.BoosterBonus = boosterBonus;
                    scratchpad.Perception.BoosterBonus = boosterBonus;
                    scratchpad.Willpower.BoosterBonus = boosterBonus;
                }

                _displayPlan.UpdateStatistics(scratchpad, true, true);

                if (_displayPlan.HasBoosterInjectionPoints)
                    _displayPlan.UpdateOldTrainingTimes();
            }

            PlanEntry[] entries = _displayPlan.ToArray();
            _planStats = PlanEntryStats.Compute(entries);
            EntryCount = entries.Length;
            OnPropertyChanged(nameof(PlanStats));
        }

        #endregion


        #region Event Handlers

        private void OnPlanChanged(PlanChangedEvent e)
        {
            if (e.Plan == _plan && _plan != null)
                UpdateDisplayPlan();
        }

        private void OnSettingsOrPricesChanged()
        {
            if (_plan != null)
                UpdateStatistics();
        }

        private void OnCharacterOrQueueUpdated(IReadOnlyList<Character> characters)
        {
            if (_plan != null && Character != null && characters.Contains((Character)Character))
                UpdateDisplayPlan();
        }

        private void OnImplantSetCollectionChanged(CharacterImplantSetCollectionChangedEvent e)
        {
            if (_plan == null || Character == null || e.Character != Character)
                return;

            OnPropertyChanged(nameof(ChosenImplantSetIndex));
            UpdateStatistics();
        }

        #endregion
    }
}
