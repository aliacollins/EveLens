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
    public class ItemBrowserViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        [Fact]
        public void CanInstantiate()
        {
            var vm = new ItemBrowserViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_HasEmptyGroups()
        {
            var vm = new ItemBrowserViewModel(CreateAggregator());
            vm.Groups.Should().NotBeNull();
            vm.SelectedItem.Should().BeNull();
            vm.TextFilter.Should().BeEmpty();
            vm.ShowCanUseOnly.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void TextFilter_DoesNotThrow()
        {
            var vm = new ItemBrowserViewModel(CreateAggregator());
            var act = () => vm.TextFilter = "Damage Control";
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void ShowCanUseOnly_DoesNotThrow()
        {
            var vm = new ItemBrowserViewModel(CreateAggregator());
            var act = () => vm.ShowCanUseOnly = true;
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void Refresh_WithNullCharacter_DoesNotThrow()
        {
            var vm = new ItemBrowserViewModel(CreateAggregator());
            var act = () => vm.Refresh();
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void PlanToUse_WithNullItem_DoesNotThrow()
        {
            var vm = new ItemBrowserViewModel(CreateAggregator());
            var act = () => vm.PlanToUse(null!);
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void SelectItem_WithNull_ClearsDetail()
        {
            var vm = new ItemBrowserViewModel(CreateAggregator());
            vm.SelectItem(null);
            vm.SelectedItem.Should().BeNull();
            vm.SelectedItemDetail.Should().BeNull();
            vm.Dispose();
        }

        [Fact]
        public void CollapseAll_WithNullGroups_DoesNotThrow()
        {
            var vm = new ItemBrowserViewModel(CreateAggregator());
            var act = () => vm.CollapseAll();
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void ExpandAll_WithNullGroups_DoesNotThrow()
        {
            var vm = new ItemBrowserViewModel(CreateAggregator());
            var act = () => vm.ExpandAll();
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void Dispose_Safe()
        {
            var vm = new ItemBrowserViewModel(CreateAggregator());
            vm.Dispose();
            var act = () => vm.Dispose();
            act.Should().NotThrow();
        }
    }
}
