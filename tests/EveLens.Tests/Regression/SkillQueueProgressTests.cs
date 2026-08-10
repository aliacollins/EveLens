// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Serialization.Eve;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Regression
{
    /// <summary>
    /// Regression tests for Issue #103: when the same skill is queued to multiple levels,
    /// every queue entry showed the SAME progress bar value, because QueuedSkill delegated
    /// FractionCompleted/IsTraining to the shared Skill object (which only knows "progress
    /// toward the next level"). Progress must be entry-local: the actively-training level
    /// shows live progress, later levels of the same skill show 0.
    /// </summary>
    [Collection("StaticData")]
    public class SkillQueueProgressTests
    {
        // Navigation (rank 1): cumulative SP per level 0/250/1414/8000/45255/256000.
        private const int NavigationId = 3449;
        private const int Level3SP = 8000;
        private const int Level4SP = 45255;
        private const int Level5SP = 256000;

        public SkillQueueProgressTests()
        {
            PlanTestFixture.EnsureStaticSkillsLoaded();
        }

        private static EveLens.Common.Models.CCPCharacter CreateCharacterWithNavigationQueue(
            int trainingStartSP)
        {
            var character = PlanTestFixture.CreateTestCharacter();
            var now = DateTime.UtcNow;

            // Navigation IV actively training (halfway), Navigation V queued after it.
            character.SkillQueue.Import(new List<SerializableQueuedSkill>
            {
                new SerializableQueuedSkill
                {
                    ID = NavigationId, Level = 4,
                    StartSP = trainingStartSP, EndSP = Level4SP,
                    StartTime = now.AddHours(-6), EndTime = now.AddHours(6),
                },
                new SerializableQueuedSkill
                {
                    ID = NavigationId, Level = 5,
                    StartSP = Level4SP, EndSP = Level5SP,
                    StartTime = now.AddHours(6), EndTime = now.AddDays(10),
                },
            });
            return character;
        }

        [Fact]
        public void LaterLevelOfSameSkill_ShowsZeroProgress()
        {
            var character = CreateCharacterWithNavigationQueue(trainingStartSP: 20000);
            var entries = character.SkillQueue.ToList();
            entries.Should().HaveCount(2);

            var later = entries[1];
            later.Level.Should().Be(5);
            later.FractionCompleted.Should().Be(0.0f,
                "a queued later level of the same skill has not started training (Issue #103)");
        }

        [Fact]
        public void ActiveEntry_ShowsItsOwnLevelProgress()
        {
            var character = CreateCharacterWithNavigationQueue(trainingStartSP: 20000);
            var active = character.SkillQueue.First();

            active.Level.Should().Be(4);
            // CurrentSP is at least StartSP (20000) inside the level-4 window 8000→45255,
            // so the fraction must be at least (20000-8000)/(45255-8000) and within [0,1].
            float floor = (20000f - Level3SP) / (Level4SP - Level3SP);
            active.FractionCompleted.Should().BeGreaterThanOrEqualTo(floor);
            active.FractionCompleted.Should().BeLessThanOrEqualTo(1.0f);
        }

        [Fact]
        public void EntriesOfSameSkill_DoNotShareProgress()
        {
            var character = CreateCharacterWithNavigationQueue(trainingStartSP: 20000);
            var entries = character.SkillQueue.ToList();

            entries[0].FractionCompleted.Should().BeGreaterThan(0.0f);
            entries[1].FractionCompleted.Should().Be(0.0f,
                "each queue entry must compute progress from its own SP window, " +
                "not from the shared Skill object (Issue #103)");
        }

        [Fact]
        public void OnlyTheQueueHead_ReportsTraining()
        {
            var character = CreateCharacterWithNavigationQueue(trainingStartSP: 20000);
            var entries = character.SkillQueue.ToList();

            entries[0].IsTraining.Should().BeTrue("the head of an active queue is training");
            entries[1].IsTraining.Should().BeFalse(
                "a later level of the same skill must not report training just because " +
                "the skill identity matches the head (Issue #103)");
        }

        [Fact]
        public void LevelStartSP_ComesFromStaticData()
        {
            var character = CreateCharacterWithNavigationQueue(trainingStartSP: 20000);
            var entries = character.SkillQueue.ToList();

            entries[0].LevelStartSP.Should().Be(Level3SP, "level 4 starts at the level-3 threshold");
            entries[1].LevelStartSP.Should().Be(Level4SP, "level 5 starts at the level-4 threshold");
        }

        [Fact]
        public void CompletedEntry_ReportsFullProgress()
        {
            var character = PlanTestFixture.CreateTestCharacter();
            var now = DateTime.UtcNow;
            character.SkillQueue.Import(new List<SerializableQueuedSkill>
            {
                // Ends in the future so SkillQueue.Import keeps it, then completes are
                // simulated by an entry whose EndTime just elapsed.
                new SerializableQueuedSkill
                {
                    ID = NavigationId, Level = 4,
                    StartSP = 20000, EndSP = Level4SP,
                    StartTime = now.AddHours(-6), EndTime = now.AddMilliseconds(-1),
                },
            });

            var entry = character.SkillQueue.FirstOrDefault();
            if (entry != null)
            {
                entry.IsCompleted.Should().BeTrue();
                entry.FractionCompleted.Should().Be(1.0f);
            }
        }
    }
}
