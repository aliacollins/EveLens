using System;
using EVEMon.Common.Services;
using EVEMon.Core.Events;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Services
{
    public class EventBridgeTests
    {
        [Fact]
        public void CharacterUpdatedEvent_HasCorrectData()
        {
            var evt = new CharacterUpdatedEvent(12345, "Test Char");
            evt.CharacterID.Should().Be(12345);
            evt.CharacterName.Should().Be("Test Char");
        }

        [Fact]
        public void CharacterUpdatedEvent_NullName_DefaultsToEmpty()
        {
            var evt = new CharacterUpdatedEvent(99, null);
            evt.CharacterName.Should().Be(string.Empty);
        }

        [Fact]
        public void SettingsChangedEvent_Instance_IsSingleton()
        {
            var a = SettingsChangedEvent.Instance;
            var b = SettingsChangedEvent.Instance;
            a.Should().BeSameAs(b);
        }

        [Fact]
        public void AllCharacterEvents_InheritFromCharacterEventBase()
        {
            var events = new CharacterEventBase[]
            {
                new CharacterUpdatedEvent(1, "A"),
                new CharacterInfoUpdatedEvent(2, "B"),
                new CharacterSkillQueueUpdatedEvent(3, "C"),
                new CharacterAssetsUpdatedEvent(4, "D"),
                new CharacterMarketOrdersUpdatedEvent(5, "E"),
                new CharacterContractsUpdatedEvent(6, "F"),
                new CharacterIndustryJobsUpdatedEvent(7, "G"),
                new CharacterMailUpdatedEvent(8, "H"),
                new CharacterNotificationsUpdatedEvent(9, "I"),
                new CharacterPlanetaryUpdatedEvent(10, "J"),
                new CharacterStandingsUpdatedEvent(11, "K"),
                new CharacterResearchUpdatedEvent(12, "L"),
                new CharacterKillLogUpdatedEvent(13, "M"),
                new CharacterContactsUpdatedEvent(14, "N"),
                new CharacterMedalsUpdatedEvent(15, "O"),
                new CharacterCalendarUpdatedEvent(16, "P"),
                new CharacterLoyaltyUpdatedEvent(17, "Q"),
            };

            for (int i = 0; i < events.Length; i++)
            {
                events[i].CharacterID.Should().Be(i + 1);
                events[i].Should().BeAssignableTo<CharacterEventBase>();
            }
        }

        [Fact]
        public void ESIKeyCollectionChangedEvent_CreatesSuccessfully()
        {
            var evt = ESIKeyCollectionChangedEvent.Instance;
            evt.Should().NotBeNull();
            evt.Should().BeSameAs(ESIKeyCollectionChangedEvent.Instance);
        }

        [Fact]
        public void ServerStatusUpdatedEvent_CreatesSuccessfully()
        {
            var evt = ServerStatusUpdatedEvent.Instance;
            evt.Should().NotBeNull();
            evt.Should().BeSameAs(ServerStatusUpdatedEvent.Instance);
        }

        [Fact]
        public void CharacterCollectionChangedEvent_CreatesSuccessfully()
        {
            var evt = CharacterCollectionChangedEvent.Instance;
            evt.Should().NotBeNull();
            evt.Should().BeSameAs(CharacterCollectionChangedEvent.Instance);
        }

        [Fact]
        public void EventAggregator_PublishesAndReceives_CharacterUpdatedEvent()
        {
            IEventAggregator aggregator = new EventAggregator();
            CharacterUpdatedEvent received = null;
            aggregator.Subscribe<CharacterUpdatedEvent>(e => received = e);

            aggregator.Publish(new CharacterUpdatedEvent(42, "Pilot"));

            received.Should().NotBeNull();
            received.CharacterID.Should().Be(42);
            received.CharacterName.Should().Be("Pilot");
        }

        [Fact]
        public void EventAggregator_PublishesAndReceives_SettingsChangedEvent()
        {
            IEventAggregator aggregator = new EventAggregator();
            SettingsChangedEvent received = null;
            aggregator.Subscribe<SettingsChangedEvent>(e => received = e);

            aggregator.Publish(SettingsChangedEvent.Instance);

            received.Should().NotBeNull();
            received.Should().BeSameAs(SettingsChangedEvent.Instance);
        }

        [Fact]
        public void DifferentEventTypes_DoNotInterfere()
        {
            IEventAggregator aggregator = new EventAggregator();
            CharacterUpdatedEvent charReceived = null;
            SettingsChangedEvent settingsReceived = null;
            ServerStatusUpdatedEvent serverReceived = null;

            aggregator.Subscribe<CharacterUpdatedEvent>(e => charReceived = e);
            aggregator.Subscribe<SettingsChangedEvent>(e => settingsReceived = e);
            aggregator.Subscribe<ServerStatusUpdatedEvent>(e => serverReceived = e);

            aggregator.Publish(new CharacterUpdatedEvent(1, "Char"));

            charReceived.Should().NotBeNull();
            settingsReceived.Should().BeNull();
            serverReceived.Should().BeNull();
        }

        [Fact]
        public void MultipleCharacterEventTypes_RoutedCorrectly()
        {
            IEventAggregator aggregator = new EventAggregator();
            CharacterUpdatedEvent updatedReceived = null;
            CharacterSkillQueueUpdatedEvent skillReceived = null;

            aggregator.Subscribe<CharacterUpdatedEvent>(e => updatedReceived = e);
            aggregator.Subscribe<CharacterSkillQueueUpdatedEvent>(e => skillReceived = e);

            aggregator.Publish(new CharacterSkillQueueUpdatedEvent(7, "SkillPilot"));

            updatedReceived.Should().BeNull();
            skillReceived.Should().NotBeNull();
            skillReceived.CharacterID.Should().Be(7);
            skillReceived.CharacterName.Should().Be("SkillPilot");
        }
    }
}
