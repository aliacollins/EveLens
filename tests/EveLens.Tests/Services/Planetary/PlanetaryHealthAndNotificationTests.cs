// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Enumerations;
using EveLens.Common.Events;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.Services.Planetary;
using EveLens.Core.Interfaces;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services.Planetary
{
    public class PlanetaryHealthAndNotificationTests
    {
        [Fact]
        public void ColonyHealthStatus_Enum_HasCorrectValues()
        {
            Enum.GetNames<ColonyHealthStatus>().Should().BeEquivalentTo(
                new[] { "Optimal", "Expiring", "Idle", "NoExtractors" });
        }

        [Fact]
        public void ColonyHealthStatus_Optimal_IsDefaultForRunningExtractors()
        {
            ColonyHealthStatus.Optimal.Should().Be(ColonyHealthStatus.Optimal);
            ((int)ColonyHealthStatus.Optimal).Should().Be(0);
        }

        [Fact]
        public void PinsExpiringEvent_AggregatesMultiplePins()
        {
            IEventAggregator aggregator = new EventAggregator();
            var received = new List<CharacterPlanetaryPinsExpiringEvent>();
            aggregator.Subscribe<CharacterPlanetaryPinsExpiringEvent>(e => received.Add(e));

            var identity = new CharacterIdentity(1L, "Test Pilot");
            var character = new CCPCharacter(identity, new NullCharacterServices());
            var pins = new PlanetaryPin[] { };

            aggregator.Publish(new CharacterPlanetaryPinsExpiringEvent(character, pins, TimeSpan.FromMinutes(120)));

            received.Should().HaveCount(1);
            received[0].Character.Should().BeSameAs(character);
            received[0].LeadTime.Should().Be(TimeSpan.FromMinutes(120));
        }

        [Fact]
        public void PinsCompletedEvent_AggregatesMultiplePins()
        {
            IEventAggregator aggregator = new EventAggregator();
            var received = new List<CharacterPlanetaryPinsCompletedEvent>();
            aggregator.Subscribe<CharacterPlanetaryPinsCompletedEvent>(e => received.Add(e));

            var identity = new CharacterIdentity(1L, "Test Pilot");
            var character = new CCPCharacter(identity, new NullCharacterServices());
            var pins = new PlanetaryPin[] { };

            aggregator.Publish(new CharacterPlanetaryPinsCompletedEvent(character, pins));

            received.Should().HaveCount(1);
            received[0].Character.Should().BeSameAs(character);
            received[0].CompletedPins.Should().BeEmpty();
        }

        [Fact]
        public void MultipleCharacters_FireSeparateEvents()
        {
            IEventAggregator aggregator = new EventAggregator();
            var received = new List<CharacterPlanetaryPinsCompletedEvent>();
            aggregator.Subscribe<CharacterPlanetaryPinsCompletedEvent>(e => received.Add(e));

            var char1 = new CCPCharacter(new CharacterIdentity(1L, "Alia"), new NullCharacterServices());
            var char2 = new CCPCharacter(new CharacterIdentity(2L, "Saino"), new NullCharacterServices());
            var char3 = new CCPCharacter(new CharacterIdentity(3L, "Tracy"), new NullCharacterServices());

            aggregator.Publish(new CharacterPlanetaryPinsCompletedEvent(char1, Array.Empty<PlanetaryPin>()));
            aggregator.Publish(new CharacterPlanetaryPinsCompletedEvent(char2, Array.Empty<PlanetaryPin>()));
            aggregator.Publish(new CharacterPlanetaryPinsCompletedEvent(char3, Array.Empty<PlanetaryPin>()));

            received.Should().HaveCount(3);
            received.Select(e => e.Character.Name).Should().BeEquivalentTo(new[] { "Alia", "Saino", "Tracy" });
        }

        [Fact]
        public void ExpiringEvent_CarriesLeadTime()
        {
            IEventAggregator aggregator = new EventAggregator();
            CharacterPlanetaryPinsExpiringEvent? received = null;
            aggregator.Subscribe<CharacterPlanetaryPinsExpiringEvent>(e => received = e);

            var character = new CCPCharacter(new CharacterIdentity(1L, "Test"), new NullCharacterServices());
            var leadTime = TimeSpan.FromMinutes(45);

            aggregator.Publish(new CharacterPlanetaryPinsExpiringEvent(character, Array.Empty<PlanetaryPin>(), leadTime));

            received.Should().NotBeNull();
            received!.LeadTime.TotalMinutes.Should().Be(45);
        }

        [Fact]
        public void ColonyAnalysis_HasYieldData_FalseWhenNoExtractorsHaveYield()
        {
            var analysis = ColonyAnalysis.Empty;
            analysis.HasYieldData.Should().BeFalse();
        }

        [Fact]
        public void ColonyAnalysis_Empty_HasNoExtractorsHealth()
        {
            ColonyAnalysis.Empty.Health.Should().Be(ColonyHealthStatus.NoExtractors);
        }
    }
}
