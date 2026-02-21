// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Services;
using EVEMon.Common.ViewModels.Lists;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.ViewModels.Lists
{
    public class ContractsListViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        [Fact]
        public void HideInactive_DefaultFalse()
        {
            var vm = new ContractsListViewModel(CreateAggregator());
            vm.HideInactive.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void HideInactive_RaisesPropertyChanged()
        {
            var vm = new ContractsListViewModel(CreateAggregator());
            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.HideInactive))
                    changedProp = e.PropertyName;
            };

            vm.HideInactive = true;

            changedProp.Should().Be("HideInactive");
            vm.Dispose();
        }

        [Fact]
        public void HideInactive_SetTrue_TriggersRefresh()
        {
            var vm = new ContractsListViewModel(CreateAggregator());
            bool groupedItemsChanged = false;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.GroupedItems))
                    groupedItemsChanged = true;
            };

            vm.HideInactive = true;

            groupedItemsChanged.Should().BeTrue();
            vm.GroupedItems.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void ShowIssuedFor_DefaultAll()
        {
            var vm = new ContractsListViewModel(CreateAggregator());
            vm.ShowIssuedFor.Should().Be(IssuedFor.All);
            vm.Dispose();
        }

        [Fact]
        public void ShowIssuedFor_RaisesPropertyChanged()
        {
            var vm = new ContractsListViewModel(CreateAggregator());
            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.ShowIssuedFor))
                    changedProp = e.PropertyName;
            };

            vm.ShowIssuedFor = IssuedFor.Character;

            changedProp.Should().Be("ShowIssuedFor");
            vm.Dispose();
        }

        [Fact]
        public void ShowIssuedFor_SetCharacter_TriggersRefresh()
        {
            var vm = new ContractsListViewModel(CreateAggregator());
            bool groupedItemsChanged = false;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.GroupedItems))
                    groupedItemsChanged = true;
            };

            vm.ShowIssuedFor = IssuedFor.Character;

            groupedItemsChanged.Should().BeTrue();
            vm.GroupedItems.Should().NotBeNull();
            vm.Dispose();
        }
    }
}
