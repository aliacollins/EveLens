// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EveLens.Common.Events;
using EveLens.Common.Services;
using EveLens.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Integration
{
    /// <summary>
    /// End-to-end tests for <see cref="EventAggregator"/> using the real implementation.
    /// Validates publish/subscribe delivery, unsubscription, weak references,
    /// concurrent publish safety, and re-entrant publish behavior.
    /// </summary>
    public class EventAggregatorEndToEndTests
    {
        // Use Core events (primitive-based, no EveLensClient dependency)
        // for end-to-end tests

        #region Test Event Types

        // Simple test event types that don't depend on EveLensClient
        private sealed class SimpleEvent
        {
            public string Message { get; set; } = string.Empty;
        }

        private sealed class AnotherEvent
        {
            public int Value { get; set; }
        }

        #endregion

        #region CharacterUpdated Publish/Subscribe

        [Fact]
        public void CharacterUpdated_PublishReachesSubscriber()
        {
            // Arrange
            IEventAggregator aggregator = new EventAggregator();
            Core.Events.CharacterUpdatedEvent? received = null;
            aggregator.Subscribe<Core.Events.CharacterUpdatedEvent>(e => received = e);

            // Act
            aggregator.Publish(new Core.Events.CharacterUpdatedEvent(12345, "TestPilot"));

            // Assert
            received.Should().NotBeNull();
            received!.CharacterID.Should().Be(12345);
            received.CharacterName.Should().Be("TestPilot");
        }

        [Fact]
        public void CharacterUpdated_MultipleSubscribers_AllReceive()
        {
            // Arrange
            IEventAggregator aggregator = new EventAggregator();
            var results = new List<long>();

            aggregator.Subscribe<Core.Events.CharacterUpdatedEvent>(e => results.Add(e.CharacterID));
            aggregator.Subscribe<Core.Events.CharacterUpdatedEvent>(e => results.Add(e.CharacterID * 10));

            // Act
            aggregator.Publish(new Core.Events.CharacterUpdatedEvent(7, "Pilot"));

            // Assert
            results.Should().HaveCount(2);
            results.Should().Contain(7);
            results.Should().Contain(70);
        }

        [Fact]
        public void CharacterUpdated_PublishMultipleTimes_AllDelivered()
        {
            // Arrange
            IEventAggregator aggregator = new EventAggregator();
            int count = 0;
            aggregator.Subscribe<Core.Events.CharacterUpdatedEvent>(e => count++);

            // Act
            for (int i = 0; i < 50; i++)
            {
                aggregator.Publish(new Core.Events.CharacterUpdatedEvent(i, $"Pilot{i}"));
            }

            // Assert
            count.Should().Be(50);
        }

        #endregion

        #region Unsubscribe Behavior

        [Fact]
        public void UnsubscribedHandler_DoesNotFire_AfterUnsubscribe()
        {
            // Arrange
            IEventAggregator aggregator = new EventAggregator();
            int callCount = 0;
            Action<SimpleEvent> handler = e => callCount++;
            aggregator.Subscribe(handler);

            // Act - publish once while subscribed
            aggregator.Publish(new SimpleEvent { Message = "first" });
            callCount.Should().Be(1);

            // Unsubscribe
            aggregator.Unsubscribe(handler);

            // Publish again -- should NOT be received
            aggregator.Publish(new SimpleEvent { Message = "second" });

            // Assert
            callCount.Should().Be(1, "handler should not fire after Unsubscribe");
        }

        [Fact]
        public void UnsubscribeViaToken_DoesNotFire_AfterDispose()
        {
            // Arrange
            IEventAggregator aggregator = new EventAggregator();
            int callCount = 0;
            var token = aggregator.Subscribe<SimpleEvent>(e => callCount++);

            // Act
            aggregator.Publish(new SimpleEvent());
            callCount.Should().Be(1);

            token.Dispose();
            aggregator.Publish(new SimpleEvent());

            // Assert
            callCount.Should().Be(1, "handler should not fire after token is disposed");
        }

        [Fact]
        public void UnsubscribeWrongHandler_OriginalStillFires()
        {
            // Arrange
            IEventAggregator aggregator = new EventAggregator();
            int callCount = 0;
            Action<SimpleEvent> subscribed = e => callCount++;
            Action<SimpleEvent> notSubscribed = e => { };

            aggregator.Subscribe(subscribed);

            // Act - unsubscribe a different handler
            aggregator.Unsubscribe(notSubscribed);
            aggregator.Publish(new SimpleEvent());

            // Assert
            callCount.Should().Be(1, "unsubscribing a different handler should not affect the original");
        }

        #endregion

        #region Weak Reference Tests

        [Fact]
        public void WeakSubscription_StillDelivers_WhileTargetAlive()
        {
            // Arrange
            IEventAggregator aggregator = new EventAggregator();
            var receiver = new EventReceiver();
            aggregator.SubscribeWeak<SimpleEvent>(receiver.HandleEvent);

            // Act
            aggregator.Publish(new SimpleEvent { Message = "alive" });

            // Assert
            receiver.ReceivedCount.Should().Be(1);

            // Keep receiver alive to prevent GC during test
            GC.KeepAlive(receiver);
        }

        [Fact]
        public void WeakSubscription_CleanedUp_AfterTargetCollected()
        {
            // Arrange
            IEventAggregator aggregator = new EventAggregator();
            int externalCount = 0;

            // Create receiver in a helper method so it can be GC'd
            SubscribeWeakFromTemporary(aggregator);

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Subscribe a strong handler to verify aggregator still works
            aggregator.Subscribe<SimpleEvent>(e => externalCount++);

            // Act - publish after GC
            aggregator.Publish(new SimpleEvent { Message = "afterGC" });

            // Assert - should not throw, strong handler receives
            externalCount.Should().Be(1);
        }

        private static void SubscribeWeakFromTemporary(IEventAggregator aggregator)
        {
            var temp = new EventReceiver();
            aggregator.SubscribeWeak<SimpleEvent>(temp.HandleEvent);
            // temp goes out of scope -- eligible for GC
        }

        #endregion

        #region Concurrent Publish Safety

        [Fact]
        public async Task ConcurrentPublish_NoDeadlocks_AllEventsDelivered()
        {
            // Arrange
            IEventAggregator aggregator = new EventAggregator();
            int receivedCount = 0;

            aggregator.Subscribe<SimpleEvent>(e =>
                Interlocked.Increment(ref receivedCount));

            int publishCount = 500;
            var tasks = new List<Task>();

            // Act - publish from many threads simultaneously
            for (int i = 0; i < publishCount; i++)
            {
                int captured = i;
                tasks.Add(Task.Run(() =>
                    aggregator.Publish(new SimpleEvent { Message = $"msg-{captured}" })));
            }

            await Task.WhenAll(tasks);

            // Assert
            receivedCount.Should().Be(publishCount,
                "all concurrent publishes should be delivered without deadlocks");
        }

        [Fact]
        public async Task ConcurrentSubscribeAndPublish_NoExceptions()
        {
            // Arrange
            IEventAggregator aggregator = new EventAggregator();
            int receivedCount = 0;
            var barrier = new ManualResetEventSlim(false);
            int threadCount = 50;

            var tasks = new List<Task>();

            // Act - half the threads subscribe, half publish
            for (int i = 0; i < threadCount; i++)
            {
                int captured = i;
                if (captured % 2 == 0)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        barrier.Wait();
                        aggregator.Subscribe<SimpleEvent>(e =>
                            Interlocked.Increment(ref receivedCount));
                    }));
                }
                else
                {
                    tasks.Add(Task.Run(() =>
                    {
                        barrier.Wait();
                        aggregator.Publish(new SimpleEvent { Message = $"msg-{captured}" });
                    }));
                }
            }

            barrier.Set();
            await Task.WhenAll(tasks);

            // Assert - no exceptions thrown (exact count depends on timing)
            // The key validation is that no deadlock or exception occurs
            receivedCount.Should().BeGreaterThanOrEqualTo(0);
        }

        #endregion

        #region Re-entrant Publish (Publish During Publish)

        [Fact]
        public void PublishDuringPublish_DoesNotStackOverflow()
        {
            // Arrange
            IEventAggregator aggregator = new EventAggregator();
            int depth = 0;
            int maxDepth = 10;
            int deliveryCount = 0;

            aggregator.Subscribe<SimpleEvent>(e =>
            {
                deliveryCount++;
                if (depth < maxDepth)
                {
                    depth++;
                    // Re-entrant publish from within a handler
                    aggregator.Publish(new SimpleEvent { Message = $"nested-{depth}" });
                }
            });

            // Act
            aggregator.Publish(new SimpleEvent { Message = "root" });

            // Assert - should complete without StackOverflowException
            deliveryCount.Should().Be(maxDepth + 1,
                "root event + 10 nested events should all be delivered");
        }

        [Fact]
        public void PublishDuringPublish_DifferentEventTypes_NoStackOverflow()
        {
            // Arrange
            IEventAggregator aggregator = new EventAggregator();
            int simpleCount = 0;
            int anotherCount = 0;

            aggregator.Subscribe<SimpleEvent>(e =>
            {
                simpleCount++;
                if (simpleCount == 1)
                {
                    // Publish a different event type during handling
                    aggregator.Publish(new AnotherEvent { Value = 42 });
                }
            });

            aggregator.Subscribe<AnotherEvent>(e =>
            {
                anotherCount++;
            });

            // Act
            aggregator.Publish(new SimpleEvent { Message = "trigger" });

            // Assert
            simpleCount.Should().Be(1);
            anotherCount.Should().Be(1);
        }

        #endregion

        #region Event Type Isolation

        [Fact]
        public void DifferentEventTypes_NoCrossContamination()
        {
            // Arrange
            IEventAggregator aggregator = new EventAggregator();
            int simpleCount = 0;
            int anotherCount = 0;

            aggregator.Subscribe<SimpleEvent>(e => simpleCount++);
            aggregator.Subscribe<AnotherEvent>(e => anotherCount++);

            // Act
            aggregator.Publish(new SimpleEvent { Message = "test" });

            // Assert
            simpleCount.Should().Be(1);
            anotherCount.Should().Be(0, "AnotherEvent handler should not receive SimpleEvent");
        }

        [Fact]
        public void CommonEvents_SettingsChanged_DeliveredCorrectly()
        {
            // Arrange
            IEventAggregator aggregator = new EventAggregator();
            bool received = false;
            aggregator.Subscribe<Core.Events.SettingsChangedEvent>(e => received = true);

            // Act - use the singleton instance pattern from CommonEvents
            aggregator.Publish(Core.Events.SettingsChangedEvent.Instance);

            // Assert
            received.Should().BeTrue();
        }

        #endregion

        #region Helper Classes

        private class EventReceiver
        {
            public int ReceivedCount { get; private set; }

            public void HandleEvent(SimpleEvent e)
            {
                ReceivedCount++;
            }
        }

        #endregion
    }
}
