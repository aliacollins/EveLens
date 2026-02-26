// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using EveLens.Common.Models;
using EveLens.Common.Serialization.Settings;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Models
{
    /// <summary>
    /// Tests for ESIKey re-authentication behavior: ClearRefreshToken(),
    /// HasError marking, and the restore contract where stale tokens
    /// are cleared to prevent auto-refresh with invalid PKCE tokens.
    /// </summary>
    public class ESIKeyReauthTests
    {
        #region ClearRefreshToken

        [Fact]
        public void ClearRefreshToken_ClearsExistingToken()
        {
            // Arrange
            var serial = new SerializableESIKey
            {
                ID = 1,
                RefreshToken = "rt_valid_token_abc123"
            };
            var key = new ESIKey(serial);
            key.RefreshToken.Should().Be("rt_valid_token_abc123");

            // Act
            key.ClearRefreshToken();

            // Assert
            key.RefreshToken.Should().BeEmpty();
        }

        [Fact]
        public void ClearRefreshToken_AlreadyEmpty_RemainsEmpty()
        {
            // Arrange
            var serial = new SerializableESIKey
            {
                ID = 1,
                RefreshToken = ""
            };
            var key = new ESIKey(serial);

            // Act
            key.ClearRefreshToken();

            // Assert
            key.RefreshToken.Should().BeEmpty();
        }

        [Fact]
        public void ClearRefreshToken_NullDeserialized_RemainsEmpty()
        {
            // Arrange — null token gets coerced to empty in ESIKey constructor
            var serial = new SerializableESIKey
            {
                ID = 1,
                RefreshToken = null
            };
            var key = new ESIKey(serial);
            key.RefreshToken.Should().BeEmpty();

            // Act
            key.ClearRefreshToken();

            // Assert
            key.RefreshToken.Should().BeEmpty();
        }

        [Fact]
        public void ClearRefreshToken_DoesNotAffectOtherProperties()
        {
            // Arrange
            var serial = new SerializableESIKey
            {
                ID = 42,
                RefreshToken = "rt_token",
#pragma warning disable CS0618
                AccessMask = 8388607,
#pragma warning restore CS0618
                Monitored = true,
                AuthorizedScopes = new List<string> { "esi-skills.read_skills.v1" }
            };
            var key = new ESIKey(serial);

            // Act
            key.ClearRefreshToken();

            // Assert — only RefreshToken should change
            key.ID.Should().Be(42);
            key.AuthorizedScopes.Should().Contain("esi-skills.read_skills.v1");
            key.Monitored.Should().BeTrue();
            key.RefreshToken.Should().BeEmpty();
        }

        [Fact]
        public void ClearRefreshToken_PreventsCheckAccessToken_FromFiring()
        {
            // Arrange — key with valid token that would trigger refresh
            var serial = new SerializableESIKey
            {
                ID = 1,
                RefreshToken = "rt_stale_pkce_token"
            };
            var key = new ESIKey(serial);
            key.RefreshToken.Should().NotBeEmpty();

            // Act — simulate post-restore clearing
            key.ClearRefreshToken();

            // Assert — CheckAccessToken checks !string.IsNullOrEmpty(RefreshToken)
            // With empty token, it should not attempt refresh
            key.RefreshToken.Should().BeEmpty();
        }

        #endregion

        #region HasError Flag

        [Fact]
        public void HasError_DefaultIsFalse()
        {
            var key = new ESIKey(1);
            key.HasError.Should().BeFalse();
        }

        [Fact]
        public void HasError_CanBeSetToTrue()
        {
            // Arrange
            var key = new ESIKey(1);

            // Act
            key.HasError = true;

            // Assert
            key.HasError.Should().BeTrue();
        }

        [Fact]
        public void HasError_CanBeToggledBackToFalse()
        {
            // Arrange
            var key = new ESIKey(1);
            key.HasError = true;

            // Act
            key.HasError = false;

            // Assert
            key.HasError.Should().BeFalse();
        }

        [Fact]
        public void HasError_FromSerialized_DefaultFalse()
        {
            var serial = new SerializableESIKey
            {
                ID = 1,
                RefreshToken = "rt_token",
                Monitored = true
            };
            var key = new ESIKey(serial);
            key.HasError.Should().BeFalse();
        }

        #endregion

        #region Restore Contract: Mark + Clear

        [Fact]
        public void RestoreContract_MarkAndClear_SetsCorrectState()
        {
            // Arrange — simulate a key loaded from backup
            var serial = new SerializableESIKey
            {
                ID = 90000001,
                RefreshToken = "rt_stale_backup_token",
#pragma warning disable CS0618
                AccessMask = ulong.MaxValue,
#pragma warning restore CS0618
                Monitored = true
            };
            var key = new ESIKey(serial);

            // Act — simulate what RestoreAsync does
            key.HasError = true;
            key.ClearRefreshToken();

            // Assert — key marked for re-auth, token cleared
            key.HasError.Should().BeTrue();
            key.RefreshToken.Should().BeEmpty();
            key.ID.Should().Be(90000001);
            // Legacy MaxValue AccessMask migrates to full scopes
            key.AuthorizedScopes.Should().NotBeEmpty();
            key.Monitored.Should().BeTrue();
        }

        [Fact]
        public void RestoreContract_MultipleKeys_AllMarked()
        {
            // Arrange — simulate 5 keys loaded from backup
            var keys = new ESIKey[5];
            for (int i = 0; i < 5; i++)
            {
                keys[i] = new ESIKey(new SerializableESIKey
                {
                    ID = 90000001 + i,
                    RefreshToken = $"rt_stale_token_{i}",
                    Monitored = true
                });
            }

            // Act — mark all for re-auth (as RestoreAsync does)
            foreach (var key in keys)
            {
                key.HasError = true;
                key.ClearRefreshToken();
            }

            // Assert — all keys marked, all tokens cleared
            foreach (var key in keys)
            {
                key.HasError.Should().BeTrue();
                key.RefreshToken.Should().BeEmpty();
            }
        }

        [Fact]
        public void RestoreContract_UnmonitoredKeys_AlsoMarked()
        {
            // Arrange — unmonitored key from backup
            var serial = new SerializableESIKey
            {
                ID = 1,
                RefreshToken = "rt_token",
                Monitored = false
            };
            var key = new ESIKey(serial);

            // Act
            key.HasError = true;
            key.ClearRefreshToken();

            // Assert — even unmonitored keys get marked
            key.HasError.Should().BeTrue();
            key.RefreshToken.Should().BeEmpty();
            key.Monitored.Should().BeFalse();
        }

        #endregion

        #region Export After Restore

        [Fact]
        public void Export_AfterClearRefreshToken_ExportsEmptyToken()
        {
            // Arrange
            var serial = new SerializableESIKey
            {
                ID = 1,
                RefreshToken = "rt_original_token",
                Monitored = true,
                AuthorizedScopes = new List<string> { "esi-skills.read_skills.v1" }
            };
            var key = new ESIKey(serial);

            // Act — clear token then export
            key.ClearRefreshToken();
            var exported = key.Export();

            // Assert — export reflects cleared state
            exported.RefreshToken.Should().BeEmpty();
            exported.ID.Should().Be(1);
            exported.AuthorizedScopes.Should().Contain("esi-skills.read_skills.v1");
            exported.Monitored.Should().BeTrue();
        }

        [Fact]
        public void Export_NormalPath_PreservesRefreshToken()
        {
            // Arrange — normal save (not restore)
            var serial = new SerializableESIKey
            {
                ID = 1,
                RefreshToken = "rt_valid_token",
                AccessMask = 999,
                Monitored = true
            };
            var key = new ESIKey(serial);

            // Act — normal export without clearing
            var exported = key.Export();

            // Assert — token preserved (not cleared)
            exported.RefreshToken.Should().Be("rt_valid_token");
        }

        #endregion

        #region Scale Tests (60+ Characters)

        [Fact]
        public void RestoreContract_SixtyKeys_AllMarkedAndCleared()
        {
            // Arrange — simulate 60+ characters
            const int keyCount = 65;
            var keys = new ESIKey[keyCount];
            for (int i = 0; i < keyCount; i++)
            {
                keys[i] = new ESIKey(new SerializableESIKey
                {
                    ID = 90000001 + i,
                    RefreshToken = $"rt_stale_pkce_{i:D4}",
                    AccessMask = ulong.MaxValue,
                    Monitored = true
                });
            }

            // Act — mark all for re-auth
            foreach (var key in keys)
            {
                key.HasError = true;
                key.ClearRefreshToken();
            }

            // Assert — all 65 keys marked
            for (int i = 0; i < keyCount; i++)
            {
                keys[i].HasError.Should().BeTrue($"key {i} should have error");
                keys[i].RefreshToken.Should().BeEmpty($"key {i} token should be cleared");
                keys[i].ID.Should().Be(90000001 + i);
                keys[i].Monitored.Should().BeTrue();
            }
        }

        [Fact]
        public void RestoreContract_HundredKeys_ExportAfterClear_AllEmpty()
        {
            // Arrange — 100 characters as in stress test
            const int keyCount = 100;
            var keys = new ESIKey[keyCount];
            for (int i = 0; i < keyCount; i++)
            {
                keys[i] = new ESIKey(new SerializableESIKey
                {
                    ID = i + 1,
                    RefreshToken = $"rt_token_{i}",
                    Monitored = true
                });
            }

            // Act — mark and clear all, then export
            foreach (var key in keys)
            {
                key.HasError = true;
                key.ClearRefreshToken();
            }

            // Assert — all exports have empty tokens
            foreach (var key in keys)
            {
                var exported = key.Export();
                exported.RefreshToken.Should().BeEmpty();
            }
        }

        #endregion

        #region XML Serialization of Restore State

        [Fact]
        public void SerializableSettings_WithMultipleESIKeys_SurvivesXmlRoundTrip()
        {
            // Arrange — simulate settings backup with many keys
            var settings = new SerializableSettings();
            for (int i = 0; i < 10; i++)
            {
                settings.ESIKeys.Add(new SerializableESIKey
                {
                    ID = 90000001 + i,
                    RefreshToken = $"rt_backup_token_{i}",
                    AccessMask = ulong.MaxValue,
                    Monitored = i % 2 == 0
                });
            }

            // Act — XML round-trip (simulates backup/restore)
            var result = XmlRoundTrip(settings);

            // Assert — all keys preserved after round-trip
            result.ESIKeys.Should().HaveCount(10);
            for (int i = 0; i < 10; i++)
            {
                result.ESIKeys[i].ID.Should().Be(90000001 + i);
                result.ESIKeys[i].RefreshToken.Should().Be($"rt_backup_token_{i}");
                result.ESIKeys[i].Monitored.Should().Be(i % 2 == 0);
            }
        }

        [Fact]
        public void SerializableSettings_EmptyRefreshTokens_SurviveXmlRoundTrip()
        {
            // Arrange — simulate settings after restore (tokens cleared)
            var settings = new SerializableSettings();
            settings.ESIKeys.Add(new SerializableESIKey
            {
                ID = 1,
                RefreshToken = "", // cleared after restore
                Monitored = true
            });

            // Act
            var result = XmlRoundTrip(settings);

            // Assert — empty token survives
            result.ESIKeys.Should().HaveCount(1);
            result.ESIKeys[0].RefreshToken.Should().BeNullOrEmpty();
        }

        #endregion

        #region Helpers

        private static T XmlRoundTrip<T>(T obj) where T : class
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using var writer = new System.IO.StringWriter();
            serializer.Serialize(writer, obj);
            using var reader = new System.IO.StringReader(writer.ToString());
            return (T)serializer.Deserialize(reader)!;
        }

        #endregion
    }
}
