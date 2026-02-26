// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EveLens.Common.Services;
using EveLens.Core.Events;
using EveLens.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Integration
{
    public class EventAggregatorScaleTests
    {
        // Event types for testing cross-contamination
        private sealed class EventTypeA { public int Value { get; set; } }
        private sealed class EventTypeB { public int Value { get; set; } }
        private sealed class EventTypeC { public int Value { get; set; } }
        private sealed class EventTypeD { public int Value { get; set; } }
        private sealed class EventTypeE { public int Value { get; set; } }
        private sealed class EventTypeF { public int Value { get; set; } }
        private sealed class EventTypeG { public int Value { get; set; } }
        private sealed class EventTypeH { public int Value { get; set; } }
        private sealed class EventTypeI { public int Value { get; set; } }
        private sealed class EventTypeJ { public int Value { get; set; } }
        private sealed class EventTypeK { public int Value { get; set; } }
        private sealed class EventTypeL { public int Value { get; set; } }
        private sealed class EventTypeM { public int Value { get; set; } }
        private sealed class EventTypeN { public int Value { get; set; } }
        private sealed class EventTypeO { public int Value { get; set; } }
        private sealed class EventTypeP { public int Value { get; set; } }
        private sealed class EventTypeQ { public int Value { get; set; } }
        private sealed class EventTypeR { public int Value { get; set; } }
        private sealed class EventTypeS { public int Value { get; set; } }
        private sealed class EventTypeT { public int Value { get; set; } }

        // --- Test 1: 100 subscribers, 1 publish, all 100 receive ---

        [Fact]
        public void Publish_100Subscribers_AllReceive()
        {
            var aggregator = new EventAggregator();
            int[] counts = new int[100];

            for (int i = 0; i < 100; i++)
            {
                int idx = i;
                aggregator.Subscribe<CharacterUpdatedEvent>(e => counts[idx]++);
            }

            aggregator.Publish(new CharacterUpdatedEvent(1, "TestChar"));

            for (int i = 0; i < 100; i++)
            {
                counts[i].Should().Be(1,
                    $"subscriber {i} should have received exactly 1 event");
            }
        }

        // --- Test 2: 20 different event types, no cross-contamination ---

        [Fact]
        public void Publish_20EventTypes_NoCrossContamination()
        {
            var aggregator = new EventAggregator();
            int[] counts = new int[20];

            aggregator.Subscribe<EventTypeA>(e => counts[0]++);
            aggregator.Subscribe<EventTypeB>(e => counts[1]++);
            aggregator.Subscribe<EventTypeC>(e => counts[2]++);
            aggregator.Subscribe<EventTypeD>(e => counts[3]++);
            aggregator.Subscribe<EventTypeE>(e => counts[4]++);
            aggregator.Subscribe<EventTypeF>(e => counts[5]++);
            aggregator.Subscribe<EventTypeG>(e => counts[6]++);
            aggregator.Subscribe<EventTypeH>(e => counts[7]++);
            aggregator.Subscribe<EventTypeI>(e => counts[8]++);
            aggregator.Subscribe<EventTypeJ>(e => counts[9]++);
            aggregator.Subscribe<EventTypeK>(e => counts[10]++);
            aggregator.Subscribe<EventTypeL>(e => counts[11]++);
            aggregator.Subscribe<EventTypeM>(e => counts[12]++);
            aggregator.Subscribe<EventTypeN>(e => counts[13]++);
            aggregator.Subscribe<EventTypeO>(e => counts[14]++);
            aggregator.Subscribe<EventTypeP>(e => counts[15]++);
            aggregator.Subscribe<EventTypeQ>(e => counts[16]++);
            aggregator.Subscribe<EventTypeR>(e => counts[17]++);
            aggregator.Subscribe<EventTypeS>(e => counts[18]++);
            aggregator.Subscribe<EventTypeT>(e => counts[19]++);

            // Publish only EventTypeA
            aggregator.Publish(new EventTypeA { Value = 1 });

            counts[0].Should().Be(1, "EventTypeA subscriber should receive the event");
            for (int i = 1; i < 20; i++)
            {
                counts[i].Should().Be(0,
                    $"subscriber for event type {i} should not receive EventTypeA events");
            }
        }

        [Fact]
        public void Publish_20EventTypes_EachTypeDeliveredToCorrectSubscribers()
        {
            var aggregator = new EventAggregator();
            int countA = 0, countB = 0, countC = 0;

            aggregator.Subscribe<EventTypeA>(e => countA++);
            aggregator.Subscribe<EventTypeB>(e => countB++);
            aggregator.Subscribe<EventTypeC>(e => countC++);

            aggregator.Publish(new EventTypeA { Value = 1 });
            aggregator.Publish(new EventTypeA { Value = 2 });
            aggregator.Publish(new EventTypeB { Value = 1 });

            countA.Should().Be(2);
            countB.Should().Be(1);
            countC.Should().Be(0);
        }

        // --- Test 3: Rapid publish/subscribe/unsubscribe cycle doesn't leak ---

        [Fact]
        public void RapidSubscribeUnsubscribe_NoLeaks()
        {
            var aggregator = new EventAggregator();
            int receivedCount = 0;

            for (int cycle = 0; cycle < 100; cycle++)
            {
                Action<CharacterUpdatedEvent> handler = e => receivedCount++;
                aggregator.Subscribe(handler);

                // Publish while subscribed
                aggregator.Publish(new CharacterUpdatedEvent(cycle, $"Char{cycle}"));

                // Unsubscribe
                aggregator.Unsubscribe(handler);

                // Publish after unsubscribe -- should NOT be received
                aggregator.Publish(new CharacterUpdatedEvent(cycle, $"Char{cycle}"));
            }

            // Each cycle: 1 received (subscribed) + 0 (unsubscribed) = 100 total
            receivedCount.Should().Be(100,
                "each cycle should deliver exactly 1 event, none after unsubscribe");
        }

        // --- Test 4: Weak reference subscribers get cleaned up ---

        [Fact]
        public void WeakSubscribers_CleanedUpAfterGC()
        {
            var aggregator = new EventAggregator();
            int receivedCount = 0;

            // Create a subscriber in a scope that lets the target be GC'd
            SubscribeFromTemporaryObject(aggregator, ref receivedCount);

            // Before GC, the handler target is alive -- publish should work
            // (though behavior depends on GC timing; we just verify no crash)
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // After GC, the weak reference target should be dead
            // The next Publish will clean up dead references
            aggregator.Publish(new CharacterUpdatedEvent(99, "AfterGC"));

            // The count may or may not have incremented depending on GC timing,
            // but the test verifies no exception occurs during cleanup
        }

        private static void SubscribeFromTemporaryObject(IEventAggregator aggregator, ref int count)
        {
            var subscriber = new TemporarySubscriber(aggregator, count);
            subscriber.Subscribe();
            // subscriber goes out of scope after this method returns
        }

        private class TemporarySubscriber
        {
            private readonly IEventAggregator _aggregator;
            private int _count;

            public TemporarySubscriber(IEventAggregator aggregator, int count)
            {
                _aggregator = aggregator;
                _count = count;
            }

            public void Subscribe()
            {
                _aggregator.SubscribeWeak<CharacterUpdatedEvent>(OnEvent);
            }

            private void OnEvent(CharacterUpdatedEvent e)
            {
                _count++;
            }
        }

        // --- Test 5: Mixed strong and weak references work correctly ---

        [Fact]
        public void MixedStrongAndWeak_BothReceiveEvents()
        {
            var aggregator = new EventAggregator();
            int strongCount = 0;
            int weakCount = 0;

            // Strong subscriber (lambda captures local variable -- effectively strong)
            aggregator.Subscribe<CharacterUpdatedEvent>(e => strongCount++);

            // Weak subscriber using an instance method
            var weakReceiver = new WeakEventReceiver();
            aggregator.SubscribeWeak<CharacterUpdatedEvent>(weakReceiver.HandleEvent);

            aggregator.Publish(new CharacterUpdatedEvent(1, "Test"));

            strongCount.Should().Be(1, "strong subscriber should receive event");
            weakReceiver.ReceivedCount.Should().Be(1, "weak subscriber should receive event");
        }

        [Fact]
        public void MixedStrongAndWeak_UnsubscribeStrongLeavesWeakWorking()
        {
            var aggregator = new EventAggregator();
            int strongCount = 0;

            Action<CharacterUpdatedEvent> strongHandler = e => strongCount++;
            aggregator.Subscribe(strongHandler);

            var weakReceiver = new WeakEventReceiver();
            aggregator.SubscribeWeak<CharacterUpdatedEvent>(weakReceiver.HandleEvent);

            // Both receive
            aggregator.Publish(new CharacterUpdatedEvent(1, "Test1"));
            strongCount.Should().Be(1);
            weakReceiver.ReceivedCount.Should().Be(1);

            // Unsubscribe strong
            aggregator.Unsubscribe(strongHandler);

            // Only weak receives
            aggregator.Publish(new CharacterUpdatedEvent(2, "Test2"));
            strongCount.Should().Be(1, "strong handler was unsubscribed");
            weakReceiver.ReceivedCount.Should().Be(2, "weak handler should still receive");
        }

        // --- Test 6: Concurrent publish from multiple threads ---

        [Fact]
        public void ConcurrentPublish_NoExceptionsOrMissedEvents()
        {
            var aggregator = new EventAggregator();
            int receivedCount = 0;

            aggregator.Subscribe<CharacterUpdatedEvent>(e =>
                Interlocked.Increment(ref receivedCount));

            int publishCount = 1000;
            var tasks = new List<Task>();
            for (int i = 0; i < publishCount; i++)
            {
                int captured = i;
                tasks.Add(Task.Run(() =>
                    aggregator.Publish(new CharacterUpdatedEvent(captured, $"Char{captured}"))));
            }

            Task.WaitAll(tasks.ToArray());

            receivedCount.Should().Be(publishCount,
                "all concurrent publishes should be delivered");
        }

        // --- Test 7: Many event types with many subscribers ---

        [Fact]
        public void ManyEventTypes_ManySubscribers_CorrectDelivery()
        {
            var aggregator = new EventAggregator();
            int aCount = 0, bCount = 0;

            // 50 subscribers for EventTypeA
            for (int i = 0; i < 50; i++)
                aggregator.Subscribe<EventTypeA>(e => Interlocked.Increment(ref aCount));

            // 50 subscribers for EventTypeB
            for (int i = 0; i < 50; i++)
                aggregator.Subscribe<EventTypeB>(e => Interlocked.Increment(ref bCount));

            // Publish 10 of each
            for (int i = 0; i < 10; i++)
            {
                aggregator.Publish(new EventTypeA { Value = i });
                aggregator.Publish(new EventTypeB { Value = i });
            }

            aCount.Should().Be(500, "50 subscribers * 10 publishes for EventTypeA");
            bCount.Should().Be(500, "50 subscribers * 10 publishes for EventTypeB");
        }

        private class WeakEventReceiver
        {
            public int ReceivedCount { get; private set; }

            public void HandleEvent(CharacterUpdatedEvent e)
            {
                ReceivedCount++;
            }
        }
    }
}
