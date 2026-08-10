// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.IO;
using System.Text.Json;
using System.Xml.Serialization;
using EveLens.Common.Models;
using EveLens.Common.Serialization.Settings;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Regression
{
    /// <summary>
    /// Regression tests for Issue #94 (stale ESI key for a deleted/biomassed character).
    /// The key↔character link used to exist only in memory, rebuilt solely by a successful
    /// token refresh — impossible for a permanently dead grant. After a restart the key was
    /// unnamed in notifications and orphaned from character deletion. The link is now
    /// persisted on SerializableESIKey (CharacterID + CharacterName) and restored at import.
    /// </summary>
    public class StaleEsiKeyTests
    {
        private const long CharId = 91234567L;
        private const string CharName = "Biomassed Pilot";

        #region Serialization round-trips (Law 13)

        [Fact]
        public void SerializableESIKey_CharacterLink_RoundTrips_Xml()
        {
            var key = new SerializableESIKey
            {
                ID = 133700000000000001L,
                RefreshToken = "rt_dead",
                CharacterID = CharId,
                CharacterName = CharName,
            };

            var serializer = new XmlSerializer(typeof(SerializableESIKey));
            using var writer = new StringWriter();
            serializer.Serialize(writer, key);
            using var reader = new StringReader(writer.ToString());
            var result = (SerializableESIKey)serializer.Deserialize(reader)!;

            result.CharacterID.Should().Be(CharId);
            result.CharacterName.Should().Be(CharName);
        }

        [Fact]
        public void SerializableESIKey_CharacterLink_RoundTrips_Json()
        {
            var key = new SerializableESIKey
            {
                ID = 133700000000000001L,
                CharacterID = CharId,
                CharacterName = CharName,
            };

            string json = JsonSerializer.Serialize(key,
                Settings.JsonDirectSerializationTests.JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableESIKey>(json,
                Settings.JsonDirectSerializationTests.JsonOptions);

            result!.CharacterID.Should().Be(CharId);
            result.CharacterName.Should().Be(CharName);
        }

        [Fact]
        public void SerializableESIKey_LegacyFileWithoutLink_DeserializesToZero()
        {
            // Settings saved before this field existed have no characterID attribute.
            const string legacyXml =
                "<SerializableESIKey id=\"1\" refreshToken=\"rt\" monitored=\"true\" />";

            var serializer = new XmlSerializer(typeof(SerializableESIKey));
            using var reader = new StringReader(legacyXml);
            var result = (SerializableESIKey)serializer.Deserialize(reader)!;

            result.CharacterID.Should().Be(0, "legacy keys have no persisted link");
            result.CharacterName.Should().BeNull();
        }

        #endregion

        #region Model round-trip

        [Fact]
        public void ESIKey_Export_CarriesCharacterLink()
        {
            var serial = new SerializableESIKey
            {
                ID = 42,
                RefreshToken = "rt_dead",
                CharacterID = CharId,
                CharacterName = CharName,
            };
            var key = new ESIKey(serial);
            key.RestoreCharacterLink(serial.CharacterID, serial.CharacterName);

            key.CharacterID.Should().Be(CharId);
            key.CharacterName.Should().Be(CharName);

            var exported = key.Export();
            exported.CharacterID.Should().Be(CharId,
                "the link must survive export so it persists across restarts (Issue #94)");
            exported.CharacterName.Should().Be(CharName);
        }

        [Fact]
        public void ESIKey_RestoreCharacterLink_ZeroId_IsHarmless()
        {
            var key = new ESIKey(new SerializableESIKey { ID = 42 });

            key.RestoreCharacterLink(0, null);

            key.CharacterID.Should().Be(0);
            key.CharacterName.Should().BeEmpty();
        }

        #endregion
    }
}
