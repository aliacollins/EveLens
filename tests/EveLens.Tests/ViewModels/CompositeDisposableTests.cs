// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.ViewModels.Binding;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.ViewModels
{
    public class CompositeDisposableTests
    {
        [Fact]
        public void Add_IncrementsCount()
        {
            var cd = new CompositeDisposable();
            cd.Count.Should().Be(0);

            cd.Add(new TestDisposable());

            cd.Count.Should().Be(1);
        }

        [Fact]
        public void Dispose_DisposesAllItems()
        {
            var cd = new CompositeDisposable();
            var d1 = new TestDisposable();
            var d2 = new TestDisposable();
            cd.Add(d1);
            cd.Add(d2);

            cd.Dispose();

            d1.IsDisposed.Should().BeTrue();
            d2.IsDisposed.Should().BeTrue();
        }

        [Fact]
        public void Dispose_DisposesInReverseOrder()
        {
            var cd = new CompositeDisposable();
            int order = 0;
            int d1Order = -1, d2Order = -1;

            cd.Add(new ActionTestDisposable(() => d1Order = order++));
            cd.Add(new ActionTestDisposable(() => d2Order = order++));

            cd.Dispose();

            d2Order.Should().Be(0, "second item should be disposed first (LIFO)");
            d1Order.Should().Be(1, "first item should be disposed second (LIFO)");
        }

        [Fact]
        public void Dispose_MultipleCalls_Safe()
        {
            var cd = new CompositeDisposable();
            var d = new TestDisposable();
            cd.Add(d);

            cd.Dispose();
            var act = () => cd.Dispose();

            act.Should().NotThrow();
            d.DisposeCount.Should().Be(1, "items should only be disposed once");
        }

        [Fact]
        public void Dispose_SetsIsDisposed()
        {
            var cd = new CompositeDisposable();
            cd.IsDisposed.Should().BeFalse();

            cd.Dispose();

            cd.IsDisposed.Should().BeTrue();
        }

        [Fact]
        public void Add_AfterDispose_ImmediatelyDisposesItem()
        {
            var cd = new CompositeDisposable();
            cd.Dispose();

            var d = new TestDisposable();
            cd.Add(d);

            d.IsDisposed.Should().BeTrue();
        }

        [Fact]
        public void Add_NullArg_Throws()
        {
            var cd = new CompositeDisposable();

            Action act = () => cd.Add(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Dispose_ExceptionInItem_DoesNotPreventOtherDisposals()
        {
            var cd = new CompositeDisposable();
            var d1 = new TestDisposable();
            cd.Add(d1);
            cd.Add(new ThrowingDisposable());
            var d3 = new TestDisposable();
            cd.Add(d3);

            // Should not throw - exceptions are caught internally
            var act = () => cd.Dispose();
            act.Should().NotThrow();

            // d3 is disposed first (reverse order), then ThrowingDisposable throws, then d1 is still disposed
            d1.IsDisposed.Should().BeTrue();
            d3.IsDisposed.Should().BeTrue();
        }

        private sealed class TestDisposable : IDisposable
        {
            public bool IsDisposed { get; private set; }
            public int DisposeCount { get; private set; }

            public void Dispose()
            {
                IsDisposed = true;
                DisposeCount++;
            }
        }

        private sealed class ActionTestDisposable : IDisposable
        {
            private readonly Action _action;
            public ActionTestDisposable(Action action) => _action = action;
            public void Dispose() => _action();
        }

        private sealed class ThrowingDisposable : IDisposable
        {
            public void Dispose() => throw new InvalidOperationException("Test exception");
        }
    }
}
