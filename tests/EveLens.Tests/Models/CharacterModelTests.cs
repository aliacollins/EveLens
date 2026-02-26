// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;
using EveLens.Common.Serialization.Settings;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Models
{
    /// <summary>
    /// Tests for character serialization DTOs.
    /// Full CCPCharacter construction requires EveLensClient + static game data.
    /// These tests cover the serializable layer that must survive round-trips.
    /// </summary>
    public class CharacterModelTests
    {
        [Fact]
        public void SerializableCCPCharacter_DefaultConstructor_InitializesCollections()
        {
            var character = new SerializableCCPCharacter();
            character.SkillQueue.Should().NotBeNull().And.BeEmpty();
            character.MarketOrders.Should().NotBeNull().And.BeEmpty();
            character.Contracts.Should().NotBeNull().And.BeEmpty();
            character.IndustryJobs.Should().NotBeNull().And.BeEmpty();
            character.LastUpdates.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void SerializableCCPCharacter_XmlRoundTrip_PreservesGuid()
        {
            var guid = Guid.NewGuid();
            var character = new SerializableCCPCharacter { Guid = guid };
            var result = XmlRoundTrip<SerializableCCPCharacter>(character);
            result.Guid.Should().Be(guid);
        }

        [Fact]
        public void SerializableCCPCharacter_XmlRoundTrip_PreservesLabel()
        {
            var character = new SerializableCCPCharacter { Label = "Main PvP" };
            var result = XmlRoundTrip<SerializableCCPCharacter>(character);
            result.Label.Should().Be("Main PvP");
        }

        [Fact]
        public void SerializableCCPCharacter_XmlRoundTrip_PreservesMailIDs()
        {
            var character = new SerializableCCPCharacter
            {
                EveMailMessagesIDs = "100,200,300",
                EveNotificationsIDs = "400,500"
            };
            var result = XmlRoundTrip<SerializableCCPCharacter>(character);
            result.EveMailMessagesIDs.Should().Be("100,200,300");
            result.EveNotificationsIDs.Should().Be("400,500");
        }

        [Fact]
        public void SerializableCCPCharacter_EmptyCollections_RoundTrip()
        {
            var character = new SerializableCCPCharacter();
            var result = XmlRoundTrip<SerializableCCPCharacter>(character);
            result.SkillQueue.Should().BeEmpty();
            result.MarketOrders.Should().BeEmpty();
            result.Contracts.Should().BeEmpty();
        }

        [Fact]
        public void SerializableSettingsCharacter_PreservesGuidAndLabel()
        {
            var guid = Guid.NewGuid();
            // SerializableSettingsCharacter is abstract-ish - test through CCPCharacter
            var character = new SerializableCCPCharacter
            {
                Guid = guid,
                Label = "Industry Alt"
            };
            var result = XmlRoundTrip<SerializableCCPCharacter>(character);
            result.Guid.Should().Be(guid);
            result.Label.Should().Be("Industry Alt");
        }

        [Fact]
        public void SerializableCCPCharacter_NullStrings_HandleGracefully()
        {
            var character = new SerializableCCPCharacter
            {
                EveMailMessagesIDs = null,
                EveNotificationsIDs = null,
                Label = null
            };
            var result = XmlRoundTrip<SerializableCCPCharacter>(character);
            result.Should().NotBeNull();
            // Null strings may round-trip as null or empty depending on XML serializer
        }

        [Fact]
        public void SerializableESIKey_InSettingsContext_PreservesAll()
        {
            // Test ESI key as part of full settings structure
            var settings = new SerializableSettings();
            settings.ESIKeys.Add(new SerializableESIKey
            {
                ID = 2119000001,
                RefreshToken = "rt_abc123def456",
                Monitored = true,
                AuthorizedScopes = new List<string> { "esi-skills.read_skills.v1" }
            });
            settings.ESIKeys.Add(new SerializableESIKey
            {
                ID = 2119000002,
                RefreshToken = "rt_xyz789",
                Monitored = false
            });

            var result = XmlRoundTrip(settings);
            result.ESIKeys.Should().HaveCount(2);
            result.ESIKeys[0].ID.Should().Be(2119000001);
            result.ESIKeys[0].AuthorizedScopes.Should().Contain("esi-skills.read_skills.v1");
            result.ESIKeys[1].Monitored.Should().BeFalse();
        }

        private static T XmlRoundTrip<T>(T obj) where T : class
        {
            var serializer = new XmlSerializer(typeof(T));
            using var writer = new StringWriter();
            serializer.Serialize(writer, obj);
            using var reader = new StringReader(writer.ToString());
            return (T)serializer.Deserialize(reader)!;
        }
    }
}
