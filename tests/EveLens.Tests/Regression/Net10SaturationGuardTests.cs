// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Models;
using EveLens.Common.Serialization.Esi;
using EveLens.Common.Serialization.Eve;
using EveLens.Common.Services.Planetary;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Regression
{
    /// <summary>
    /// Pinning tests for the .NET 10 migration (authored and green on net8 BEFORE the
    /// retarget — see NET10-MIGRATION.md, behavioral audit findings C1/C2/C3).
    ///
    /// .NET 9+ changed out-of-range double→int casts from wrapping (often negative) to
    /// saturating (int.MaxValue). Three guards in this codebase only worked BECAUSE the
    /// overflow wrapped negative; on .NET 10 the saturated value would instead be
    /// selected by <c>Math.Max</c>/"&lt; 0" clamps. The fixes compute in double space and
    /// clamp explicitly, so these invariants hold on any runtime. These tests pin them.
    /// </summary>
    [Collection("StaticData")]
    public class Net10SaturationGuardTests
    {
        // An ID no skill in the datafiles uses — resolves to Skill.UnknownSkill, whose
        // synthetic SkillPointsPerHour = (EndSP - StartSP) / (EndTime - StartTime) is the
        // huge-rate ingredient of the C1 overflow.
        private const int UnknownSkillId = 987654;
        private const int Level3SP = 8000;
        private const int Level4SP = 45255;

        public Net10SaturationGuardTests()
        {
            PlanTestFixture.EnsureStaticSkillsLoaded();
        }

        /// <summary>
        /// Imports a single-entry active queue so the character's queue head matches an
        /// unknown skill training to level 4 — the precondition for QueuedSkill.IsTraining.
        /// </summary>
        private static CCPCharacter CreateCharacterTrainingUnknownSkill()
        {
            var character = PlanTestFixture.CreateTestCharacter(id: 424242L);
            var now = DateTime.UtcNow;
            character.SkillQueue.Import(new List<SerializableQueuedSkill>
            {
                new SerializableQueuedSkill
                {
                    ID = UnknownSkillId, Level = 4,
                    StartSP = Level3SP, EndSP = Level4SP,
                    StartTime = now.AddHours(-1), EndTime = now.AddHours(1),
                },
            });
            return character;
        }

        #region C1 — QueuedSkill.CurrentSP must stay inside its entry's SP window

        [Fact]
        public void CurrentSP_WithStaleEndTimeAndUnknownSkill_NeverExceedsEndSP()
        {
            // The C1 scenario: app resumed from sleep, queue head not yet refreshed.
            // The entry's EndTime is 30 days stale and its recorded training window is
            // 1 second wide, so the unknown-skill synthetic rate is ~1.3e8 SP/hour and
            // the raw estimate (~9.7e10) overflows int. net8 happened to wrap negative
            // (floored to StartSP); net10 saturates to int.MaxValue, which the old
            // Math.Max floor would have SELECTED and persisted via Skill.Export().
            var character = CreateCharacterTrainingUnknownSkill();
            var now = DateTime.UtcNow;
            var staleSerial = new SerializableQueuedSkill
            {
                ID = UnknownSkillId, Level = 4,
                StartSP = Level3SP, EndSP = Level4SP,
                StartTime = now.AddDays(-30), EndTime = now.AddDays(-30).AddSeconds(1),
            };

            DateTime unused = now;
            var queued = new QueuedSkill(character, staleSerial, ref unused);

            queued.IsTraining.Should().BeTrue("the entry matches the live queue head");
            queued.CurrentSP.Should().BeInRange(Level3SP, Level4SP,
                "an entry can never report SP outside its own StartSP..EndSP window, " +
                "on any runtime (NET10-MIGRATION.md finding C1)");
        }

        [Fact]
        public void CurrentSP_MidTraining_StillEstimatesNormally()
        {
            // Guard against over-clamping: a healthy in-window estimate must be unchanged.
            // Unknown skill, 2-hour window, 1 hour elapsed → rate = ceil(37255/2h) SP/hour,
            // estimate = EndSP - 1h × rate ≈ halfway through the window.
            var character = CreateCharacterTrainingUnknownSkill();
            var queued = character.SkillQueue.First();

            int expected = Level4SP - (int)Math.Ceiling((Level4SP - Level3SP) / 2.0);
            queued.CurrentSP.Should().BeCloseTo(expected, 100,
                "clamping must not distort healthy mid-training estimates");
        }

        [Fact]
        public void CurrentSP_NotTraining_ReturnsStartSP()
        {
            // A detached entry that does not match the queue head is not training and
            // reports its starting SP, exactly as before the fix.
            var character = CreateCharacterTrainingUnknownSkill();
            var now = DateTime.UtcNow;
            var otherLevel = new SerializableQueuedSkill
            {
                ID = UnknownSkillId, Level = 5,
                StartSP = Level4SP, EndSP = 256000,
                StartTime = now.AddHours(1), EndTime = now.AddDays(10),
            };

            DateTime unused = now;
            var queued = new QueuedSkill(character, otherLevel, ref unused);

            queued.IsTraining.Should().BeFalse();
            queued.CurrentSP.Should().Be(Level4SP);
        }

        #endregion

        #region C2 — Character.Import skill/queue merge must not inflate skill points

        [Fact]
        public void ImportMerge_TrainingQueueEntry_NeverInflatesSkillpointsPastEndSP()
        {
            // Character.Import merges queued progress into imported skills with
            // Math.Max(skill.Skillpoints, tempSkill.CurrentSP) — the same inverted-guard
            // shape as C1. With CurrentSP clamped at the root, the merge can never push
            // a skill past the queue entry's EndSP on any runtime.
            var character = CreateCharacterTrainingUnknownSkill();
            var now = DateTime.UtcNow;

            var esiSkill = new EsiSkillListItem
            {
                ID = UnknownSkillId, Level = 3, ActiveLevel = 3, Skillpoints = Level3SP,
            };
            var esiSkills = new EsiAPISkills
            {
                TotalSP = Level3SP,
                Skills = new List<EsiSkillListItem> { esiSkill },
            };
            var esiQueue = new EsiAPISkillQueue
            {
                new EsiSkillQueueListItem
                {
                    ID = UnknownSkillId, Level = 4,
                    StartSP = Level3SP, LevelStartSP = Level3SP, EndSP = Level4SP,
                    StartTime = now.AddDays(-30), EndTime = now.AddHours(1),
                },
            };

            character.Import(esiSkills, esiQueue);

            esiSkill.Skillpoints.Should().BeInRange(Level3SP, Level4SP,
                "the queue merge must never raise a skill's SP past the queued level's " +
                "EndSP (NET10-MIGRATION.md finding C2)");
        }

        [Fact]
        public void ImportMerge_CompletedQueueEntry_SetsExactlyEndSP()
        {
            // Completed entries take the explicit EndSP path — pin that it stays exact.
            var character = CreateCharacterTrainingUnknownSkill();
            var now = DateTime.UtcNow;

            var esiSkill = new EsiSkillListItem
            {
                ID = UnknownSkillId, Level = 3, ActiveLevel = 3, Skillpoints = Level3SP,
            };
            var esiSkills = new EsiAPISkills
            {
                TotalSP = Level3SP,
                Skills = new List<EsiSkillListItem> { esiSkill },
            };
            var esiQueue = new EsiAPISkillQueue
            {
                new EsiSkillQueueListItem
                {
                    ID = UnknownSkillId, Level = 4,
                    StartSP = Level3SP, LevelStartSP = Level3SP, EndSP = Level4SP,
                    StartTime = now.AddDays(-30), EndTime = now.AddMinutes(-5),
                },
            };

            character.Import(esiSkills, esiQueue);

            esiSkill.Skillpoints.Should().Be(Level4SP);
            esiSkill.Level.Should().Be(4, "a completed queue entry promotes the skill level");
        }

        #endregion

        #region C3 — PI extraction cycle index must stay inside [0, cycleCount - 1]

        [Theory]
        [InlineData(-3600.0, 1800, 10, 0)]       // future InstallTime → first cycle
        [InlineData(0.0, 1800, 10, 0)]           // just installed → first cycle
        [InlineData(6300.0, 1800, 10, 3)]        // mid-program: 3.5 cycles elapsed → index 3
        [InlineData(90000.0, 1800, 10, 9)]       // past program end (in int range) → last cycle
        public void ComputeCurrentCycleIndex_ClampsToValidRange(
            double elapsedSeconds, int cycleTime, int cycleCount, int expected)
        {
            ProductionChainAnalyzer.ComputeCurrentCycleIndex(elapsedSeconds, cycleTime, cycleCount)
                .Should().Be(expected);
        }

        [Fact]
        public void ComputeCurrentCycleIndex_AncientInstallTime_ClampsToLastCycle()
        {
            // The C3 scenario: degenerate stored data where elapsed/cycleTime exceeds int
            // range (~63 billion seconds since DateTime.MinValue at a 1-second cycle time).
            // The raw (int) cast was runtime-defined — net8 wrapped negative (the "< 0"
            // guard then picked cycle 0, an overflow accident), net10 saturates. The index
            // is now clamped in double space: anything past the program's end is the LAST
            // cycle, consistent with in-range elapsed values past the end. Deliberate,
            // documented behavior change for unreachable-with-real-ESI-data input
            // (real extractor cycles are ≥ 15 minutes; overflow would need ~60,000 years).
            double elapsed = (DateTime.UtcNow - DateTime.MinValue).TotalSeconds;

            int index = ProductionChainAnalyzer.ComputeCurrentCycleIndex(elapsed, 1, 10);

            index.Should().Be(9,
                "an expired program reports its last (near-exhausted) cycle, never a " +
                "negative index or int.MaxValue (NET10-MIGRATION.md finding C3)");
        }

        [Theory]
        [InlineData(0, 10)]   // zero cycle time
        [InlineData(1800, 0)] // no yields
        [InlineData(-5, -1)]  // garbage
        public void ComputeCurrentCycleIndex_DegenerateInputs_ReturnZero(
            int cycleTime, int cycleCount)
        {
            ProductionChainAnalyzer.ComputeCurrentCycleIndex(1e12, cycleTime, cycleCount)
                .Should().Be(0);
        }

        #endregion
    }
}
