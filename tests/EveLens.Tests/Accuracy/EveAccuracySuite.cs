// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Linq;
using EveLens.Common.Data;
using EveLens.Common.Enumerations;
using EveLens.Common.Extensions;
using EveLens.Common.Helpers;
using EveLens.Common.Models;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Accuracy
{
    /// <summary>
    /// ═══════════════════════════════ EVE ACCURACY SUITE ═══════════════════════════════
    ///
    /// "EveLens numbers can be trusted" is the #1 product promise. Every test here is a
    /// GOLDEN SCENARIO: the expected value is a known EVE Online outcome (game formulas
    /// published by CCP, verifiable in the client), not a value observed from our own code.
    /// If one of these fails, EveLens is showing players wrong numbers — treat as P0.
    ///
    /// Run just this suite:
    ///   dotnet test --filter "Suite=EveAccuracy"
    ///
    /// Covered: SP-per-level thresholds, SP/hour from attributes, training durations,
    /// Alpha/Omega rates, implant effects, remap math, prerequisites, cumulative queue
    /// time. (Temporary boosters: out of scope for now.)
    /// ═══════════════════════════════════════════════════════════════════════════════════
    /// </summary>
    [Collection("StaticData")]
    [Trait("Suite", "EveAccuracy")]
    public class EveAccuracySuite
    {
        public EveAccuracySuite()
        {
            PlanTestFixture.EnsureStaticSkillsLoaded();
        }

        // ─────────────────────────── SP per level (CCP formula) ───────────────────────────
        // SP(level) = 250 × rank × sqrt(32)^(level-1), i.e. rank 1: 250/1414/8000/45255/256000

        [Theory]
        [InlineData(1, 250)]
        [InlineData(2, 1_415)]     // EveLens uses CCP's actual table: 1415 for rank 1 (±1 on lv2 is the known rounding)
        [InlineData(3, 8_000)]
        [InlineData(4, 45_255)]
        [InlineData(5, 256_000)]
        public void SpPerLevel_Rank1_MatchesGameTable(int level, long expectedSp)
        {
            // Navigation is rank 1 — the canonical reference skill
            var skill = PlanTestFixture.GetSkill("Navigation");
            skill.Rank.Should().Be(1, "Navigation has been rank 1 since 2003");

            long sp = skill.GetPointsRequiredForLevel(level);
            sp.Should().BeCloseTo(expectedSp, 1,
                "rank-1 SP thresholds are fixed game constants (±1 known lv2 rounding)");
        }

        [Theory]
        [InlineData("Spaceship Command", 1)]   // rank 1
        [InlineData("Gunnery", 1)]
        [InlineData("Capacitor Management", 3)]
        public void SkillRanks_MatchGame(string skillName, int expectedRank)
        {
            PlanTestFixture.GetSkill(skillName).Rank.Should().Be(expectedRank,
                $"{skillName}'s rank is fixed game data");
        }

        [Fact]
        public void SpForLevel5_ScalesLinearlyWithRank()
        {
            // rank R at level 5 = R × 256,000 — exact in-game rule
            var capManagement = PlanTestFixture.GetSkill("Capacitor Management"); // rank 3
            capManagement.GetPointsRequiredForLevel(5).Should().Be(
                capManagement.Rank * 256_000L);
        }

        // ─────────────────────────── SP/hour from attributes ───────────────────────────
        // SP/min = primary + secondary/2  →  SP/hour = primary×60 + secondary×30

        [Theory]
        [InlineData(17, 17, 1_530)]  // fresh character, no remap: 17×60 + 17×30
        [InlineData(27, 21, 2_250)]  // full remap into primary: 27×60 + 21×30
        [InlineData(20, 20, 1_800)]
        public void SpPerHour_MatchesAttributeFormula(int primary, int secondary, int expected)
        {
            var character = CreateOmegaCharacter();
            var scratchpad = new CharacterScratchpad(character);
            var skill = PlanTestFixture.GetSkill("Navigation"); // Int primary, Per secondary

            scratchpad.Intelligence.Base = primary;
            scratchpad.Perception.Base = secondary;

            scratchpad.GetBaseSPPerHour(skill).Should().Be(expected,
                "SP/hour = primary×60 + secondary×30 (CCP formula)");
        }

        // ─────────────────────────── Training duration ───────────────────────────

        [Fact]
        public void TrainingDuration_Navigation1_FreshCharacter()
        {
            // 250 SP at 1530 SP/hour = 0.16339869h = 9m 48.2s (game-verifiable)
            var character = CreateOmegaCharacter();
            var scratchpad = new CharacterScratchpad(character);
            var skill = PlanTestFixture.GetSkill("Navigation");

            scratchpad.Intelligence.Base = 17;
            scratchpad.Perception.Base = 17;
            scratchpad.Train(skill, 1);

            scratchpad.TrainingTime.Should().BeCloseTo(
                TimeSpan.FromHours(250.0 / 1530.0), TimeSpan.FromSeconds(1),
                "250 SP at 1,530 SP/hour");
        }

        [Fact]
        public void TrainingDuration_IsExactlyProportionalToSp()
        {
            // Level 5 of a rank-1 skill (256,000 SP) must take exactly 1024× level 1 (250 SP)
            var character = PlanTestFixture.CreateTestCharacter();
            var skill = PlanTestFixture.GetSkill("Navigation");

            var lv1 = new CharacterScratchpad(character);
            lv1.Train(skill, 1);
            var lv5 = new CharacterScratchpad(character);
            lv5.Train(skill, 5);

            double ratio = lv5.TrainingTime.TotalSeconds / lv1.TrainingTime.TotalSeconds;
            ratio.Should().BeApproximately(256_000.0 / 250.0, 0.01,
                "training time is strictly SP ÷ SP/hour — no hidden factors");
        }

        // ─────────────────────────── Alpha / Omega rates ───────────────────────────

        [Fact]
        public void AlphaClone_TrainsAtExactlyHalfSpeed()
        {
            AccountStatus.Alpha.GetTrainingRate().Should().Be(0.5f,
                "CCP's alpha clone rate is exactly 50%");
            AccountStatus.Omega.GetTrainingRate().Should().Be(1.0f);
        }

        // ─────────────────────────── Implants ───────────────────────────

        [Fact]
        public void ImplantBonus_AddsDirectlyToEffectiveAttribute()
        {
            // A +4 implant adds exactly 4 to the effective attribute → SP/hour shifts by 4×60
            var character = CreateOmegaCharacter();
            var scratchpad = new CharacterScratchpad(character);
            var skill = PlanTestFixture.GetSkill("Navigation");

            scratchpad.Intelligence.Base = 17;
            scratchpad.Perception.Base = 17;
            float before = scratchpad.GetBaseSPPerHour(skill);

            scratchpad.Intelligence.ImplantBonus = 4;
            float after = scratchpad.GetBaseSPPerHour(skill);

            (after - before).Should().Be(4 * 60,
                "a +4 primary implant adds exactly 240 SP/hour");
        }

        // ─────────────────────────── Remap invariants ───────────────────────────

        [Fact]
        public void Remap_AttributeBudget_MatchesGameRules()
        {
            // EVE remap: each attribute 17..27, total across five = 99 (17×5 + 14 spare)
            Common.Constants.EveConstants.CharacterBaseAttributePoints.Should().Be(17);
            Common.Constants.EveConstants.SpareAttributePointsOnRemap.Should().Be(14);
            Common.Constants.EveConstants.MaxRemappablePointsPerAttribute.Should().Be(10);
            Common.Constants.EveConstants.MaxBaseAttributePoints.Should().Be(27);
        }

        [Fact]
        public void Remap_AppliedMidPlan_ChangesOnlySubsequentTraining()
        {
            var character = PlanTestFixture.CreateTestCharacter();
            var plan = PlanTestFixture.CreateTestPlan(character);
            var navigation = PlanTestFixture.GetSkill("Navigation");   // INT/PER
            plan.PlanTo(navigation, 3);

            // Same plan trained twice: once flat, once with a full-INT remap first.
            var flat = new CharacterScratchpad(character);
            flat.TrainEntries(plan.ToArray(), applyRemappingPoints: false);

            var entries = plan.ToArray();
            var remap = new RemappingPoint();
            // INT 27 (17+10), PER 21 (17+4): legal spread favoring Navigation's attributes
            typeof(RemappingPoint).GetMethod("SetAttributes",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(remap, new object[] { 27, 21, 17, 17, 17 });
            entries[0].Remapping = remap;

            var remapped = new CharacterScratchpad(character);
            remapped.TrainEntries(entries, applyRemappingPoints: true);

            remapped.TrainingTime.Should().BeLessThan(flat.TrainingTime,
                "a remap into the skill's attributes must strictly reduce training time");

            // Exact ratio check: 1530 SP/h → 27×60+21×30 = 2250 SP/h
            double ratio = flat.TrainingTime.TotalSeconds / remapped.TrainingTime.TotalSeconds;
            ratio.Should().BeApproximately(2250.0 / 1530.0, 0.01,
                "duration scales inversely with SP/hour");
        }

        // ─────────────────────────── Prerequisites (SDE ground truth) ───────────────────────────

        [Theory]
        [InlineData("Capital Jump Portal Generation", "Jump Portal Generation", 3)] // Issue #99
        [InlineData("Cloaking", "CPU Management", 4)]
        public void Prerequisites_MatchCurrentSde(string skillName, string prereqName, int prereqLevel)
        {
            var skill = StaticSkills.GetSkillByName(skillName);
            skill.Should().NotBeNull();

            var prereq = skill!.Prerequisites.FirstOrDefault(p => p.Skill?.Name == prereqName);
            prereq.Should().NotBeNull($"{skillName} requires {prereqName} in game");
            prereq!.Level.Should().Be(prereqLevel,
                $"{skillName} requires {prereqName} {prereqLevel} in the current game build");
        }

        // ─────────────────────────── Queue / plan cumulative math ───────────────────────────

        [Fact]
        public void PlanTotal_EqualsSumOfPerEntryTimes_WhenNoRemaps()
        {
            var character = CreateOmegaCharacter();
            var plan = PlanTestFixture.CreateTestPlan(character);
            plan.PlanTo(PlanTestFixture.GetSkill("Navigation"), 3);
            plan.PlanTo(PlanTestFixture.GetSkill("Spaceship Command"), 2);

            var scratchpad = new CharacterScratchpad(character);
            TimeSpan total = plan.GetTotalTime(scratchpad, applyRemappingPoints: false);

            var manual = new CharacterScratchpad(character);
            TimeSpan sum = TimeSpan.Zero;
            foreach (var entry in plan)
            {
                TimeSpan before = manual.TrainingTime;
                manual.Train(entry.Skill, entry.Level);
                sum += manual.TrainingTime - before;
            }

            total.Should().BeCloseTo(sum, TimeSpan.FromSeconds(1),
                "the plan total must be the exact sum of its parts — no double counting");
        }

        [Fact]
        public void UnknownCloneState_TrainsAtAlphaSpeed_FailSafe()
        {
            // Deliberate EveLens policy: when clone state can't be determined, assume the
            // SLOWER alpha rate so plans never promise times the player can't achieve.
            AccountStatus.Unknown.GetTrainingRate().Should().Be(0.5f,
                "unknown status must fail safe to the pessimistic alpha rate");
        }

        [Fact]
        public void PlanSp_CountsEachLevelWindowOnce()
        {
            // Navigation 1→5 plans exactly 256,000 SP for a fresh character (rank 1).
            // Per-entry SP is computed by UpdateStatistics — the editor always runs it,
            // so the golden scenario exercises the same path users see.
            var character = PlanTestFixture.CreateTestCharacter();
            var plan = PlanTestFixture.CreateTestPlan(character);
            plan.PlanTo(PlanTestFixture.GetSkill("Navigation"), 5);
            plan.UpdateStatistics();

            plan.TotalSkillPoints.Should().Be(256_000L,
                "SP to level 5 of a rank-1 skill is a fixed game constant");
        }

        /// <summary>
        /// Golden scenarios assume Omega (1.0× rate) unless testing clone states — a fresh
        /// test character has Unknown status, which EveLens fail-safes to alpha speed.
        /// </summary>
        private static CCPCharacter CreateOmegaCharacter()
        {
            var character = PlanTestFixture.CreateTestCharacter();
            character.AccountStatusSettings = AccountStatusMode.Omega;
            return character;
        }
    }
}
