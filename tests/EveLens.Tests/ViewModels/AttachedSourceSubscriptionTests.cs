// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using EveLens.Common.ViewModels.Binding;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.ViewModels
{
    public class AttachedSourceSubscriptionTests
    {
        [Fact]
        public void Changes_are_forwarded_while_attached()
        {
            var source = new Source();
            var calls = new List<Source>();
            var subscription = new AttachedSourceSubscription<Source>(calls.Add);
            subscription.SetSource(source);
            subscription.Attach();
            calls.Clear();

            source.Raise();

            calls.Should().Equal(source);
        }

        [Fact]
        public void Changes_are_not_forwarded_while_detached()
        {
            var source = new Source();
            var calls = new List<Source>();
            var subscription = new AttachedSourceSubscription<Source>(calls.Add);
            subscription.SetSource(source);
            subscription.Attach();
            subscription.Detach();
            calls.Clear();

            source.Raise();

            calls.Should().BeEmpty();
            source.HandlerCount.Should().Be(0);
        }

        [Fact]
        public void Reattaching_with_the_same_source_resumes_forwarding()
        {
            var source = new Source();
            var calls = new List<Source>();
            var subscription = new AttachedSourceSubscription<Source>(calls.Add);
            subscription.SetSource(source);
            subscription.Attach();
            subscription.Detach();
            subscription.Attach();
            calls.Clear();

            source.Raise();

            calls.Should().Equal(source);
            subscription.Source.Should().BeSameAs(source);
        }

        [Fact]
        public void Attach_invokes_the_callback_once_for_the_current_source()
        {
            var source = new Source();
            var calls = new List<Source>();
            var subscription = new AttachedSourceSubscription<Source>(calls.Add);
            subscription.SetSource(source);

            subscription.Attach();

            calls.Should().Equal(source);
        }

        [Fact]
        public void Attach_without_a_source_does_not_invoke_the_callback()
        {
            var calls = new List<Source>();
            var subscription = new AttachedSourceSubscription<Source>(calls.Add);

            subscription.Attach();

            calls.Should().BeEmpty();
        }

        [Fact]
        public void Repeated_attach_subscribes_once()
        {
            var source = new Source();
            var calls = new List<Source>();
            var subscription = new AttachedSourceSubscription<Source>(calls.Add);
            subscription.SetSource(source);

            subscription.Attach();
            subscription.Attach();

            source.HandlerCount.Should().Be(1);
            calls.Should().HaveCount(1);
        }

        [Fact]
        public void Source_set_after_attach_is_subscribed_without_invoking_the_callback()
        {
            var source = new Source();
            var calls = new List<Source>();
            var subscription = new AttachedSourceSubscription<Source>(calls.Add);
            subscription.Attach();

            subscription.SetSource(source);

            calls.Should().BeEmpty();
            source.Raise();
            calls.Should().Equal(source);
        }

        [Fact]
        public void Replacing_the_source_unsubscribes_the_previous_one()
        {
            var first = new Source();
            var second = new Source();
            var calls = new List<Source>();
            var subscription = new AttachedSourceSubscription<Source>(calls.Add);
            subscription.Attach();
            subscription.SetSource(first);

            subscription.SetSource(second);
            first.Raise();
            second.Raise();

            first.HandlerCount.Should().Be(0);
            calls.Should().Equal(second);
        }

        [Fact]
        public void Clearing_the_source_unsubscribes()
        {
            var source = new Source();
            var subscription = new AttachedSourceSubscription<Source>(_ => { });
            subscription.Attach();
            subscription.SetSource(source);

            subscription.SetSource(null);

            source.HandlerCount.Should().Be(0);
            subscription.Source.Should().BeNull();
        }

        [Fact]
        public void Every_view_that_handles_detach_also_handles_attach()
        {
            var viewsDir = Path.Combine(FindProjectRoot(), "src", "EveLens.Avalonia", "Views");
            var offenders = Directory.GetFiles(viewsDir, "*.cs", SearchOption.AllDirectories)
                .Where(file =>
                {
                    var text = File.ReadAllText(file);
                    return text.Contains("override void OnDetachedFromVisualTree")
                        && !text.Contains("override void OnAttachedToVisualTree");
                })
                .Select(Path.GetFileName)
                .ToList();

            offenders.Should().BeEmpty(
                "a view that releases subscriptions on detach restores them on attach");
        }

        private static string FindProjectRoot()
        {
            var dir = Directory.GetCurrentDirectory();
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir, "EveLens.sln")))
                    return dir;
                dir = Directory.GetParent(dir)?.FullName;
            }
            return Directory.GetCurrentDirectory();
        }

        private sealed class Source : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;

            public int HandlerCount => PropertyChanged?.GetInvocationList().Length ?? 0;

            public void Raise() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
        }
    }
}
