// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.ViewModels;
using EveLens.Core.Interfaces;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.ViewModels
{
    public class CharacterMonitorBodyViewModelTests
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
            var vm = new CharacterMonitorBodyViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_SelectedPageEmpty()
        {
            var vm = new CharacterMonitorBodyViewModel(CreateAggregator());
            vm.SelectedPage.Should().BeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_IsVisibleFalse()
        {
            var vm = new CharacterMonitorBodyViewModel(CreateAggregator());
            vm.IsVisible.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void SelectPage_SetsSelectedPage()
        {
            var vm = new CharacterMonitorBodyViewModel(CreateAggregator());

            vm.SelectPage("Assets");

            vm.SelectedPage.Should().Be("Assets");
            vm.Dispose();
        }

        [Fact]
        public void SelectPage_Null_SetsEmpty()
        {
            var vm = new CharacterMonitorBodyViewModel(CreateAggregator());
            vm.SelectPage("Assets");

            vm.SelectPage(null!);

            vm.SelectedPage.Should().BeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void SelectedPage_RaisesPropertyChanged()
        {
            var vm = new CharacterMonitorBodyViewModel(CreateAggregator());
            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CharacterMonitorBodyViewModel.SelectedPage))
                    changedProp = e.PropertyName;
            };

            vm.SelectPage("MarketOrders");

            changedProp.Should().Be("SelectedPage");
            vm.Dispose();
        }

        [Fact]
        public void IsVisible_RaisesPropertyChanged()
        {
            var vm = new CharacterMonitorBodyViewModel(CreateAggregator());
            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CharacterMonitorBodyViewModel.IsVisible))
                    changedProp = e.PropertyName;
            };

            vm.IsVisible = true;

            changedProp.Should().Be("IsVisible");
            vm.Dispose();
        }

        [Fact]
        public void Character_CanBeSet()
        {
            var vm = new CharacterMonitorBodyViewModel(CreateAggregator());
            var character = CreateTestCharacter(1L, "Test Pilot");

            vm.Character = character;

            vm.Character.Should().Be(character);
            vm.Dispose();
        }

        [Fact]
        public void Character_RaisesPropertyChanged()
        {
            var vm = new CharacterMonitorBodyViewModel(CreateAggregator());
            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CharacterMonitorBodyViewModel.Character))
                    changedProp = e.PropertyName;
            };

            vm.Character = CreateTestCharacter(1L, "Test");

            changedProp.Should().Be("Character");
            vm.Dispose();
        }

        [Fact]
        public void Dispose_Safe()
        {
            var vm = new CharacterMonitorBodyViewModel(CreateAggregator());
            vm.Dispose();
            var act = () => vm.Dispose();
            act.Should().NotThrow();
        }
    }
}
