// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;
using EVEMon.Common.Events;
using EVEMon.Common.Models;
using EVEMon.Common.Services;
using EVEMon.Common.ViewModels;
using EVEMon.Core.Interfaces;
using EVEMon.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.ViewModels
{
    public class MainWindowViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        private static CCPCharacter CreateTestCharacter(long id, string name)
        {
            var services = new NullCharacterServices();
            var identity = new CharacterIdentity(id, name);
            return new CCPCharacter(identity, services);
        }

        [Fact]
        public void CanInstantiate()
        {
            var vm = new MainWindowViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_SelectedCharacterNull()
        {
            var vm = new MainWindowViewModel(CreateAggregator());
            vm.SelectedCharacter.Should().BeNull();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_CharactersEmpty()
        {
            var vm = new MainWindowViewModel(CreateAggregator());
            vm.Characters.Should().BeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void SelectCharacter_SetsSelectedCharacter()
        {
            var vm = new MainWindowViewModel(CreateAggregator());
            var character = CreateTestCharacter(1L, "Test Pilot");

            vm.SelectCharacter(character);

            vm.SelectedCharacter.Should().Be(character);
            vm.Dispose();
        }

        [Fact]
        public void SelectCharacter_Null_ClearsSelection()
        {
            var vm = new MainWindowViewModel(CreateAggregator());
            var character = CreateTestCharacter(1L, "Test Pilot");
            vm.SelectCharacter(character);

            vm.SelectCharacter(null);

            vm.SelectedCharacter.Should().BeNull();
            vm.Dispose();
        }

        [Fact]
        public void SelectedCharacter_RaisesPropertyChanged()
        {
            var vm = new MainWindowViewModel(CreateAggregator());
            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainWindowViewModel.SelectedCharacter))
                    changedProp = e.PropertyName;
            };

            vm.SelectCharacter(CreateTestCharacter(1L, "Test"));

            changedProp.Should().Be("SelectedCharacter");
            vm.Dispose();
        }

        [Fact]
        public void Characters_DefaultsToEmpty()
        {
            var vm = new MainWindowViewModel(CreateAggregator());

            vm.Characters.Should().BeEmpty();

            vm.Dispose();
        }

        [Fact]
        public void RefreshCharacters_DoesNotThrow()
        {
            var vm = new MainWindowViewModel(CreateAggregator());

            var act = () => vm.RefreshCharacters();
            act.Should().NotThrow();

            vm.Dispose();
        }

        [Fact]
        public void Dispose_Safe()
        {
            var vm = new MainWindowViewModel(CreateAggregator());
            vm.Dispose();
            var act = () => vm.Dispose();
            act.Should().NotThrow();
        }

        [Fact]
        public void MonitoredCharacterEvent_DoesNotThrow()
        {
            var agg = CreateAggregator();
            var vm = new MainWindowViewModel(agg);

            var act = () => agg.Publish(MonitoredCharacterCollectionChangedEvent.Instance);
            act.Should().NotThrow();

            vm.Dispose();
        }
    }
}
