// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Events;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Core.Interfaces;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// ViewModel for the main application window. Manages character tab selection
    /// and the list of monitored characters.
    /// </summary>
    public sealed class MainWindowViewModel : ViewModelBase
    {
        private Character? _selectedCharacter;
        private IReadOnlyList<Character> _characters = Array.Empty<Character>();

        public MainWindowViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            Subscribe<MonitoredCharacterCollectionChangedEvent>(e => RefreshCharacters());
            Subscribe<CharacterCollectionChangedEvent>(e => RefreshCharacters());
        }

        public MainWindowViewModel() : base()
        {
            Subscribe<MonitoredCharacterCollectionChangedEvent>(e => RefreshCharacters());
            Subscribe<CharacterCollectionChangedEvent>(e => RefreshCharacters());
        }

        /// <summary>
        /// Gets the list of monitored characters (one per tab).
        /// </summary>
        public IReadOnlyList<Character> Characters
        {
            get => _characters;
            private set => SetProperty(ref _characters, value);
        }

        /// <summary>
        /// Gets or sets the currently selected character.
        /// </summary>
        public Character? SelectedCharacter
        {
            get => _selectedCharacter;
            set => SetProperty(ref _selectedCharacter, value);
        }

        /// <summary>
        /// Selects a character by reference. No-op if already selected.
        /// </summary>
        public void SelectCharacter(Character? character)
        {
            SelectedCharacter = character;
        }

        /// <summary>
        /// Refreshes the character list from AppServices.
        /// </summary>
        public void RefreshCharacters()
        {
            try
            {
                Characters = AppServices.MonitoredCharacters.ToList().AsReadOnly();

                // If selected character was removed, select the first one
                if (_selectedCharacter != null && !Characters.Contains(_selectedCharacter))
                {
                    SelectedCharacter = Characters.FirstOrDefault();
                }
            }
            catch (Exception)
            {
                // AppServices may not be initialized in tests
                Characters = Array.Empty<Character>();
            }
        }
    }
}
