// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
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
    /// Tier 4 regression tests: ensure edge cases that previously caused
    /// crashes or could reasonably cause crashes are handled gracefully.
    /// </summary>
    public class CrashPreventionTests
    {
        #region Null/Empty CharacterIdentity

        [Fact]
        public void NullCharacterName_DoesNotCrash()
        {
            // CharacterIdentity constructor is internal, but we have InternalsVisibleTo
            // A null-ish name should not crash creation
            var identity = new CharacterIdentity(0, string.Empty);
            identity.CharacterID.Should().Be(0);
            identity.CharacterName.Should().BeEmpty();
        }

        [Fact]
        public void NullCharacterIdentity_CCPCharacter_DoesNotCrash()
        {
            // Creating a CCPCharacter with a minimal identity should not throw
            var services = new NullCharacterServices();
            var identity = new CharacterIdentity(0, "");

            var character = new CCPCharacter(identity, services);

            character.Should().NotBeNull();
            character.CharacterID.Should().Be(0);
            character.Dispose();
        }

        #endregion

        #region Empty Skill Queue

        [Fact]
        public void EmptySkillQueue_DoesNotCrash()
        {
            // SerializableCCPCharacter with empty skill queue should be safe
            var character = new SerializableCCPCharacter();
            character.SkillQueue.Should().NotBeNull().And.BeEmpty();

            // Accessing properties on empty collections should not throw
            var action = () =>
            {
                foreach (var skill in character.SkillQueue)
                {
                    _ = skill.ID;
                    _ = skill.Level;
                }
            };
            action.Should().NotThrow();
        }

        [Fact]
        public void EmptySkillQueue_CCPCharacter_DoesNotCrash()
        {
            var services = new NullCharacterServices();
            var identity = new CharacterIdentity(9999, "Empty Queue Pilot");
            var character = new CCPCharacter(identity, services);

            // Skill queue should be accessible even when empty
            character.SkillQueue.Should().NotBeNull();
            character.IsTraining.Should().BeFalse();

            character.Dispose();
        }

        #endregion

        #region Zero Characters

        [Fact]
        public void ZeroCharacters_EmptyCollections_DoNotCrash()
        {
            // Simulates an app state with zero characters
            var settings = new SerializableSettings();
            settings.Characters.Should().BeEmpty();
            settings.ESIKeys.Should().BeEmpty();
            settings.Plans.Should().BeEmpty();
            settings.MonitoredCharacters.Should().BeEmpty();

            // Iterating over empty collections should be safe
            var action = () =>
            {
                foreach (var c in settings.Characters) { _ = c.Guid; }
                foreach (var k in settings.ESIKeys) { _ = k.ID; }
                foreach (var p in settings.Plans) { _ = p.Name; }
            };
            action.Should().NotThrow();
        }

        [Fact]
        public void ZeroCharacters_NullCharacterServices_StillFunctions()
        {
            var services = new NullCharacterServices();

            // NullCharacterServices should handle all calls gracefully
            services.AnyESIKeyUnprocessed().Should().BeFalse();
            services.CharacterUpdatedCount.Should().Be(0);

            // Calling event-firing methods with null should not crash
            var action = () =>
            {
                services.OnCharacterUpdated(null!);
                services.OnMarketOrdersUpdated(null!);
                services.OnContractsUpdated(null!);
                services.OnIndustryJobsUpdated(null!);
                services.OnCharacterInfoUpdated(null!);
                services.OnCharacterSkillQueueUpdated(null!);
                services.OnCharacterQueuedSkillsCompleted(null!, null!);
            };
            action.Should().NotThrow();
        }

        #endregion

        #region Serialization Null Safety

        [Fact]
        public void SerializableCCPCharacter_AllNullStrings_DoesNotCrash()
        {
            var character = new SerializableCCPCharacter
            {
                Label = null,
                EveMailMessagesIDs = null,
                EveNotificationsIDs = null
            };

            // All collections should still be initialized
            character.SkillQueue.Should().NotBeNull();
            character.MarketOrders.Should().NotBeNull();
            character.Contracts.Should().NotBeNull();
            character.IndustryJobs.Should().NotBeNull();
            character.LastUpdates.Should().NotBeNull();
        }

        [Fact]
        public void SerializableSettings_DefaultConstructor_AllCollectionsInitialized()
        {
            var settings = new SerializableSettings();

            // All settings sub-objects should be initialized (not null)
            settings.UI.Should().NotBeNull();
            settings.Notifications.Should().NotBeNull();
            settings.Updates.Should().NotBeNull();
            settings.Proxy.Should().NotBeNull();
            settings.Calendar.Should().NotBeNull();
            settings.Exportation.Should().NotBeNull();
            settings.MarketPricer.Should().NotBeNull();
            settings.LoadoutsProvider.Should().NotBeNull();
            settings.CloudStorageServiceProvider.Should().NotBeNull();
            settings.PortableEveInstallations.Should().NotBeNull();
            settings.G15.Should().NotBeNull();
            settings.Scheduler.Should().NotBeNull();
        }

        [Fact]
        public void SerializableCharacterSheetBase_DefaultAttributes_NotNull()
        {
            // SerializableCCPCharacter inherits from SerializableCharacterSheetBase
            var character = new SerializableCCPCharacter();

            character.Attributes.Should().NotBeNull();
            character.Skills.Should().NotBeNull();
            character.Certificates.Should().NotBeNull();
            character.EmploymentHistory.Should().NotBeNull();
        }

        #endregion

        #region Large Scale Character Creation

        [Fact]
        public void CreatingManyCharacters_DoesNotCrash()
        {
            var services = new NullCharacterServices();
            var characters = new List<CCPCharacter>();

            // Create 60+ characters to verify no re-entrancy or resource exhaustion issues
            var action = () =>
            {
                for (int i = 0; i < 65; i++)
                {
                    var identity = new CharacterIdentity(10000 + i, $"Crash Test Pilot {i}");
                    characters.Add(new CCPCharacter(identity, services));
                }
            };

            action.Should().NotThrow();
            characters.Should().HaveCount(65);

            // Dispose all
            foreach (var c in characters)
                c.Dispose();
        }

        [Fact]
        public void CreatingManyCharacters_WithCollectionAccess_DoesNotCrash()
        {
            var services = new NullCharacterServices();
            var characters = new List<CCPCharacter>();

            var action = () =>
            {
                for (int i = 0; i < 30; i++)
                {
                    var identity = new CharacterIdentity(20000 + i, $"Lazy Pilot {i}");
                    var character = new CCPCharacter(identity, services);
                    characters.Add(character);

                    // Trigger lazy collection initialization
                    _ = character.SkillQueue;
                    _ = character.Standings;
                    _ = character.Assets;
                    _ = character.CharacterMarketOrders;
                    _ = character.CharacterContracts;
                    _ = character.CharacterIndustryJobs;
                }
            };

            action.Should().NotThrow();

            foreach (var c in characters)
                c.Dispose();
        }

        #endregion

        #region Edge Case Values

        [Fact]
        public void MaxLongCharacterID_DoesNotCrash()
        {
            var services = new NullCharacterServices();
            var identity = new CharacterIdentity(long.MaxValue, "Max ID Pilot");
            var character = new CCPCharacter(identity, services);

            character.CharacterID.Should().Be(long.MaxValue);
            character.Dispose();
        }

        [Fact]
        public void SerializableQueuedSkill_DefaultDates_DoNotCrash()
        {
            // A queued skill with default (min) dates represents a paused queue
            var skill = new SerializableQueuedSkill();
            skill.IsPaused.Should().BeTrue("default EndTime should be DateTime.MinValue");
            skill.IsCompleted.Should().BeFalse();
            skill.IsTraining.Should().BeFalse();
        }

        [Fact]
        public void SerializableESIKey_MaxAccessMask_DoesNotCrash()
        {
            var key = new SerializableESIKey
            {
                ID = 1,
                AccessMask = ulong.MaxValue,
                Monitored = true,
                RefreshToken = "test"
            };

            key.AccessMask.Should().Be(ulong.MaxValue);
        }

        #endregion
    }
}
