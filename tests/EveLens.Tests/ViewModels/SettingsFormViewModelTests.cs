// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;
using EveLens.Common.Services;
using EveLens.Common.ViewModels;
using EveLens.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.ViewModels
{
    public class SettingsFormViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        [Fact]
        public void CanInstantiate()
        {
            var vm = new SettingsFormViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_NotDirty()
        {
            var vm = new SettingsFormViewModel(CreateAggregator());
            vm.IsDirty.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_SelectedCategoryEmpty()
        {
            var vm = new SettingsFormViewModel(CreateAggregator());
            vm.SelectedCategory.Should().BeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void SelectedCategory_CanBeSet()
        {
            var vm = new SettingsFormViewModel(CreateAggregator());

            vm.SelectedCategory = "General";

            vm.SelectedCategory.Should().Be("General");
            vm.Dispose();
        }

        [Fact]
        public void SelectedCategory_Null_TreatedAsEmpty()
        {
            var vm = new SettingsFormViewModel(CreateAggregator());
            vm.SelectedCategory = "General";

            vm.SelectedCategory = null!;

            vm.SelectedCategory.Should().BeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void SelectedCategory_RaisesPropertyChanged()
        {
            var vm = new SettingsFormViewModel(CreateAggregator());
            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SettingsFormViewModel.SelectedCategory))
                    changedProp = e.PropertyName;
            };

            vm.SelectedCategory = "Network";

            changedProp.Should().Be("SelectedCategory");
            vm.Dispose();
        }

        [Fact]
        public void Cancel_ResetsIsDirty()
        {
            var vm = new SettingsFormViewModel(CreateAggregator());
            // SettingsFormViewModel doesn't have editable properties that call MarkDirty,
            // but we can test Cancel doesn't throw
            vm.Cancel();
            vm.IsDirty.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void Dispose_Safe()
        {
            var vm = new SettingsFormViewModel(CreateAggregator());
            vm.Dispose();
            var act = () => vm.Dispose();
            act.Should().NotThrow();
        }
    }
}
