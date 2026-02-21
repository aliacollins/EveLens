// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels
{
    /// <summary>
    /// ViewModel for the settings form. Provides dirty tracking and apply/cancel
    /// semantics for the settings dialog.
    /// </summary>
    public sealed class SettingsFormViewModel : FormViewModel
    {
        private string _selectedCategory = string.Empty;

        public SettingsFormViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
        }

        public SettingsFormViewModel() : base()
        {
        }

        /// <summary>
        /// Gets or sets the currently selected settings category/page.
        /// </summary>
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value ?? string.Empty))
                {
                    // Category change doesn't dirty the form
                }
            }
        }

        /// <summary>
        /// Saves the settings. Called by <see cref="FormViewModel.Apply"/>.
        /// </summary>
        protected override void OnApply()
        {
            Settings.Save();
        }

        /// <summary>
        /// Reverts to saved settings. Called by <see cref="FormViewModel.Cancel"/>.
        /// </summary>
        protected override void OnCancel()
        {
            // Settings are re-read from disk on next access
        }
    }
}
