// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using EVEMon.Common.Serialization.Settings;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Models
{
    /// <summary>
    /// Tests for ESI key serialization and basic ESIKey behavior.
    /// Full ESIKey construction requires EveMonClient for CharacterIdentities.
    /// These tests cover the serializable layer and the ESIKey public constructor.
    /// </summary>
    public class ESIKeyTests
    {
        #region SerializableESIKey basics

        [Fact]
        public void SerializableESIKey_DefaultConstructor_HasDefaults()
        {
            var key = new SerializableESIKey();
            key.ID.Should().Be(0);
            key.RefreshToken.Should().BeNull();
            key.AccessMask.Should().Be(0UL);
            key.Monitored.Should().BeFalse();
        }

        [Fact]
        public void SerializableESIKey_SetProperties_Preserves()
        {
            var key = new SerializableESIKey
            {
                ID = 2119000001,
                RefreshToken = "rt_abc123def456",
                AccessMask = 8388607,
                Monitored = true
            };

            key.ID.Should().Be(2119000001);
            key.RefreshToken.Should().Be("rt_abc123def456");
            key.AccessMask.Should().Be(8388607UL);
            key.Monitored.Should().BeTrue();
        }

        #endregion

        #region XML round-trip

        [Fact]
        public void SerializableESIKey_XmlRoundTrip_PreservesID()
        {
            var key = new SerializableESIKey { ID = 9876543210 };
            var result = XmlRoundTrip(key);
            result.ID.Should().Be(9876543210);
        }

        [Fact]
        public void SerializableESIKey_XmlRoundTrip_PreservesRefreshToken()
        {
            var key = new SerializableESIKey { RefreshToken = "rt_long_refresh_token_value" };
            var result = XmlRoundTrip(key);
            result.RefreshToken.Should().Be("rt_long_refresh_token_value");
        }

        [Fact]
        public void SerializableESIKey_XmlRoundTrip_PreservesAuthorizedScopes()
        {
            var key = new SerializableESIKey
            {
                AuthorizedScopes = new List<string> { "esi-skills.read_skills.v1", "esi-wallet.read_character_wallet.v1" }
            };
            var result = XmlRoundTrip(key);
            result.AuthorizedScopes.Should().HaveCount(2);
            result.AuthorizedScopes.Should().Contain("esi-skills.read_skills.v1");
        }

        [Fact]
        public void SerializableESIKey_XmlRoundTrip_PreservesMonitoredTrue()
        {
            var key = new SerializableESIKey { Monitored = true };
            var result = XmlRoundTrip(key);
            result.Monitored.Should().BeTrue();
        }

        [Fact]
        public void SerializableESIKey_XmlRoundTrip_PreservesMonitoredFalse()
        {
            var key = new SerializableESIKey { Monitored = false };
            var result = XmlRoundTrip(key);
            result.Monitored.Should().BeFalse();
        }

        [Fact]
        public void SerializableESIKey_XmlRoundTrip_NullRefreshToken()
        {
            var key = new SerializableESIKey { RefreshToken = null };
            var result = XmlRoundTrip(key);
            // Null may round-trip as null or empty
            result.RefreshToken.Should().BeNullOrEmpty();
        }

        #endregion

        #region ESIKey construction (from serialized)

        [Fact]
        public void ESIKey_FromSerialized_PreservesID()
        {
            var serial = new SerializableESIKey
            {
                ID = 2119000001,
                RefreshToken = "rt_test",
                AccessMask = 1234,
                Monitored = true
            };

            var key = new Common.Models.ESIKey(serial);
            key.ID.Should().Be(2119000001);
        }

        [Fact]
        public void ESIKey_FromSerialized_PreservesAuthorizedScopes()
        {
            var serial = new SerializableESIKey
            {
                ID = 1,
                Monitored = false,
                AuthorizedScopes = new List<string> { "esi-skills.read_skills.v1", "esi-skills.read_skillqueue.v1" }
            };

            var key = new Common.Models.ESIKey(serial);
            key.AuthorizedScopes.Should().HaveCount(2);
            key.AuthorizedScopes.Should().Contain("esi-skills.read_skills.v1");
        }

        [Fact]
        public void ESIKey_FromSerialized_PreservesMonitoredState()
        {
            var serialMonitored = new SerializableESIKey
            {
                ID = 1,
                Monitored = true
            };
            var serialNotMonitored = new SerializableESIKey
            {
                ID = 2,
                Monitored = false
            };

            var keyMonitored = new Common.Models.ESIKey(serialMonitored);
            var keyNotMonitored = new Common.Models.ESIKey(serialNotMonitored);

            keyMonitored.Monitored.Should().BeTrue();
            keyNotMonitored.Monitored.Should().BeFalse();
        }

        [Fact]
        public void ESIKey_FromSerialized_NullRefreshToken_HandledGracefully()
        {
            var serial = new SerializableESIKey
            {
                ID = 1,
                RefreshToken = null
            };

            var key = new Common.Models.ESIKey(serial);
            key.RefreshToken.Should().BeEmpty();
        }

        [Fact]
        public void ESIKey_FromSerialized_EmptyRefreshToken_Preserves()
        {
            var serial = new SerializableESIKey
            {
                ID = 1,
                RefreshToken = ""
            };

            var key = new Common.Models.ESIKey(serial);
            key.RefreshToken.Should().BeEmpty();
        }

        #endregion

        #region ESIKey public constructor

        [Fact]
        public void ESIKey_PublicConstructor_SetsIDAndMonitored()
        {
            var key = new Common.Models.ESIKey(42);
            key.ID.Should().Be(42);
            key.Monitored.Should().BeTrue();
            key.RefreshToken.Should().BeEmpty();
        }

        [Fact]
        public void ESIKey_PublicConstructor_HasNoError()
        {
            var key = new Common.Models.ESIKey(1);
            key.HasError.Should().BeFalse();
        }

        #endregion

        #region ESIKey.IsProcessed

        [Fact]
        public void ESIKey_NotMonitored_IsProcessed()
        {
            var serial = new SerializableESIKey
            {
                ID = 1,
                Monitored = false
            };

            var key = new Common.Models.ESIKey(serial);
            // Not monitored => IsProcessed should be true (m_queried=false but m_monitored=false)
            key.IsProcessed.Should().BeTrue();
        }

        [Fact]
        public void ESIKey_MonitoredNotQueried_IsNotProcessed()
        {
            var serial = new SerializableESIKey
            {
                ID = 1,
                Monitored = true
            };

            var key = new Common.Models.ESIKey(serial);
            // Monitored but not yet queried => not processed
            key.IsProcessed.Should().BeFalse();
        }

        #endregion

        #region ESIKey Equals and GetHashCode

        [Fact]
        public void ESIKey_Equals_SameID_ReturnsTrue()
        {
            var key1 = new Common.Models.ESIKey(42);
            var key2 = new Common.Models.ESIKey(42);
            key1.Equals(key2).Should().BeTrue();
        }

        [Fact]
        public void ESIKey_Equals_DifferentID_ReturnsFalse()
        {
            var key1 = new Common.Models.ESIKey(1);
            var key2 = new Common.Models.ESIKey(2);
            key1.Equals(key2).Should().BeFalse();
        }

        [Fact]
        public void ESIKey_Equals_Null_ReturnsFalse()
        {
            var key = new Common.Models.ESIKey(1);
            key.Equals(null).Should().BeFalse();
        }

        [Fact]
        public void ESIKey_GetHashCode_SameID_SameHash()
        {
            var key1 = new Common.Models.ESIKey(42);
            var key2 = new Common.Models.ESIKey(42);
            key1.GetHashCode().Should().Be(key2.GetHashCode());
        }

        [Fact]
        public void ESIKey_GetHashCode_DifferentID_DifferentHash()
        {
            var key1 = new Common.Models.ESIKey(1);
            var key2 = new Common.Models.ESIKey(2);
            // Different IDs should typically produce different hash codes
            key1.GetHashCode().Should().NotBe(key2.GetHashCode());
        }

        #endregion

        #region ESIKey Export

        [Fact]
        public void ESIKey_Export_PreservesID()
        {
            var serial = new SerializableESIKey
            {
                ID = 2119000001,
                RefreshToken = "rt_token",
                Monitored = true,
                AuthorizedScopes = new List<string> { "esi-skills.read_skills.v1" }
            };

            var key = new Common.Models.ESIKey(serial);
            var exported = key.Export();

            exported.ID.Should().Be(2119000001);
            exported.RefreshToken.Should().Be("rt_token");
            exported.AuthorizedScopes.Should().Contain("esi-skills.read_skills.v1");
            exported.Monitored.Should().BeTrue();
        }

        [Fact]
        public void ESIKey_Export_RoundTrip_PreservesAll()
        {
            var original = new SerializableESIKey
            {
                ID = 12345,
                RefreshToken = "rt_round_trip",
                Monitored = true,
                AuthorizedScopes = new List<string> { "esi-skills.read_skills.v1", "esi-wallet.read_character_wallet.v1" }
            };

            var key = new Common.Models.ESIKey(original);
            var exported = key.Export();

            exported.ID.Should().Be(original.ID);
            exported.RefreshToken.Should().Be(original.RefreshToken);
            exported.AuthorizedScopes.Should().BeEquivalentTo(original.AuthorizedScopes);
            exported.Monitored.Should().Be(original.Monitored);
        }

        #endregion

        #region Settings collection context

        [Fact]
        public void SerializableSettings_ESIKeys_MultipleKeys_PreserveAll()
        {
            var settings = new SerializableSettings();
            settings.ESIKeys.Add(new SerializableESIKey
            {
                ID = 1,
                RefreshToken = "rt_first",
                AccessMask = 100,
                Monitored = true
            });
            settings.ESIKeys.Add(new SerializableESIKey
            {
                ID = 2,
                RefreshToken = "rt_second",
                AccessMask = 200,
                Monitored = false
            });

            var result = XmlRoundTrip(settings);
            result.ESIKeys.Should().HaveCount(2);
            result.ESIKeys[0].ID.Should().Be(1);
            result.ESIKeys[0].RefreshToken.Should().Be("rt_first");
            result.ESIKeys[1].ID.Should().Be(2);
            result.ESIKeys[1].Monitored.Should().BeFalse();
        }

        #endregion

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
