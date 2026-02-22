// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using System.IO;
using EVEMon.Common.Helpers;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.SettingsObjects;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Settings
{
    public class SettingsSerializationTests
    {
        // Mirror the JSON options from SettingsFileManager
        private static readonly JsonSerializerOptions JsonWriteOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        private static readonly JsonSerializerOptions JsonReadOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        #region SerializableSettings XML Round-Trip

        [Fact]
        public void SerializableSettings_XmlRoundTrip_PreservesRevision()
        {
            var settings = new SerializableSettings { Revision = 42 };
            var result = XmlRoundTrip(settings);
            result.Revision.Should().Be(42);
        }

        [Fact]
        public void SerializableSettings_XmlRoundTrip_PreservesForkId()
        {
            var settings = new SerializableSettings
            {
                ForkId = "aliacollins",
                ForkVersion = "5.2.0"
            };
            var result = XmlRoundTrip(settings);
            result.ForkId.Should().Be("aliacollins");
            result.ForkVersion.Should().Be("5.2.0");
        }

        [Fact]
        public void SerializableSettings_XmlRoundTrip_PreservesESIKeys()
        {
            var settings = new SerializableSettings();
            settings.ESIKeys.Add(new SerializableESIKey
            {
                ID = 12345678,
                RefreshToken = "test-token-abc",
                Monitored = true,
                AuthorizedScopes = new List<string> { "esi-skills.read_skills.v1" }
            });

            var result = XmlRoundTrip(settings);
            result.ESIKeys.Should().HaveCount(1);
            result.ESIKeys[0].ID.Should().Be(12345678);
            result.ESIKeys[0].RefreshToken.Should().Be("test-token-abc");
            result.ESIKeys[0].AuthorizedScopes.Should().Contain("esi-skills.read_skills.v1");
            result.ESIKeys[0].Monitored.Should().BeTrue();
        }

        [Fact]
        public void SerializableSettings_EmptySettings_RoundTripsWithoutException()
        {
            var settings = new SerializableSettings();
            var result = XmlRoundTrip(settings);
            result.Should().NotBeNull();
            result.ESIKeys.Should().BeEmpty();
            result.Characters.Should().BeEmpty();
            result.Plans.Should().BeEmpty();
        }

        [Fact]
        public void SerializableSettings_XmlRoundTrip_PreservesSSOCredentials()
        {
            var settings = new SerializableSettings
            {
                SSOClientID = "test-client-id",
                SSOClientSecret = "test-secret"
            };
            var result = XmlRoundTrip(settings);
            result.SSOClientID.Should().Be("test-client-id");
            result.SSOClientSecret.Should().Be("test-secret");
        }

        #endregion

        #region SerializableESIKey Tests

        [Fact]
        public void SerializableESIKey_DefaultValues_AreCorrect()
        {
            var key = new SerializableESIKey();
            key.ID.Should().Be(0);
            key.RefreshToken.Should().BeNull();
            key.AccessMask.Should().Be(0UL);
            key.Monitored.Should().BeFalse();
        }

        [Fact]
        public void SerializableESIKey_XmlRoundTrip_PreservesAllProperties()
        {
            var key = new SerializableESIKey
            {
                ID = 98765432,
                RefreshToken = "refresh-token-xyz",
                Monitored = true,
                AuthorizedScopes = new List<string> { "esi-skills.read_skills.v1", "esi-wallet.read_character_wallet.v1" }
            };

            var result = XmlRoundTrip(key);
            result.ID.Should().Be(98765432);
            result.RefreshToken.Should().Be("refresh-token-xyz");
            result.AuthorizedScopes.Should().HaveCount(2);
            result.Monitored.Should().BeTrue();
        }

        [Fact]
        public void SerializableESIKey_NullRefreshToken_HandlesGracefully()
        {
            var key = new SerializableESIKey { ID = 1, RefreshToken = null };
            var result = XmlRoundTrip(key);
            result.RefreshToken.Should().BeNull();
        }

        [Fact]
        public void SerializableESIKey_EmptyRefreshToken_RoundTrips()
        {
            var key = new SerializableESIKey { ID = 1, RefreshToken = string.Empty };
            var result = XmlRoundTrip(key);
            result.RefreshToken.Should().BeEmpty();
        }

        #endregion

        #region SerializablePlan Tests

        [Fact]
        public void SerializablePlan_XmlRoundTrip_PreservesNameAndDescription()
        {
            var plan = new SerializablePlan
            {
                Name = "Training Plan Alpha",
                Description = "Get into a Raven"
            };

            var result = XmlRoundTrip(plan);
            result.Name.Should().Be("Training Plan Alpha");
            result.Description.Should().Be("Get into a Raven");
        }

        [Fact]
        public void SerializablePlan_WithEntries_PreservesOrder()
        {
            var plan = new SerializablePlan { Name = "Test Plan" };
            plan.Entries.Add(new SerializablePlanEntry { ID = 100, SkillName = "Skill A", Level = 3 });
            plan.Entries.Add(new SerializablePlanEntry { ID = 200, SkillName = "Skill B", Level = 5 });
            plan.Entries.Add(new SerializablePlanEntry { ID = 300, SkillName = "Skill C", Level = 1 });

            var result = XmlRoundTrip(plan);
            result.Entries.Should().HaveCount(3);
            result.Entries[0].ID.Should().Be(100);
            result.Entries[1].ID.Should().Be(200);
            result.Entries[2].ID.Should().Be(300);
        }

        [Fact]
        public void SerializablePlan_EmptyPlan_RoundTrips()
        {
            var plan = new SerializablePlan { Name = "Empty" };
            var result = XmlRoundTrip(plan);
            result.Name.Should().Be("Empty");
            result.Entries.Should().BeEmpty();
        }

        #endregion

        #region SerializablePlanEntry Tests

        [Fact]
        public void SerializablePlanEntry_DefaultPriority_IsThree()
        {
            var entry = new SerializablePlanEntry();
            entry.Priority.Should().Be(3);
        }

        [Fact]
        public void SerializablePlanEntry_XmlRoundTrip_PreservesAllProperties()
        {
            var entry = new SerializablePlanEntry
            {
                ID = 3350,
                SkillName = "Caldari Battleship",
                Level = 4,
                Priority = 1,
                Notes = "Needed for Raven Navy Issue"
            };

            var result = XmlRoundTrip(entry);
            result.ID.Should().Be(3350);
            result.SkillName.Should().Be("Caldari Battleship");
            result.Level.Should().Be(4);
            result.Priority.Should().Be(1);
            result.Notes.Should().Be("Needed for Raven Navy Issue");
        }

        #endregion

        #region JSON DTO Tests (credentials.json, config.json)

        [Fact]
        public void JsonCredentials_RoundTrip_PreservesEsiKeys()
        {
            var creds = new JsonCredentials
            {
                Version = 1,
                EsiKeys = new List<JsonEsiKey>
                {
                    new() { CharacterId = 12345, RefreshToken = "token-a", AccessMask = 512, Monitored = true },
                    new() { CharacterId = 67890, RefreshToken = "token-b", AccessMask = 1024, Monitored = false }
                }
            };

            string json = JsonSerializer.Serialize(creds, JsonWriteOptions);
            var result = JsonSerializer.Deserialize<JsonCredentials>(json, JsonReadOptions);

            result.Should().NotBeNull();
            result!.Version.Should().Be(1);
            result.EsiKeys.Should().HaveCount(2);
            result.EsiKeys[0].CharacterId.Should().Be(12345);
            result.EsiKeys[0].RefreshToken.Should().Be("token-a");
            result.EsiKeys[1].CharacterId.Should().Be(67890);
            result.EsiKeys[1].Monitored.Should().BeFalse();
        }

        [Fact]
        public void JsonConfig_RoundTrip_PreservesForkInfo()
        {
            var config = new JsonConfig
            {
                Version = 1,
                ForkId = "aliacollins",
                ForkVersion = "5.2.0"
            };

            string json = JsonSerializer.Serialize(config, JsonWriteOptions);
            var result = JsonSerializer.Deserialize<JsonConfig>(json, JsonReadOptions);

            result.Should().NotBeNull();
            result!.ForkId.Should().Be("aliacollins");
            result.ForkVersion.Should().Be("5.2.0");
        }

        [Fact]
        public void JsonEsiKey_DefaultValues_SerializeCorrectly()
        {
            var key = new JsonEsiKey();
            string json = JsonSerializer.Serialize(key, JsonWriteOptions);
            var result = JsonSerializer.Deserialize<JsonEsiKey>(json, JsonReadOptions);

            result!.CharacterId.Should().Be(0);
            result.AccessMask.Should().Be(0UL);
            result.Monitored.Should().BeFalse();
        }

        [Fact]
        public void JsonCredentials_EmptyKeyList_RoundTrips()
        {
            var creds = new JsonCredentials { EsiKeys = new List<JsonEsiKey>() };
            string json = JsonSerializer.Serialize(creds, JsonWriteOptions);
            var result = JsonSerializer.Deserialize<JsonCredentials>(json, JsonReadOptions);

            result!.EsiKeys.Should().NotBeNull();
            result.EsiKeys.Should().BeEmpty();
        }

        [Fact]
        public void JsonCredentials_LargeKeyList_RoundTrips()
        {
            var creds = new JsonCredentials();
            for (int i = 0; i < 100; i++)
            {
                creds.EsiKeys.Add(new JsonEsiKey
                {
                    CharacterId = 90000000 + i,
                    RefreshToken = $"token-{i}",
                    AccessMask = (ulong)i,
                    Monitored = i % 2 == 0
                });
            }

            string json = JsonSerializer.Serialize(creds, JsonWriteOptions);
            var result = JsonSerializer.Deserialize<JsonCredentials>(json, JsonReadOptions);

            result!.EsiKeys.Should().HaveCount(100);
            result.EsiKeys[50].CharacterId.Should().Be(90000050);
            result.EsiKeys[50].RefreshToken.Should().Be("token-50");
        }

        [Fact]
        public void JsonCharacterIndex_RoundTrip_PreservesEntries()
        {
            var index = new JsonCharacterIndex
            {
                Characters = new List<JsonCharacterIndexEntry>
                {
                    new() { CharacterId = 12345, Name = "Pilot One", CorporationName = "Corp A" },
                    new() { CharacterId = 67890, Name = "Pilot Two", IsUriCharacter = true }
                },
                MonitoredCharacterIds = new List<long> { 12345 }
            };

            string json = JsonSerializer.Serialize(index, JsonWriteOptions);
            var result = JsonSerializer.Deserialize<JsonCharacterIndex>(json, JsonReadOptions);

            result!.Characters.Should().HaveCount(2);
            result.Characters[0].Name.Should().Be("Pilot One");
            result.Characters[1].IsUriCharacter.Should().BeTrue();
            result.MonitoredCharacterIds.Should().Contain(12345);
        }

        #endregion

        #region Helpers

        private static T XmlRoundTrip<T>(T obj) where T : class
        {
            var serializer = new XmlSerializer(typeof(T));
            using var writer = new StringWriter();
            serializer.Serialize(writer, obj);
            string xml = writer.ToString();

            using var reader = new StringReader(xml);
            return (T)serializer.Deserialize(reader)!;
        }

        #endregion
    }
}
