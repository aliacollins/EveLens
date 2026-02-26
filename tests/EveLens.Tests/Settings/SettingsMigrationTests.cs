// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Services;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Settings
{
    /// <summary>
    /// Fork migration detection tests using <see cref="SmartSettingsManager.DetectForkMigration"/>.
    /// Validates the heuristics that detect settings files from other EveLens forks
    /// (peterhaneve, etc.) and trigger the appropriate migration path.
    /// </summary>
    public class SettingsMigrationTests
    {
        #region Peterhaneve Detection by Revision Threshold

        [Fact]
        public void DetectPeterhaneveForK_ByRevisionThreshold_HighRevisionWithEsiKeys_MigrationDetected()
        {
            // Arrange - peterhaneve users typically have revision > 1000
            string content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Settings revision=""2500"">
  <esiKeys>
    <esikey accessMask=""456"" refreshToken=""peter-refresh-token"" characterID=""12345"" />
  </esiKeys>
</Settings>";

            // Act
            var result = SmartSettingsManager.DetectForkMigration(content);

            // Assert
            result.MigrationDetected.Should().BeTrue(
                "revision > 1000 without forkId implies peterhaneve user");
            result.DetectedForkId.Should().BeNull();
            result.DetectedRevision.Should().Be(2500);
            result.HasEsiKeys.Should().BeTrue();
        }

        [Fact]
        public void DetectPeterhaneveForK_ExactThreshold_RevisionAt1001_MigrationDetected()
        {
            // Arrange - just above the threshold
            string content = @"<Settings revision=""1001"">
  <esiKeys>
    <esikey refreshToken=""tok"" />
  </esiKeys>
</Settings>";

            // Act
            var result = SmartSettingsManager.DetectForkMigration(content);

            // Assert
            result.MigrationDetected.Should().BeTrue(
                "revision 1001 > 1000 threshold triggers peterhaneve detection");
            result.DetectedRevision.Should().Be(1001);
        }

        [Fact]
        public void DetectPeterhaneveForK_ExactThreshold_RevisionAt1000_NoMigration()
        {
            // Arrange - exactly at the threshold (not above)
            string content = @"<Settings revision=""1000"">
  <esiKeys>
    <esikey refreshToken=""tok"" />
  </esiKeys>
</Settings>";

            // Act
            var result = SmartSettingsManager.DetectForkMigration(content);

            // Assert
            result.MigrationDetected.Should().BeFalse(
                "revision 1000 is NOT > 1000, so it's treated as our existing user");
            result.NeedsForkIdAdded.Should().BeTrue();
            result.DetectedRevision.Should().Be(1000);
        }

        [Fact]
        public void DetectPeterhaneveForK_VeryHighRevision_MigrationDetected()
        {
            // Arrange - very high revision like a long-time peterhaneve user
            string content = @"<Settings revision=""9999"">
  <esiKeys>
    <esikey refreshToken=""long-time-user-token"" characterID=""98765"" />
    <esikey refreshToken=""second-char-token"" characterID=""98766"" />
  </esiKeys>
</Settings>";

            // Act
            var result = SmartSettingsManager.DetectForkMigration(content);

            // Assert
            result.MigrationDetected.Should().BeTrue();
            result.DetectedRevision.Should().Be(9999);
            result.HasEsiKeys.Should().BeTrue();
        }

        [Fact]
        public void DetectPeterhaneveForK_HighRevisionNoEsiKeys_NoMigration()
        {
            // Arrange - high revision but no ESI keys = no active authentication to migrate
            string content = @"<Settings revision=""5000"">
  <esiKeys></esiKeys>
</Settings>";

            // Act
            var result = SmartSettingsManager.DetectForkMigration(content);

            // Assert
            result.MigrationDetected.Should().BeFalse(
                "even with high revision, no ESI keys means nothing to migrate");
            result.NeedsForkIdAdded.Should().BeTrue();
            result.HasEsiKeys.Should().BeFalse();
        }

        #endregion

        #region Migration from Old XML Succeeds Cleanly

        [Fact]
        public void MigrationFromOldXml_OurExistingUser_LowRevision_NoMigrationNeeded()
        {
            // Arrange - our own user before forkId was introduced
            string content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Settings revision=""5"">
  <esiKeys>
    <esikey refreshToken=""our-user-token"" characterID=""11111"" />
  </esiKeys>
  <plans>
    <plan name=""Test Plan"" />
  </plans>
</Settings>";

            // Act
            var result = SmartSettingsManager.DetectForkMigration(content);

            // Assert
            result.MigrationDetected.Should().BeFalse(
                "low revision without forkId is our existing user, not a fork migration");
            result.NeedsForkIdAdded.Should().BeTrue(
                "forkId should be added silently for our existing user");
            result.DetectedRevision.Should().Be(5);
            result.HasEsiKeys.Should().BeTrue();
        }

        [Fact]
        public void MigrationFromOldXml_EmptySettings_NoMigration()
        {
            // Arrange - fresh install or empty settings
            string content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Settings revision=""0"">
  <esiKeys></esiKeys>
</Settings>";

            // Act
            var result = SmartSettingsManager.DetectForkMigration(content);

            // Assert
            result.MigrationDetected.Should().BeFalse();
            result.NeedsForkIdAdded.Should().BeTrue();
            result.DetectedRevision.Should().Be(0);
            result.HasEsiKeys.Should().BeFalse();
        }

        [Fact]
        public void MigrationFromOldXml_NoRevisionAttribute_ReturnsNegativeOne()
        {
            // Arrange - missing revision attribute entirely
            string content = @"<Settings>
  <esiKeys></esiKeys>
</Settings>";

            // Act
            var result = SmartSettingsManager.DetectForkMigration(content);

            // Assert
            result.DetectedRevision.Should().Be(-1,
                "missing revision attribute should result in -1");
            result.MigrationDetected.Should().BeFalse();
            result.NeedsForkIdAdded.Should().BeTrue();
        }

        #endregion

        #region ForkId Persistence After Migration

        [Fact]
        public void ForkId_AliaCollins_NoMigrationNeeded()
        {
            // Arrange - settings already have our forkId
            string content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Settings revision=""5"" forkId=""aliacollins"" forkVersion=""5.1.2"">
  <esiKeys>
    <esikey refreshToken=""our-token"" characterID=""12345"" />
  </esiKeys>
</Settings>";

            // Act
            var result = SmartSettingsManager.DetectForkMigration(content);

            // Assert
            result.MigrationDetected.Should().BeFalse();
            result.NeedsForkIdAdded.Should().BeFalse(
                "forkId is already present and matches our fork");
            result.DetectedForkId.Should().Be("aliacollins");
        }

        [Fact]
        public void ForkId_DifferentForkWithEsiKeys_MigrationDetected()
        {
            // Arrange - settings from another named fork with ESI keys
            string content = @"<Settings revision=""10"" forkId=""someotherfork"" forkVersion=""3.0.0"">
  <esiKeys>
    <esikey refreshToken=""foreign-token"" characterID=""99999"" />
  </esiKeys>
</Settings>";

            // Act
            var result = SmartSettingsManager.DetectForkMigration(content);

            // Assert
            result.MigrationDetected.Should().BeTrue(
                "different forkId with ESI keys requires migration");
            result.DetectedForkId.Should().Be("someotherfork");
            result.HasEsiKeys.Should().BeTrue();
        }

        [Fact]
        public void ForkId_DifferentForkWithoutEsiKeys_NoMigration_JustUpdateForkId()
        {
            // Arrange - settings from another fork but no ESI keys
            string content = @"<Settings revision=""10"" forkId=""otherfork"">
  <esiKeys></esiKeys>
</Settings>";

            // Act
            var result = SmartSettingsManager.DetectForkMigration(content);

            // Assert
            result.MigrationDetected.Should().BeFalse(
                "no ESI keys means no migration needed, just update forkId");
            result.NeedsForkIdAdded.Should().BeTrue();
            result.HasEsiKeys.Should().BeFalse();
            result.DetectedForkId.Should().Be("otherfork");
        }

        [Fact]
        public void ForkId_CaseMatching_IsExact()
        {
            // Arrange - forkId is case-sensitive
            string content = @"<Settings revision=""5"" forkId=""AliACollins"">
  <esiKeys>
    <esikey refreshToken=""tok"" />
  </esiKeys>
</Settings>";

            // Act
            var result = SmartSettingsManager.DetectForkMigration(content);

            // Assert
            result.DetectedForkId.Should().Be("AliACollins");
            // "AliACollins" != "aliacollins", so it's treated as a different fork
            result.MigrationDetected.Should().BeTrue(
                "forkId matching is case-sensitive; 'AliACollins' != 'aliacollins'");
        }

        #endregion

        #region ParseRevisionNumber Edge Cases

        [Fact]
        public void ParseRevisionNumber_ValidValue_ReturnsCorrectNumber()
        {
            // Act
            int revision = SmartSettingsManager.ParseRevisionNumber(@"<Settings revision=""42"">");

            // Assert
            revision.Should().Be(42);
        }

        [Fact]
        public void ParseRevisionNumber_Zero_ReturnsZero()
        {
            // Act
            int revision = SmartSettingsManager.ParseRevisionNumber(@"<Settings revision=""0"">");

            // Assert
            revision.Should().Be(0);
        }

        [Fact]
        public void ParseRevisionNumber_MissingAttribute_ReturnsNegativeOne()
        {
            // Act
            int revision = SmartSettingsManager.ParseRevisionNumber(@"<Settings>");

            // Assert
            revision.Should().Be(-1);
        }

        [Fact]
        public void ParseRevisionNumber_LargeValue_ReturnsCorrectNumber()
        {
            // Act
            int revision = SmartSettingsManager.ParseRevisionNumber(@"<Settings revision=""999999"">");

            // Assert
            revision.Should().Be(999999);
        }

        [Fact]
        public void ParseRevisionNumber_NonNumericValue_ReturnsNegativeOne()
        {
            // Act
            int revision = SmartSettingsManager.ParseRevisionNumber(@"<Settings revision=""abc"">");

            // Assert
            revision.Should().Be(-1,
                "non-numeric revision values should be treated as missing");
        }

        #endregion

        #region Null Input Handling

        [Fact]
        public void DetectForkMigration_NullContent_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => SmartSettingsManager.DetectForkMigration(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void DetectForkMigration_EmptyContent_NoMigration()
        {
            // Arrange
            string content = "";

            // Act
            var result = SmartSettingsManager.DetectForkMigration(content);

            // Assert
            result.MigrationDetected.Should().BeFalse();
            result.DetectedForkId.Should().BeNull();
            result.DetectedRevision.Should().Be(-1);
            result.HasEsiKeys.Should().BeFalse();
        }

        #endregion

        #region Constants Verification

        [Fact]
        public void OurForkId_IsAliaCollins()
        {
            // Assert - verify the constant matches expected value
            SmartSettingsManager.OurForkId.Should().Be("aliacollins");
        }

        [Fact]
        public void PeterhaneveRevisionThreshold_Is1000()
        {
            // Assert - verify the threshold constant
            SmartSettingsManager.PeterhaneveRevisionThreshold.Should().Be(1000);
        }

        #endregion
    }
}
