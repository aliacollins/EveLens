// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Services;
using EVEMon.Core.Events;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EVEMon.Tests.Integration
{
    public class LargeScaleStartupTests
    {
        // --- Test 1: Factory handles 100 characters ---

        [Fact]
        public void Factory_Creates100Characters_TracksAll()
        {
            var repo = Substitute.For<ICharacterRepository>();
            var events = Substitute.For<IEventAggregator>();
            var factory = new CharacterFactory(repo, events);

            for (int i = 0; i < 100; i++)
            {
                var identity = Substitute.For<ICharacterIdentity>();
                identity.CharacterID.Returns((long)(i + 1));
                identity.Name.Returns($"Char {i}");
                identity.Guid.Returns(Guid.NewGuid());
                factory.CreateNew(identity);
            }

            factory.ManagedCount.Should().Be(100);
        }

        // --- Test 2: EventAggregator handles 100 rapid publishes ---

        [Fact]
        public void EventAggregator_100RapidPublishes_AllDelivered()
        {
            var aggregator = new EventAggregator();
            int received = 0;
            aggregator.Subscribe<CharacterUpdatedEvent>(e => received++);

            for (int i = 0; i < 100; i++)
                aggregator.Publish(new CharacterUpdatedEvent(i, $"Char{i}"));

            received.Should().Be(100);
        }

        // --- Test 3: Factory publishes creation events for each character ---

        [Fact]
        public void Factory_Creates70Characters_PublishesAllEvents()
        {
            var repo = Substitute.For<ICharacterRepository>();
            var aggregator = new EventAggregator();
            var factory = new CharacterFactory(repo, aggregator);

            int createdCount = 0;
            aggregator.Subscribe<CharacterCreatedEvent>(e => createdCount++);

            for (int i = 0; i < 70; i++)
            {
                var identity = Substitute.For<ICharacterIdentity>();
                identity.CharacterID.Returns((long)(i + 1));
                identity.Name.Returns($"Char {i}");
                identity.Guid.Returns(Guid.NewGuid());
                factory.CreateNew(identity);
            }

            createdCount.Should().Be(70);
            factory.ManagedCount.Should().Be(70);
        }
    }
}
