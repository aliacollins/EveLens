// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;
using EVEMon.Common.ViewModels;
using EVEMon.Common.Services;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.ViewModels
{
    public class FormViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        private sealed class TestFormViewModel : FormViewModel
        {
            public bool ApplyCalled { get; private set; }
            public bool CancelCalled { get; private set; }
            private string _value = string.Empty;

            public TestFormViewModel(IEventAggregator aggregator) : base(aggregator) { }

            public string Value
            {
                get => _value;
                set
                {
                    if (SetProperty(ref _value, value))
                        MarkDirty();
                }
            }

            protected override void OnApply() => ApplyCalled = true;
            protected override void OnCancel() => CancelCalled = true;
        }

        [Fact]
        public void IsDirty_InitiallyFalse()
        {
            var vm = new TestFormViewModel(CreateAggregator());

            vm.IsDirty.Should().BeFalse();
        }

        [Fact]
        public void IsDirty_TrueAfterPropertyChange()
        {
            var vm = new TestFormViewModel(CreateAggregator());

            vm.Value = "changed";

            vm.IsDirty.Should().BeTrue();
        }

        [Fact]
        public void IsDirty_RaisesPropertyChanged()
        {
            var vm = new TestFormViewModel(CreateAggregator());
            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FormViewModel.IsDirty))
                    changedProp = e.PropertyName;
            };

            vm.Value = "changed";

            changedProp.Should().Be(nameof(FormViewModel.IsDirty));
        }

        [Fact]
        public void Apply_CallsOnApply()
        {
            var vm = new TestFormViewModel(CreateAggregator());
            vm.Value = "changed";

            vm.Apply();

            vm.ApplyCalled.Should().BeTrue();
        }

        [Fact]
        public void Apply_ResetsDirtyFlag()
        {
            var vm = new TestFormViewModel(CreateAggregator());
            vm.Value = "changed";
            vm.IsDirty.Should().BeTrue();

            vm.Apply();

            vm.IsDirty.Should().BeFalse();
        }

        [Fact]
        public void Cancel_CallsOnCancel()
        {
            var vm = new TestFormViewModel(CreateAggregator());
            vm.Value = "changed";

            vm.Cancel();

            vm.CancelCalled.Should().BeTrue();
        }

        [Fact]
        public void Cancel_ResetsDirtyFlag()
        {
            var vm = new TestFormViewModel(CreateAggregator());
            vm.Value = "changed";
            vm.IsDirty.Should().BeTrue();

            vm.Cancel();

            vm.IsDirty.Should().BeFalse();
        }

        [Fact]
        public void Apply_WhenNotDirty_StillCallsOnApply()
        {
            var vm = new TestFormViewModel(CreateAggregator());
            vm.IsDirty.Should().BeFalse();

            vm.Apply();

            vm.ApplyCalled.Should().BeTrue();
            vm.IsDirty.Should().BeFalse();
        }
    }
}
