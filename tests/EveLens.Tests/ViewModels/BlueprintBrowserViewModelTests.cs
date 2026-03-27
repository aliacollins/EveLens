// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Services;
using EveLens.Common.ViewModels;
using EveLens.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.ViewModels
{
    public class BlueprintBrowserViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        [Fact]
        public void CanInstantiate()
        {
            var vm = new BlueprintBrowserViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_HasEmptyFlattenedNodes()
        {
            var vm = new BlueprintBrowserViewModel(CreateAggregator());
            vm.FlattenedNodes.Should().NotBeNull();
            vm.FlattenedNodes.Should().BeEmpty();
            vm.SelectedBlueprint.Should().BeNull();
            vm.TextFilter.Should().BeEmpty();
            vm.ShowCanBuildOnly.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void TextFilter_DoesNotThrow()
        {
            var vm = new BlueprintBrowserViewModel(CreateAggregator());
            var act = () => vm.TextFilter = "Rifter";
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void Refresh_WithNullCharacter_DoesNotThrow()
        {
            var vm = new BlueprintBrowserViewModel(CreateAggregator());
            var act = () => vm.Refresh();
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void PlanToBuild_WithNullBlueprint_DoesNotThrow()
        {
            var vm = new BlueprintBrowserViewModel(CreateAggregator());
            var act = () => vm.PlanToBuild(null!);
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void SelectBlueprint_WithNull_ClearsDetail()
        {
            var vm = new BlueprintBrowserViewModel(CreateAggregator());
            vm.SelectBlueprint(null);
            vm.SelectedBlueprint.Should().BeNull();
            vm.SelectedBlueprintDetail.Should().BeNull();
            vm.Dispose();
        }

        [Fact]
        public void CollapseAll_WithNoNodes_DoesNotThrow()
        {
            var vm = new BlueprintBrowserViewModel(CreateAggregator());
            var act = () => vm.CollapseAll();
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void ExpandAll_WithNoNodes_DoesNotThrow()
        {
            var vm = new BlueprintBrowserViewModel(CreateAggregator());
            var act = () => vm.ExpandAll();
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void ToggleNode_WithNull_DoesNotThrow()
        {
            var vm = new BlueprintBrowserViewModel(CreateAggregator());
            var act = () => vm.ToggleNode(null!);
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void ShowCanBuildOnly_CanBeToggled()
        {
            var vm = new BlueprintBrowserViewModel(CreateAggregator());
            vm.ShowCanBuildOnly = true;
            vm.ShowCanBuildOnly.Should().BeTrue();
            vm.ShowCanBuildOnly = false;
            vm.ShowCanBuildOnly.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void Dispose_Safe()
        {
            var vm = new BlueprintBrowserViewModel(CreateAggregator());
            vm.Dispose();
            var act = () => vm.Dispose();
            act.Should().NotThrow();
        }
    }
}
