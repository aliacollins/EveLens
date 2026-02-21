// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.ComponentModel;
using EVEMon.Common.ViewModels;
using EVEMon.Common.Services;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EVEMon.Tests.ViewModels
{
    public class ViewModelBaseTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        private static IDispatcher CreateSyncDispatcher()
        {
            var dispatcher = Substitute.For<IDispatcher>();
            dispatcher.When(d => d.Invoke(Arg.Any<Action>())).Do(ci => ci.ArgAt<Action>(0).Invoke());
            dispatcher.When(d => d.Post(Arg.Any<Action>())).Do(ci => ci.ArgAt<Action>(0).Invoke());
            return dispatcher;
        }

        // Concrete test subclass
        private sealed class TestViewModel : ViewModelBase
        {
            private string _name = string.Empty;
            private int _count;

            public TestViewModel(IEventAggregator aggregator, IDispatcher? dispatcher = null)
                : base(aggregator, dispatcher) { }

            public string Name
            {
                get => _name;
                set => SetProperty(ref _name, value);
            }

            public int Count
            {
                get => _count;
                set => SetPropertyOnUI(ref _count, value);
            }

            public void SubscribeTo<TEvent>(Action<TEvent> handler) where TEvent : class
                => Subscribe(handler);

            public void TrackDisposable(IDisposable d) => Track(d);

            public new bool IsDisposed => base.IsDisposed;
        }

        [Fact]
        public void Constructor_WithAggregator_DoesNotThrow()
        {
            var agg = CreateAggregator();
            var vm = new TestViewModel(agg);
            vm.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_NullAggregator_Throws()
        {
            Action act = () => new TestViewModel(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("eventAggregator");
        }

        [Fact]
        public void SetProperty_RaisesPropertyChanged()
        {
            var vm = new TestViewModel(CreateAggregator());
            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProp = e.PropertyName;

            vm.Name = "Test";

            changedProp.Should().Be(nameof(TestViewModel.Name));
        }

        [Fact]
        public void SetProperty_SameValue_DoesNotRaise()
        {
            var vm = new TestViewModel(CreateAggregator());
            vm.Name = "Test";

            bool raised = false;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => raised = true;

            vm.Name = "Test"; // same value

            raised.Should().BeFalse();
        }

        [Fact]
        public void SetPropertyOnUI_WithDispatcher_RaisesViaDispatcher()
        {
            var dispatcher = CreateSyncDispatcher();
            var vm = new TestViewModel(CreateAggregator(), dispatcher);

            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProp = e.PropertyName;

            vm.Count = 42;

            changedProp.Should().Be(nameof(TestViewModel.Count));
            dispatcher.Received(1).Post(Arg.Any<Action>());
        }

        [Fact]
        public void SetPropertyOnUI_WithoutDispatcher_RaisesDirectly()
        {
            var vm = new TestViewModel(CreateAggregator());

            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProp = e.PropertyName;

            vm.Count = 42;

            changedProp.Should().Be(nameof(TestViewModel.Count));
        }

        [Fact]
        public void SetPropertyOnUI_SameValue_DoesNotRaise()
        {
            var vm = new TestViewModel(CreateAggregator());
            vm.Count = 5;

            bool raised = false;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => raised = true;

            vm.Count = 5;

            raised.Should().BeFalse();
        }

        [Fact]
        public void Subscribe_TracksAndReceivesEvents()
        {
            var agg = CreateAggregator();
            var vm = new TestViewModel(agg);

            bool received = false;
            vm.SubscribeTo<TestEvent>(e => received = true);

            agg.Publish(new TestEvent());

            received.Should().BeTrue();
        }

        [Fact]
        public void Dispose_UnsubscribesEvents()
        {
            var agg = CreateAggregator();
            var vm = new TestViewModel(agg);

            bool received = false;
            vm.SubscribeTo<TestEvent>(e => received = true);

            vm.Dispose();

            agg.Publish(new TestEvent());
            received.Should().BeFalse();
        }

        [Fact]
        public void Dispose_SetsIsDisposed()
        {
            var vm = new TestViewModel(CreateAggregator());
            vm.IsDisposed.Should().BeFalse();

            vm.Dispose();

            vm.IsDisposed.Should().BeTrue();
        }

        [Fact]
        public void Dispose_MultipleCalls_Safe()
        {
            var vm = new TestViewModel(CreateAggregator());

            vm.Dispose();
            var act = () => vm.Dispose();

            act.Should().NotThrow();
        }

        [Fact]
        public void Track_DisposedOnDispose()
        {
            var vm = new TestViewModel(CreateAggregator());
            bool disposed = false;
            var trackedDisposable = Substitute.For<IDisposable>();
            trackedDisposable.When(d => d.Dispose()).Do(_ => disposed = true);

            vm.TrackDisposable(trackedDisposable);
            vm.Dispose();

            disposed.Should().BeTrue();
        }

        [Fact]
        public void Track_NullArg_Throws()
        {
            var vm = new TestViewModel(CreateAggregator());

            Action act = () => vm.TrackDisposable(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Subscribe_NullHandler_Throws()
        {
            var vm = new TestViewModel(CreateAggregator());

            Action act = () => vm.SubscribeTo<TestEvent>(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        // Test event class
        private sealed class TestEvent { }
    }
}
