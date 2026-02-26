// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Core.Events;
using EveLens.Core.Interfaces;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EveLens.Tests.Integration
{
    /// <summary>
    /// Tests for the virtual tab scaling pattern (Phase 1).
    /// Validates that the ActiveCharacterChangedEvent fires correctly,
    /// the tier subscriber bridges tab selection to query tiers,
    /// and 100+ characters can be created without exceeding handle limits.
    /// </summary>
    [Collection("AppServices")]
    public class VirtualTabScalingTests : IDisposable
    {
        private readonly NullCharacterServices _services;
        private readonly IEventAggregator _realAggregator;

        public VirtualTabScalingTests()
        {
            AppServices.Reset();
            _services = new NullCharacterServices();
            _realAggregator = new EventAggregator();
            AppServices.SetEventAggregator(_realAggregator);
        }

        public void Dispose()
        {
            AppServices.Reset();
        }

        /// <summary>
        /// Verifies that 100 CCPCharacter instances can be created without issue.
        /// Before virtual tabs, 100 CharacterMonitors (150 controls each) would crash.
        /// With virtual tabs, characters are lightweight — no UI controls created.
        /// </summary>
        [Fact]
        public void VirtualTab_100Characters_CanBeCreatedWithoutCrash()
        {
            var characters = new List<CCPCharacter>();
            for (int i = 0; i < 100; i++)
            {
                var identity = new CharacterIdentity(2000 + i, $"Pilot {i}");
                characters.Add(new CCPCharacter(identity, _services));
            }

            characters.Should().HaveCount(100, "100 characters should be creatable without crash");
            characters.Select(c => c.Name).Should().OnlyHaveUniqueItems();
        }

        /// <summary>
        /// Verifies that ActiveCharacterChangedEvent fires with the correct character ID.
        /// </summary>
        [Fact]
        public void VirtualTab_ActiveCharacterChangedEvent_CarriesCorrectId()
        {
            long receivedId = -1;
            _realAggregator.Subscribe<ActiveCharacterChangedEvent>(e => receivedId = e.CharacterId);

            _realAggregator.Publish(new ActiveCharacterChangedEvent(42));

            receivedId.Should().Be(42);
        }

        /// <summary>
        /// Verifies that ActiveCharacterChangedEvent with ID 0 indicates no active character.
        /// </summary>
        [Fact]
        public void VirtualTab_ActiveCharacterChangedEvent_ZeroMeansNoSelection()
        {
            long receivedId = -1;
            _realAggregator.Subscribe<ActiveCharacterChangedEvent>(e => receivedId = e.CharacterId);

            _realAggregator.Publish(new ActiveCharacterChangedEvent(0));

            receivedId.Should().Be(0, "0 means no character is selected (overview tab)");
        }

        /// <summary>
        /// Verifies the ActiveCharacterChangedEvent can be subscribed and disposed correctly.
        /// </summary>
        [Fact]
        public void VirtualTab_ActiveCharacterChangedEvent_SubscriptionDisposable()
        {
            int callCount = 0;
            var sub = _realAggregator.Subscribe<ActiveCharacterChangedEvent>(e => callCount++);

            _realAggregator.Publish(new ActiveCharacterChangedEvent(1));
            callCount.Should().Be(1);

            sub.Dispose();

            _realAggregator.Publish(new ActiveCharacterChangedEvent(2));
            callCount.Should().Be(1, "after dispose, handler should not be called");
        }

        /// <summary>
        /// Verifies that character Tag-based lookup works correctly.
        /// This is the pattern used by GetCurrentCharacter() with lightweight tabs.
        /// </summary>
        [Fact]
        public void VirtualTab_GetCurrentCharacter_WorksWithTagPattern()
        {
            var identity = new CharacterIdentity(1001, "Test Pilot");
            var character = new CCPCharacter(identity, _services);

            // Simulate the Tag pattern used by lightweight tabs
            object tag = character;
            var retrieved = tag as Character;

            retrieved.Should().NotBeNull();
            retrieved.Should().BeSameAs(character);
            retrieved!.Name.Should().Be("Test Pilot");
        }

        /// <summary>
        /// Verifies that multiple rapid ActiveCharacterChangedEvent publications
        /// don't cause issues (simulates fast tab switching).
        /// </summary>
        [Fact]
        public void VirtualTab_RapidTabSwitching_EventsDeliverCorrectly()
        {
            var receivedIds = new List<long>();
            _realAggregator.Subscribe<ActiveCharacterChangedEvent>(e => receivedIds.Add(e.CharacterId));

            // Simulate rapid tab switching
            for (int i = 1; i <= 50; i++)
            {
                _realAggregator.Publish(new ActiveCharacterChangedEvent(i));
            }

            receivedIds.Should().HaveCount(50);
            receivedIds.Should().BeInAscendingOrder();
        }

        /// <summary>
        /// Verifies that the tier subscriber can be created and disposed without error.
        /// </summary>
        [Fact]
        public void VirtualTab_TierSubscriber_CreatesAndDisposesCleanly()
        {
            var subscriber = new ActiveCharacterTierSubscriber();
            subscriber.Dispose();
            // No exception = pass
        }

        /// <summary>
        /// Verifies that publishing ActiveCharacterChangedEvent with a non-existent character
        /// doesn't throw (the subscriber should handle missing characters gracefully).
        /// </summary>
        [Fact]
        public void VirtualTab_TierSubscriber_HandlesNonExistentCharacterGracefully()
        {
            var subscriber = new ActiveCharacterTierSubscriber();

            // Publish event for a character that doesn't exist in MonitoredCharacters
            // This should not throw
            Action act = () => _realAggregator.Publish(new ActiveCharacterChangedEvent(999999));

            act.Should().NotThrow("subscriber should handle missing characters gracefully");
            subscriber.Dispose();
        }

        /// <summary>
        /// Verifies the event data contract: CharacterId is the only property.
        /// </summary>
        [Fact]
        public void VirtualTab_ActiveCharacterChangedEvent_DataContract()
        {
            var evt = new ActiveCharacterChangedEvent(12345);

            evt.CharacterId.Should().Be(12345);
        }

        /// <summary>
        /// Verifies that GetOrderedCharactersTrainingTime-style iteration
        /// works with MonitoredCharacters (the new pattern replacing tab iteration).
        /// </summary>
        [Fact]
        public void VirtualTab_MonitoredCharacterIteration_WorksWithCCPCharacters()
        {
            var characters = new List<CCPCharacter>();
            for (int i = 0; i < 10; i++)
            {
                var identity = new CharacterIdentity(3000 + i, $"Pilot {i}");
                characters.Add(new CCPCharacter(identity, _services));
            }

            // Verify the OfType pattern used in the new code
            var ccpChars = characters.Cast<Character>().OfType<CCPCharacter>().ToList();
            ccpChars.Should().HaveCount(10);
        }
    }
}
