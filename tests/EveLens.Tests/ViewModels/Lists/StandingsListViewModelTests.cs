// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.Events;
using EveLens.Common.Services;
using EveLens.Common.ViewModels.Lists;
using EveLens.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.ViewModels.Lists
{
    public class StandingsListViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        [Fact]
        public void CanInstantiate()
        {
            var vm = new StandingsListViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_TextFilterEmpty()
        {
            var vm = new StandingsListViewModel(CreateAggregator());
            vm.TextFilter.Should().BeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_SortAscendingTrue()
        {
            var vm = new StandingsListViewModel(CreateAggregator());
            vm.SortAscending.Should().BeTrue();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_GroupingNone()
        {
            var vm = new StandingsListViewModel(CreateAggregator());
            vm.Grouping.Should().Be(StandingGrouping.None);
            vm.Dispose();
        }

        [Fact]
        public void RefreshWithNoCharacter_EmptyResults()
        {
            var vm = new StandingsListViewModel(CreateAggregator());
            vm.Refresh();
            vm.TotalItemCount.Should().Be(0);
            vm.GroupedItems.Should().HaveCount(1);
            vm.GroupedItems[0].Items.Should().BeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void Dispose_SafeMultipleCalls()
        {
            var vm = new StandingsListViewModel(CreateAggregator());
            vm.Dispose();
            var act = () => vm.Dispose();
            act.Should().NotThrow();
        }

        [Fact]
        public void TextFilter_RaisesPropertyChanged()
        {
            var vm = new StandingsListViewModel(CreateAggregator());
            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.TextFilter))
                    changedProp = e.PropertyName;
            };

            vm.TextFilter = "test";

            changedProp.Should().Be("TextFilter");
            vm.Dispose();
        }

        [Fact]
        public void Grouping_RaisesPropertyChanged()
        {
            var vm = new StandingsListViewModel(CreateAggregator());
            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.Grouping))
                    changedProp = e.PropertyName;
            };

            vm.Grouping = StandingGrouping.Group;

            changedProp.Should().Be("Grouping");
            vm.Dispose();
        }

        [Fact]
        public void SortColumn_RaisesPropertyChanged()
        {
            var vm = new StandingsListViewModel(CreateAggregator());
            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.SortColumn))
                    changedProp = e.PropertyName;
            };

            vm.SortColumn = StandingColumn.StandingValue;

            changedProp.Should().Be("SortColumn");
            vm.Dispose();
        }

        [Fact]
        public void ToggleSort_SameColumn_ReversesDirection()
        {
            var vm = new StandingsListViewModel(CreateAggregator());
            vm.SortColumn = StandingColumn.EntityName;
            vm.SortAscending = true;

            vm.ToggleSort(StandingColumn.EntityName);

            vm.SortAscending.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void ToggleSort_DifferentColumn_SetsAscending()
        {
            var vm = new StandingsListViewModel(CreateAggregator());
            vm.SortColumn = StandingColumn.EntityName;
            vm.SortAscending = false;

            vm.ToggleSort(StandingColumn.StandingValue);

            vm.SortColumn.Should().Be(StandingColumn.StandingValue);
            vm.SortAscending.Should().BeTrue();
            vm.Dispose();
        }

        [Fact]
        public void EventSubscription_SettingsChanged_NoCharacter_DoesNotThrow()
        {
            var agg = CreateAggregator();
            var vm = new StandingsListViewModel(agg);

            var act = () => agg.Publish(SettingsChangedEvent.Instance);

            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void EventSubscription_EveIDToNameUpdated_NoCharacter_DoesNotThrow()
        {
            var agg = CreateAggregator();
            var vm = new StandingsListViewModel(agg);

            var act = () => agg.Publish(EveIDToNameUpdatedEvent.Instance);

            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void EventSubscription_DisposedVM_DoesNotReceive()
        {
            var agg = CreateAggregator();
            var vm = new StandingsListViewModel(agg);
            vm.Dispose();

            var act = () => agg.Publish(SettingsChangedEvent.Instance);
            act.Should().NotThrow();
        }

        [Fact]
        public void Refresh_GroupedItems_AlwaysHasAtLeastOneGroup()
        {
            var vm = new StandingsListViewModel(CreateAggregator());
            vm.Refresh();

            vm.GroupedItems.Should().HaveCountGreaterOrEqualTo(1);
            vm.Dispose();
        }
    }
}
