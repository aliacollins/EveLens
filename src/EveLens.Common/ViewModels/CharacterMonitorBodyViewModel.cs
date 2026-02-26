// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Core.Interfaces;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// ViewModel for the character monitor body. Manages page routing
    /// (which monitoring page is visible: Assets, Market Orders, etc.).
    /// </summary>
    public sealed class CharacterMonitorBodyViewModel : CharacterViewModelBase
    {
        private string _selectedPage = string.Empty;
        private bool _isVisible;

        public CharacterMonitorBodyViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
        }

        public CharacterMonitorBodyViewModel() : base()
        {
        }

        /// <summary>
        /// Gets or sets the currently selected monitoring page name.
        /// </summary>
        public string SelectedPage
        {
            get => _selectedPage;
            set => SetProperty(ref _selectedPage, value ?? string.Empty);
        }

        /// <summary>
        /// Gets or sets whether this monitor is currently visible (active tab).
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        /// <summary>
        /// Navigates to the specified monitoring page.
        /// </summary>
        /// <param name="pageName">The page to display (e.g., "Assets", "MarketOrders").</param>
        public void SelectPage(string pageName)
        {
            SelectedPage = pageName ?? string.Empty;
        }
    }
}
