// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.ViewModels;
using EveLens.Common.Services;
using EveLens.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.ViewModels
{
    public class ShipBrowserViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        [Fact]
        public void CanInstantiate()
        {
            var vm = new ShipBrowserViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_HasEmptyGroups()
        {
            var vm = new ShipBrowserViewModel(CreateAggregator());
            vm.Groups.Should().NotBeNull();
            vm.SelectedShip.Should().BeNull();
            vm.TextFilter.Should().BeEmpty();
            vm.ShowCanFlyOnly.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void TextFilter_DoesNotThrow()
        {
            var vm = new ShipBrowserViewModel(CreateAggregator());
            var act = () => vm.TextFilter = "Rifter";
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void ShowCanFlyOnly_DoesNotThrow()
        {
            var vm = new ShipBrowserViewModel(CreateAggregator());
            var act = () => vm.ShowCanFlyOnly = true;
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void Refresh_WithNullCharacter_DoesNotThrow()
        {
            var vm = new ShipBrowserViewModel(CreateAggregator());
            var act = () => vm.Refresh();
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void PlanToFly_WithNullShip_DoesNotThrow()
        {
            var vm = new ShipBrowserViewModel(CreateAggregator());
            var act = () => vm.PlanToFly(null!);
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void SelectShip_WithNull_ClearsDetail()
        {
            var vm = new ShipBrowserViewModel(CreateAggregator());
            vm.SelectShip(null);
            vm.SelectedShip.Should().BeNull();
            vm.SelectedShipDetail.Should().BeNull();
            vm.Dispose();
        }

        [Fact]
        public void CollapseAll_DoesNotThrow()
        {
            var vm = new ShipBrowserViewModel(CreateAggregator());
            var act = () => vm.CollapseAll();
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void ExpandAll_DoesNotThrow()
        {
            var vm = new ShipBrowserViewModel(CreateAggregator());
            var act = () => vm.ExpandAll();
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void Dispose_Safe()
        {
            var vm = new ShipBrowserViewModel(CreateAggregator());
            vm.Dispose();
            var act = () => vm.Dispose();
            act.Should().NotThrow();
        }
    }
}
