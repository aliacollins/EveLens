// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using EVEMon.Common.Helpers;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.SettingsObjects;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Serialization
{
    /// <summary>
    /// Tier 5 serialization tests: extended settings serialization tests covering
    /// JSON round-trips, partial/missing fields, and edge cases.
    /// Complements the existing Settings/SettingsSerializationTests.cs (which covers
    /// XML round-trips and basic JSON DTO tests).
    /// </summary>
    public class SettingsSerializationExtendedTests
    {
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

        #region Full Settings JSON Round-Trip

        [Fact]
        public void FullSettings_JsonRoundTrip_Lossless()
        {
            // Build a fully populated JsonConfig
            var config = new JsonConfig
            {
                Version = 1,
                ForkId = "aliacollins",
                ForkVersion = "5.1.3",
                LastSaved = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc)
            };

            string json = JsonSerializer.Serialize(config, JsonWriteOptions);
            var result = JsonSerializer.Deserialize<JsonConfig>(json, JsonReadOptions);

            result.Should().NotBeNull();
            result!.Version.Should().Be(1);
            result.ForkId.Should().Be("aliacollins");
            result.ForkVersion.Should().Be("5.1.3");
        }

        [Fact]
        public void FullCredentials_JsonRoundTrip_Lossless()
        {
            var creds = new JsonCredentials
            {
                Version = 1,
                LastSaved = DateTime.UtcNow,
                EsiKeys = new List<JsonEsiKey>
                {
                    new()
                    {
                        CharacterId = 2119000001,
                        RefreshToken = "rt_alphatoken123",
                        AccessMask = 8388607,
                        Monitored = true
                    },
                    new()
                    {
                        CharacterId = 2119000002,
                        RefreshToken = "rt_betatoken456",
                        AccessMask = 1024,
                        Monitored = false
                    },
                    new()
                    {
                        CharacterId = 2119000003,
                        RefreshToken = "rt_gammatoken789",
                        AccessMask = ulong.MaxValue,
                        Monitored = true
                    }
                }
            };

            string json = JsonSerializer.Serialize(creds, JsonWriteOptions);
            var result = JsonSerializer.Deserialize<JsonCredentials>(json, JsonReadOptions);

            result.Should().NotBeNull();
            result!.EsiKeys.Should().HaveCount(3);
            result.EsiKeys[0].CharacterId.Should().Be(2119000001);
            result.EsiKeys[0].RefreshToken.Should().Be("rt_alphatoken123");
            result.EsiKeys[0].AccessMask.Should().Be(8388607UL);
            result.EsiKeys[0].Monitored.Should().BeTrue();

            result.EsiKeys[1].Monitored.Should().BeFalse();

            result.EsiKeys[2].AccessMask.Should().Be(ulong.MaxValue);
        }

        [Fact]
        public void FullCharacterIndex_JsonRoundTrip_Lossless()
        {
            var index = new JsonCharacterIndex
            {
                Version = 1,
                LastSaved = DateTime.UtcNow,
                Characters = new List<JsonCharacterIndexEntry>
                {
                    new() { CharacterId = 1001, Name = "Pilot Alpha", CorporationName = "Test Corp" },
                    new() { CharacterId = 1002, Name = "Pilot Beta", IsUriCharacter = true },
                    new() { CharacterId = 1003, Name = "Pilot Gamma" }
                },
                MonitoredCharacterIds = new List<long> { 1001, 1003 }
            };

            string json = JsonSerializer.Serialize(index, JsonWriteOptions);
            var result = JsonSerializer.Deserialize<JsonCharacterIndex>(json, JsonReadOptions);

            result.Should().NotBeNull();
            result!.Characters.Should().HaveCount(3);
            result.Characters[0].Name.Should().Be("Pilot Alpha");
            result.Characters[0].CorporationName.Should().Be("Test Corp");
            result.Characters[1].IsUriCharacter.Should().BeTrue();
            result.MonitoredCharacterIds.Should().HaveCount(2);
            result.MonitoredCharacterIds.Should().Contain(1001);
            result.MonitoredCharacterIds.Should().Contain(1003);
        }

        #endregion

        #region Partial Settings - Missing Fields Use Defaults

        [Fact]
        public void PartialSettings_MissingFields_UseDefaults()
        {
            // Deserialize JSON with only some fields set
            string partialJson = @"{ ""version"": 2 }";
            var result = JsonSerializer.Deserialize<JsonConfig>(partialJson, JsonReadOptions);

            result.Should().NotBeNull();
            result!.Version.Should().Be(2);
            // Missing fields should get their default values
            result.ForkId.Should().Be("aliacollins", "ForkId default is 'aliacollins'");
        }

        [Fact]
        public void PartialCredentials_MissingEsiKeys_DefaultsToEmptyList()
        {
            string partialJson = @"{ ""version"": 1 }";
            var result = JsonSerializer.Deserialize<JsonCredentials>(partialJson, JsonReadOptions);

            result.Should().NotBeNull();
            result!.Version.Should().Be(1);
            result.EsiKeys.Should().NotBeNull("EsiKeys default should be initialized list");
        }

        [Fact]
        public void PartialEsiKey_MissingMonitored_DefaultsToFalse()
        {
            string json = @"{ ""characterId"": 12345, ""refreshToken"": ""token"", ""accessMask"": 1024 }";
            var result = JsonSerializer.Deserialize<JsonEsiKey>(json, JsonReadOptions);

            result.Should().NotBeNull();
            result!.CharacterId.Should().Be(12345);
            result.Monitored.Should().BeFalse("default for bool should be false");
        }

        [Fact]
        public void PartialCharacterIndex_MissingCharacters_DefaultsToEmptyList()
        {
            string json = @"{ ""version"": 1, ""monitoredCharacterIds"": [100] }";
            var result = JsonSerializer.Deserialize<JsonCharacterIndex>(json, JsonReadOptions);

            result.Should().NotBeNull();
            result!.MonitoredCharacterIds.Should().Contain(100);
            result.Characters.Should().NotBeNull();
        }

        #endregion

        #region Empty Settings Creates Valid Defaults

        [Fact]
        public void EmptySettings_CreatesValidDefaults()
        {
            // A default-constructed SerializableSettings should have all sub-objects valid
            var settings = new SerializableSettings();

            // All settings objects should be non-null with valid defaults
            settings.ESIKeys.Should().NotBeNull().And.BeEmpty();
            settings.Characters.Should().NotBeNull().And.BeEmpty();
            settings.Plans.Should().NotBeNull().And.BeEmpty();
            settings.MonitoredCharacters.Should().NotBeNull().And.BeEmpty();
            settings.Revision.Should().Be(0);
            settings.SSOClientID.Should().NotBeNull();
            settings.SSOClientSecret.Should().NotBeNull();

            // All nested settings should be initialized
            settings.UI.Should().NotBeNull();
            settings.Notifications.Should().NotBeNull();
            settings.Updates.Should().NotBeNull();
            settings.Proxy.Should().NotBeNull();
        }

        [Fact]
        public void EmptyJsonConfig_CreatesValidDefaults()
        {
            var config = new JsonConfig();
            config.Version.Should().Be(1);
            config.ForkId.Should().Be("aliacollins");
        }

        [Fact]
        public void EmptyJsonCredentials_CreatesValidDefaults()
        {
            var creds = new JsonCredentials();
            creds.Version.Should().Be(1);
            creds.EsiKeys.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void EmptyJsonCharacterIndex_CreatesValidDefaults()
        {
            var index = new JsonCharacterIndex();
            index.Version.Should().Be(1);
            index.Characters.Should().NotBeNull().And.BeEmpty();
            index.MonitoredCharacterIds.Should().NotBeNull().And.BeEmpty();
        }

        #endregion

        #region XML Settings Round-Trip Extended

        [Fact]
        public void SerializableSettings_FullPopulated_XmlRoundTrip()
        {
            var settings = new SerializableSettings
            {
                Revision = 42,
                ForkId = "aliacollins",
                ForkVersion = "5.1.3",
                SSOClientID = "client-id-abc",
                SSOClientSecret = "client-secret-xyz"
            };

            // Add ESI keys
            settings.ESIKeys.Add(new SerializableESIKey
            {
                ID = 100001,
                RefreshToken = "token-a",
                AccessMask = 512,
                Monitored = true
            });
            settings.ESIKeys.Add(new SerializableESIKey
            {
                ID = 100002,
                RefreshToken = "token-b",
                AccessMask = 1024,
                Monitored = false
            });

            // Add a plan
            var plan = new SerializablePlan { Name = "Test Plan", Description = "For testing" };
            plan.Entries.Add(new SerializablePlanEntry { ID = 3350, SkillName = "Caldari Battleship", Level = 4, Priority = 1 });
            plan.Entries.Add(new SerializablePlanEntry { ID = 3351, SkillName = "Minmatar Cruiser", Level = 5, Priority = 2 });
            settings.Plans.Add(plan);

            // Add a CCP character
            var character = new SerializableCCPCharacter
            {
                Guid = Guid.NewGuid(),
                Label = "Main",
                ID = 2119000001,
                Name = "Test Pilot"
            };
            character.LastUpdates.Add(new SerializableAPIUpdate
            {
                Method = "CharacterSheet",
                Time = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
            settings.Characters.Add(character);

            var result = XmlRoundTrip(settings);

            result.Revision.Should().Be(42);
            result.ForkId.Should().Be("aliacollins");
            result.SSOClientID.Should().Be("client-id-abc");
            result.ESIKeys.Should().HaveCount(2);
            result.ESIKeys[0].RefreshToken.Should().Be("token-a");
            result.Plans.Should().HaveCount(1);
            result.Plans[0].Entries.Should().HaveCount(2);
            result.Plans[0].Entries[0].SkillName.Should().Be("Caldari Battleship");
            result.Characters.Should().HaveCount(1);
            result.Characters[0].Should().BeOfType<SerializableCCPCharacter>();
            ((SerializableCCPCharacter)result.Characters[0]).LastUpdates.Should().HaveCount(1);
        }

        [Fact]
        public void SerializableSettings_SpecialCharactersInStrings_XmlRoundTrip()
        {
            var settings = new SerializableSettings
            {
                SSOClientID = "id-with-<special>&chars\"",
                SSOClientSecret = "secret'with\"quotes"
            };

            var result = XmlRoundTrip(settings);

            result.SSOClientID.Should().Be("id-with-<special>&chars\"");
            result.SSOClientSecret.Should().Be("secret'with\"quotes");
        }

        #endregion

        #region JSON Edge Cases

        [Fact]
        public void JsonEsiKey_NullRefreshToken_SerializesCorrectly()
        {
            var key = new JsonEsiKey
            {
                CharacterId = 12345,
                RefreshToken = null,
                AccessMask = 0,
                Monitored = false
            };

            string json = JsonSerializer.Serialize(key, JsonWriteOptions);
            var result = JsonSerializer.Deserialize<JsonEsiKey>(json, JsonReadOptions);

            result.Should().NotBeNull();
            result!.CharacterId.Should().Be(12345);
            result.RefreshToken.Should().BeNull();
        }

        [Fact]
        public void JsonConfig_ExtraFields_AreIgnored()
        {
            // JSON with extra fields that don't exist in the class should not cause errors
            string json = @"{ ""version"": 1, ""forkId"": ""aliacollins"", ""unknownField"": ""value"", ""anotherExtra"": 42 }";
            var action = () => JsonSerializer.Deserialize<JsonConfig>(json, JsonReadOptions);

            action.Should().NotThrow("extra fields should be silently ignored during deserialization");
        }

        [Fact]
        public void JsonCredentials_UnicodeInToken_Preserved()
        {
            var creds = new JsonCredentials
            {
                EsiKeys = new List<JsonEsiKey>
                {
                    new() { CharacterId = 1, RefreshToken = "token-with-unicode-\u00e9\u00e8\u00ea" }
                }
            };

            string json = JsonSerializer.Serialize(creds, JsonWriteOptions);
            var result = JsonSerializer.Deserialize<JsonCredentials>(json, JsonReadOptions);

            result!.EsiKeys[0].RefreshToken.Should().Contain("\u00e9");
        }

        #endregion

        #region Helpers

        private static T XmlRoundTrip<T>(T obj) where T : class
        {
            var serializer = new XmlSerializer(typeof(T));
            using var writer = new StringWriter();
            serializer.Serialize(writer, obj);
            using var reader = new StringReader(writer.ToString());
            return (T)serializer.Deserialize(reader)!;
        }

        #endregion
    }
}
