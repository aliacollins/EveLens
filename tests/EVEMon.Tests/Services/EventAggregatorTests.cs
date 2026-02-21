// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EVEMon.Common.Services;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Services
{
    public class EventAggregatorTests
    {
        private class TestEvent
        {
            public string Message { get; set; } = "";
        }

        private class OtherEvent
        {
            public int Value { get; set; }
        }

        [Fact]
        public void Subscribe_And_Publish_DeliversEvent()
        {
            // Arrange
            IEventAggregator aggregator = new EventAggregator();
            TestEvent? received = null;
            aggregator.Subscribe<TestEvent>(e => received = e);

            // Act
            aggregator.Publish(new TestEvent { Message = "hello" });

            // Assert
            received.Should().NotBeNull();
            received!.Message.Should().Be("hello");
        }

        [Fact]
        public void Unsubscribe_StopsDelivery()
        {
            // Arrange
            IEventAggregator aggregator = new EventAggregator();
            int callCount = 0;
            Action<TestEvent> handler = e => callCount++;
            aggregator.Subscribe(handler);

            // Act
            aggregator.Publish(new TestEvent());
            aggregator.Unsubscribe(handler);
            aggregator.Publish(new TestEvent());

            // Assert
            callCount.Should().Be(1);
        }

        [Fact]
        public void Publish_DoesNotDeliverToWrongEventType()
        {
            // Arrange
            IEventAggregator aggregator = new EventAggregator();
            bool called = false;
            aggregator.Subscribe<OtherEvent>(e => called = true);

            // Act
            aggregator.Publish(new TestEvent { Message = "test" });

            // Assert
            called.Should().BeFalse();
        }

        [Fact]
        public void Multiple_Subscribers_AllReceive()
        {
            // Arrange
            IEventAggregator aggregator = new EventAggregator();
            int count1 = 0, count2 = 0;
            aggregator.Subscribe<TestEvent>(e => count1++);
            aggregator.Subscribe<TestEvent>(e => count2++);

            // Act
            aggregator.Publish(new TestEvent());

            // Assert
            count1.Should().Be(1);
            count2.Should().Be(1);
        }

        [Fact]
        public void Publish_WithNoSubscribers_DoesNotThrow()
        {
            // Arrange
            IEventAggregator aggregator = new EventAggregator();

            // Act & Assert
            Action act = () => aggregator.Publish(new TestEvent());
            act.Should().NotThrow();
        }

        [Fact]
        public void Subscribe_NullHandler_Throws()
        {
            // Arrange
            IEventAggregator aggregator = new EventAggregator();

            // Act & Assert
            Action act = () => aggregator.Subscribe<TestEvent>(null!);
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
