// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EVEMon.Common.Models;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels
{
    /// <summary>
    /// Modes available in the plan editor window.
    /// </summary>
    public enum PlanEditorMode
    {
        Plan = 0,
        Skills = 1,
        Ships = 2,
        Items = 3,
        Blueprints = 4,
        Advanced = 5
    }

    /// <summary>
    /// Top-level ViewModel for the plan editor window. Owns the inner
    /// <see cref="PlanEditorViewModel"/> and all dashboard/detail child ViewModels.
    /// </summary>
    public sealed class PlanEditorWindowViewModel : ViewModelBase
    {
        private PlanEditorMode _selectedMode;
        private bool _isOptimizerVisible;

        public PlanEditorWindowViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            PlanEditor = new PlanEditorViewModel(eventAggregator, dispatcher);
            Dashboard = new PlanDashboardViewModel(PlanEditor, eventAggregator, dispatcher);
            SkillList = new PlanSkillListViewModel(eventAggregator, dispatcher);
            Optimizer = new PlanOptimizerViewModel(eventAggregator, dispatcher);
            Detail = new PlanEntryDetailViewModel(eventAggregator, dispatcher);

            SkillList.PlanEditor = PlanEditor;
        }

        public PlanEditorWindowViewModel()
        {
            PlanEditor = new PlanEditorViewModel();
            Dashboard = new PlanDashboardViewModel(PlanEditor);
            SkillList = new PlanSkillListViewModel();
            Optimizer = new PlanOptimizerViewModel();
            Detail = new PlanEntryDetailViewModel();

            SkillList.PlanEditor = PlanEditor;
        }

        /// <summary>
        /// Gets the inner plan editor ViewModel that manages the plan display and mutations.
        /// </summary>
        public PlanEditorViewModel PlanEditor { get; }

        /// <summary>
        /// Gets the dashboard ViewModel that computes summary stats.
        /// </summary>
        public PlanDashboardViewModel Dashboard { get; }

        /// <summary>
        /// Gets the skill list ViewModel that categorizes plan entries.
        /// </summary>
        public PlanSkillListViewModel SkillList { get; }

        /// <summary>
        /// Gets the optimizer ViewModel (stub for future attribute optimization).
        /// </summary>
        public PlanOptimizerViewModel Optimizer { get; }

        /// <summary>
        /// Gets the detail ViewModel for the currently selected plan entry.
        /// </summary>
        public PlanEntryDetailViewModel Detail { get; }

        /// <summary>
        /// Gets or sets the plan. Delegates to <see cref="PlanEditor"/> and propagates to children.
        /// </summary>
        public Plan? Plan
        {
            get => PlanEditor.Plan;
            set
            {
                PlanEditor.Plan = value;
                OnPropertyChanged(nameof(Plan));
                RefreshChildren();
            }
        }

        /// <summary>
        /// Gets or sets the character. Delegates to <see cref="PlanEditor"/>.
        /// </summary>
        public Character? Character
        {
            get => PlanEditor.Character as Character;
            set
            {
                PlanEditor.Character = value;
                OnPropertyChanged(nameof(Character));
            }
        }

        public PlanEditorMode SelectedMode
        {
            get => _selectedMode;
            set => SetProperty(ref _selectedMode, value);
        }

        public bool IsOptimizerVisible
        {
            get => _isOptimizerVisible;
            set => SetProperty(ref _isOptimizerVisible, value);
        }

        /// <summary>
        /// Stub: creates a plan from a ship (Phase 5 placeholder).
        /// </summary>
        public void CreatePlanFromShip(object ship)
        {
            // Phase 5 — no-op
        }

        /// <summary>
        /// Stub: creates a plan from an EFT fitting string (Phase 5 placeholder).
        /// </summary>
        public void CreatePlanFromFitting(string eftText)
        {
            // Phase 5 — no-op
        }

        private void RefreshChildren()
        {
            Dashboard.Refresh();
            SkillList.Refresh();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                PlanEditor.Dispose();
                Dashboard.Dispose();
                SkillList.Dispose();
                Optimizer.Dispose();
                Detail.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
