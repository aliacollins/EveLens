// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using EveLens.Common.Models;
using EveLens.Common.Serialization.Esi;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Models
{
    /// <summary>
    /// A skill's points and levels after the skills list and the skill queue have both been
    /// imported are the same whichever arrives first, and the queue never lowers them.
    /// </summary>
    [Collection("StaticData")]
    public class SkillQueueMergeTests
    {
        // Navigation (rank 1): cumulative skill points per level 250 / 1,414 / 8,000 / 45,255 / 256,000.
        private const int NavigationId = 3449;
        private const int Level3SP = 8000;
        private const int Level4SP = 45255;
        private const int Level5SP = 256000;

        public SkillQueueMergeTests()
        {
            PlanTestFixture.EnsureStaticSkillsLoaded();
        }

        private sealed record QueueEntry(int Level, int StartSP, int LevelStartSP, int EndSP,
            DateTime? Start, DateTime? Finish);

        private static EsiAPISkills Skills(int level, int activeLevel, long skillPoints) => new()
        {
            Skills =
            {
                new EsiSkillListItem
                {
                    ID = NavigationId, Level = level, ActiveLevel = activeLevel, Skillpoints = skillPoints,
                },
            },
        };

        private static EsiAPISkillQueue Queue(params QueueEntry[] entries)
        {
            var json = new StringBuilder("[");
            for (int i = 0; i < entries.Length; i++)
            {
                QueueEntry entry = entries[i];
                if (i > 0) json.Append(',');
                json.Append($"{{\"skill_id\":{NavigationId},\"finished_level\":{entry.Level},");
                json.Append($"\"training_start_sp\":{entry.StartSP},\"level_start_sp\":{entry.LevelStartSP},");
                json.Append($"\"level_end_sp\":{entry.EndSP},\"queue_position\":{i}");
                if (entry.Start.HasValue)
                    json.Append($",\"start_date\":\"{entry.Start:yyyy-MM-ddTHH:mm:ssZ}\"");
                if (entry.Finish.HasValue)
                    json.Append($",\"finish_date\":\"{entry.Finish:yyyy-MM-ddTHH:mm:ssZ}\"");
                json.Append('}');
            }
            json.Append(']');

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json.ToString()));
            return (EsiAPISkillQueue)new DataContractJsonSerializer(typeof(EsiAPISkillQueue)).ReadObject(stream)!;
        }

        /// <summary>The queue arrives, then the skills list.</summary>
        private static Skill QueueThenSkills(EsiAPISkills skills, EsiAPISkillQueue queue)
        {
            CCPCharacter character = PlanTestFixture.CreateTestCharacter();
            character.SkillQueue.Import(queue.CreateSkillQueue());
            character.Import(skills, queue);
            return character.Skills[NavigationId];
        }

        /// <summary>The skills list arrives, then the queue.</summary>
        private static Skill SkillsThenQueue(EsiAPISkills skills, EsiAPISkillQueue queue)
        {
            CCPCharacter character = PlanTestFixture.CreateTestCharacter();
            character.Import(skills, null!);
            character.SkillQueue.Import(queue.CreateSkillQueue());
            return character.Skills[NavigationId];
        }

        public static IEnumerable<object[]> ArrivalOrders()
        {
            yield return new object[] { "queue then skills" };
            yield return new object[] { "skills then queue" };
        }

        private static Skill Merge(string order, EsiAPISkills skills, EsiAPISkillQueue queue)
            => order == "queue then skills" ? QueueThenSkills(skills, queue) : SkillsThenQueue(skills, queue);

        [Theory]
        [MemberData(nameof(ArrivalOrders))]
        public void CompletedQueueEntry_RaisesTheSkillToItsLevel(string order)
        {
            DateTime now = DateTime.UtcNow;
            var queue = Queue(new QueueEntry(4, 20000, Level3SP, Level4SP, now.AddHours(-12), now.AddHours(-1)));

            Skill skill = Merge(order, Skills(level: 3, activeLevel: 3, skillPoints: Level3SP), queue);

            skill.Level.Should().Be(4);
            skill.LastConfirmedLvl.Should().Be(4);
            skill.ActiveLevel.Should().Be(4);
            skill.SkillPoints.Should().Be(Level4SP);
            skill.IsKnown.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(ArrivalOrders))]
        public void TwoCompletedEntries_RaiseTheSkillToTheHigherLevel(string order)
        {
            DateTime now = DateTime.UtcNow;
            var queue = Queue(
                new QueueEntry(4, 20000, Level3SP, Level4SP, now.AddDays(-3), now.AddDays(-2)),
                new QueueEntry(5, Level4SP, Level4SP, Level5SP, now.AddDays(-2), now.AddHours(-1)));

            Skill skill = Merge(order, Skills(level: 3, activeLevel: 3, skillPoints: Level3SP), queue);

            skill.Level.Should().Be(5);
            skill.LastConfirmedLvl.Should().Be(5);
            skill.ActiveLevel.Should().Be(5);
            skill.SkillPoints.Should().Be(Level5SP);
        }

        [Theory]
        [MemberData(nameof(ArrivalOrders))]
        public void CompletedEntryFollowedByATrainingOne_RaisesTheActiveLevelToTheCompletedLevel(string order)
        {
            DateTime now = DateTime.UtcNow;
            var queue = Queue(
                new QueueEntry(4, 20000, Level3SP, Level4SP, now.AddDays(-3), now.AddHours(-2)),
                new QueueEntry(5, Level4SP, Level4SP, Level5SP, now.AddHours(-2), now.AddDays(8)));

            Skill skill = Merge(order, Skills(level: 3, activeLevel: 3, skillPoints: Level3SP), queue);

            skill.Level.Should().Be(4);
            skill.ActiveLevel.Should().Be(4);
            skill.LastConfirmedLvl.Should().Be(4);
            skill.SkillPoints.Should().BeGreaterThanOrEqualTo(Level4SP).And.BeLessThan(Level5SP);
        }

        [Theory]
        [MemberData(nameof(ArrivalOrders))]
        public void TrainingEntry_LeavesTheLevelAndEstimatesThePoints(string order)
        {
            DateTime now = DateTime.UtcNow;
            var queue = Queue(new QueueEntry(4, 20000, Level3SP, Level4SP, now.AddHours(-6), now.AddHours(6)));

            Skill skill = Merge(order, Skills(level: 3, activeLevel: 3, skillPoints: 20000), queue);

            skill.Level.Should().Be(3);
            skill.LastConfirmedLvl.Should().Be(3);
            skill.ActiveLevel.Should().Be(3);
            skill.SkillPoints.Should().BeInRange(20000, Level4SP);
            skill.IsTraining.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(ArrivalOrders))]
        public void PausedEntry_DoesNotLowerPointsTheSkillsListReported(string order)
        {
            var queue = Queue(new QueueEntry(4, 20000, Level3SP, Level4SP, null, null));

            Skill skill = Merge(order, Skills(level: 3, activeLevel: 3, skillPoints: 30000), queue);

            skill.SkillPoints.Should().Be(30000);
            skill.Level.Should().Be(3);
            skill.ActiveLevel.Should().Be(3);
        }

        [Theory]
        [MemberData(nameof(ArrivalOrders))]
        public void PausedEntry_AheadOfTheSkillsList_RaisesThePoints(string order)
        {
            var queue = Queue(new QueueEntry(4, 30000, Level3SP, Level4SP, null, null));

            Skill skill = Merge(order, Skills(level: 3, activeLevel: 3, skillPoints: 20000), queue);

            skill.SkillPoints.Should().Be(30000);
            skill.Level.Should().Be(3);
        }

        [Fact]
        public void PausedEntry_ProjectedDuration_CoversThePointsStillMissing()
        {
            var queue = Queue(new QueueEntry(4, 30000, Level3SP, Level4SP, null, null));
            CCPCharacter character = PlanTestFixture.CreateTestCharacter();
            character.Import(Skills(level: 3, activeLevel: 3, skillPoints: 30000), null!);

            character.SkillQueue.Import(queue.CreateSkillQueue());

            QueuedSkill entry = character.SkillQueue.Single();
            Skill skill = character.Skills[NavigationId];
            TimeSpan expected = skill.GetTimeSpanForPoints(Level4SP - 30000);
            (entry.EndTime - entry.StartTime).Should().BeCloseTo(expected, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [MemberData(nameof(ArrivalOrders))]
        public void QueueEntryBelowTheReportedLevel_ChangesNothing(string order)
        {
            DateTime now = DateTime.UtcNow;
            var queue = Queue(new QueueEntry(3, 1414, 1414, Level3SP, now.AddDays(-3), now.AddDays(-2)));

            Skill skill = Merge(order, Skills(level: 4, activeLevel: 4, skillPoints: Level4SP), queue);

            skill.Level.Should().Be(4);
            skill.LastConfirmedLvl.Should().Be(4);
            skill.ActiveLevel.Should().Be(4);
            skill.SkillPoints.Should().Be(Level4SP);
        }

        [Fact]
        public void SkillsListAlone_IsImportedAsReported()
        {
            CCPCharacter character = PlanTestFixture.CreateTestCharacter();

            character.Import(Skills(level: 3, activeLevel: 2, skillPoints: 9000), null!);

            Skill skill = character.Skills[NavigationId];
            skill.Level.Should().Be(3);
            skill.LastConfirmedLvl.Should().Be(3);
            skill.ActiveLevel.Should().Be(2);
            skill.SkillPoints.Should().Be(9000);
        }
    }
}
