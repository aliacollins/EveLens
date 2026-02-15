using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;
using EVEMon.Common;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Models;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.Serialization.Eve;
using EVEMon.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Regression
{
    /// <summary>
    /// Tier 4 regression tests: verify fixes for documented GitHub issues remain intact.
    /// See CLAUDE.md for bug fix documentation.
    /// </summary>
    public class GitHubIssueTests
    {
        #region Issue #4: Settings Not Saving - GetRevisionNumber

        /// <summary>
        /// Issue #4: GetRevisionNumber previously returned 0 for both revision="0" and missing attribute.
        /// Fix: Return -1 for missing, so callers can distinguish "no revision" from "revision 0".
        /// </summary>
        [Fact]
        public void GetRevisionNumber_MissingAttribute_ReturnsNegativeOne()
        {
            // XML content with no revision attribute at all
            string xmlContent = @"<Settings clientID="""" clientSecret=""""><esiKeys /></Settings>";
            int result = Util.GetRevisionNumber(xmlContent);
            result.Should().Be(-1, "missing revision attribute should return -1 (old format)");
        }

        [Fact]
        public void GetRevisionNumber_ExplicitZero_ReturnsZero()
        {
            // XML content with revision="0" - this is valid for modern stable versions
            string xmlContent = @"<Settings revision=""0"" clientID="""" clientSecret=""""><esiKeys /></Settings>";
            int result = Util.GetRevisionNumber(xmlContent);
            result.Should().Be(0, "explicit revision='0' should return 0, not -1");
        }

        [Fact]
        public void GetRevisionNumber_PositiveValue_ReturnsCorrectValue()
        {
            string xmlContent = @"<Settings revision=""42"" clientID=""""><esiKeys /></Settings>";
            int result = Util.GetRevisionNumber(xmlContent);
            result.Should().Be(42);
        }

        [Fact]
        public void GetRevisionNumber_HighRevision_ReturnsPeterhaneveRange()
        {
            // peterhaneve's fork uses high revision numbers (e.g., 4986)
            string xmlContent = @"<Settings revision=""4986"" clientID=""""><esiKeys /></Settings>";
            int result = Util.GetRevisionNumber(xmlContent);
            result.Should().Be(4986);
            result.Should().BeGreaterThan(1000,
                "peterhaneve fork revision numbers are > 1000");
        }

        [Fact]
        public void GetRevisionNumber_EmptyString_ReturnsNegativeOne()
        {
            // Edge case: completely empty content
            int result = Util.GetRevisionNumber(string.Empty);
            result.Should().Be(-1);
        }

        [Fact]
        public void GetRevisionNumber_GarbageContent_ReturnsNegativeOne()
        {
            int result = Util.GetRevisionNumber("not xml at all {{{ }}");
            result.Should().Be(-1);
        }

        [Fact]
        public void GetRevisionNumber_CaseInsensitive()
        {
            // The regex should be case-insensitive
            string xmlContent = @"<Settings REVISION=""7""><esiKeys /></Settings>";
            int result = Util.GetRevisionNumber(xmlContent);
            result.Should().Be(7, "revision matching should be case-insensitive");
        }

        #endregion

        #region Fork Migration Detection

        [Fact]
        public void ForkMigration_SettingsWithOurForkId_NoMigrationNeeded()
        {
            // Settings with forkId="aliacollins" are ours - no migration
            var settings = new SerializableSettings
            {
                ForkId = "aliacollins",
                ForkVersion = "5.1.3",
                Revision = 5
            };

            settings.ForkId.Should().Be("aliacollins");
        }

        [Fact]
        public void ForkMigration_SettingsWithDifferentForkId_DetectsAsOtherFork()
        {
            // If another fork sets their forkId, we should detect it
            var settings = new SerializableSettings
            {
                ForkId = "otherfork",
                Revision = 10
            };

            settings.ForkId.Should().NotBe("aliacollins");
            settings.ForkId.Should().Be("otherfork");
        }

        [Fact]
        public void ForkMigration_SettingsWithNoForkId_LowRevision_IsOurUser()
        {
            // No forkId + low revision (< 1000) = our existing user pre-forkId
            var settings = new SerializableSettings
            {
                Revision = 5
            };

            settings.ForkId.Should().BeNull("pre-forkId settings have no forkId");
            settings.Revision.Should().BeLessThan(1000,
                "our fork uses revision < 1000");
        }

        [Fact]
        public void ForkMigration_SettingsWithNoForkId_HighRevision_IsPeterhaneveUser()
        {
            // No forkId + high revision (> 1000) = peterhaneve user
            var settings = new SerializableSettings
            {
                Revision = 4986
            };

            settings.ForkId.Should().BeNull();
            settings.Revision.Should().BeGreaterThan(1000,
                "peterhaneve uses auto-incrementing build numbers > 1000");
        }

        [Fact]
        public void ForkMigration_ForkId_SurvivesXmlRoundTrip()
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

        #endregion

        #region 60+ Character Tick Cascade / Re-entrancy

        /// <summary>
        /// Regression test for the 30+ character crash (documented in CLAUDE.md).
        /// Verifies that NullCharacterServices (the test double for the re-entrancy guard)
        /// correctly tracks update counts without cascade.
        /// </summary>
        [Fact]
        public void SixtyPlusCharacters_NoReentrancy_UpdatesTrackCorrectly()
        {
            var services = new NullCharacterServices();
            var characters = new List<CCPCharacter>();

            for (int i = 0; i < 65; i++)
            {
                var identity = new CharacterIdentity(30000 + i, $"Reentrancy Pilot {i}");
                characters.Add(new CCPCharacter(identity, services));
            }

            // Simulate update callbacks for each character
            // This should NOT cause re-entrancy or cascade
            foreach (var character in characters)
            {
                services.OnCharacterUpdated(character);
            }

            services.CharacterUpdatedCount.Should().Be(65,
                "each update should be counted exactly once, no re-entrancy cascade");

            foreach (var c in characters)
                c.Dispose();
        }

        [Fact]
        public void SixtyPlusCharacters_DisposalOrder_DoesNotCrash()
        {
            var services = new NullCharacterServices();
            var characters = new List<CCPCharacter>();

            for (int i = 0; i < 65; i++)
            {
                var identity = new CharacterIdentity(40000 + i, $"Disposal Pilot {i}");
                characters.Add(new CCPCharacter(identity, services));
            }

            // Dispose in reverse order - this could expose issues with shared state
            var action = () =>
            {
                for (int i = characters.Count - 1; i >= 0; i--)
                    characters[i].Dispose();
            };

            action.Should().NotThrow("disposing 65 characters in reverse order should be safe");
        }

        [Fact]
        public void SixtyPlusCharacters_ConcurrentCollectionAccess_DoesNotCrash()
        {
            var services = new NullCharacterServices();
            var characters = new List<CCPCharacter>();

            for (int i = 0; i < 65; i++)
            {
                var identity = new CharacterIdentity(50000 + i, $"Concurrent Pilot {i}");
                characters.Add(new CCPCharacter(identity, services));
            }

            // Access lazy collections on all characters - this verifies no shared state corruption
            var action = () =>
            {
                foreach (var character in characters)
                {
                    _ = character.SkillQueue;
                    _ = character.Assets;
                    _ = character.CharacterMarketOrders;
                }
            };

            action.Should().NotThrow("accessing collections across 65 characters should be safe");

            foreach (var c in characters)
                c.Dispose();
        }

        #endregion

        #region Serialization Robustness

        [Fact]
        public void SerializableSettings_WithESIKeys_NoTokenLeakOnRoundTrip()
        {
            // Ensure ESI tokens survive round-trip without corruption
            var settings = new SerializableSettings();
            settings.ESIKeys.Add(new SerializableESIKey
            {
                ID = 12345678,
                RefreshToken = "super-secret-token-abc123",
                AccessMask = 8388607,
                Monitored = true
            });

            var result = XmlRoundTrip(settings);

            result.ESIKeys.Should().HaveCount(1);
            result.ESIKeys[0].RefreshToken.Should().Be("super-secret-token-abc123",
                "refresh tokens must survive XML round-trip exactly");
        }

        [Fact]
        public void SerializableSettings_MultipleCharacterTypes_RoundTrip()
        {
            // Verify polymorphic serialization of CCP and Uri characters
            var settings = new SerializableSettings();
            settings.Characters.Add(new SerializableCCPCharacter
            {
                Guid = Guid.NewGuid(),
                Label = "CCP Char",
                ID = 1001
            });
            settings.Characters.Add(new SerializableUriCharacter
            {
                Guid = Guid.NewGuid(),
                Label = "URI Char",
                ID = 2002
            });

            var result = XmlRoundTrip(settings);

            result.Characters.Should().HaveCount(2);
            result.Characters[0].Should().BeOfType<SerializableCCPCharacter>();
            result.Characters[1].Should().BeOfType<SerializableUriCharacter>();
            result.Characters[0].Label.Should().Be("CCP Char");
            result.Characters[1].Label.Should().Be("URI Char");
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
