// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using EveLens.Common.Models;
using EveLens.Common.Serialization.Esi;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Models
{
    [Collection("StaticData")]
    public class RemapAvailabilityTests
    {
        private static readonly DateTime s_now = new(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void NoCooldown_TimedRemapIsUsable()
        {
            var remaps = new RemapAvailability(0, null);

            remaps.IsTimedAvailableAt(s_now).Should().BeTrue();
            remaps.AvailableAt(s_now).Should().Be(1);
            remaps.CanRemapAt(s_now).Should().BeTrue();
            remaps.DaysUntilTimedRemap(s_now).Should().Be(0);
        }

        [Fact]
        public void CooldownRunning_OnlyBonusRemapsCount()
        {
            var remaps = new RemapAvailability(2, s_now.AddDays(100.5));

            remaps.IsTimedAvailableAt(s_now).Should().BeFalse();
            remaps.AvailableAt(s_now).Should().Be(2);
            remaps.CanRemapAt(s_now).Should().BeTrue();
            remaps.DaysUntilTimedRemap(s_now).Should().Be(100);
        }

        [Fact]
        public void CooldownRunning_WithoutBonusRemaps_NothingToSpend()
        {
            var remaps = new RemapAvailability(0, s_now.AddDays(30));

            remaps.AvailableAt(s_now).Should().Be(0);
            remaps.CanRemapAt(s_now).Should().BeFalse();
            remaps.PlanningBudgetAt(s_now).Should().Be(1);
            remaps.DaysUntilTimedRemap(s_now).Should().Be(30);
        }

        [Fact]
        public void CooldownEndingExactlyNow_TimedRemapIsUsable()
        {
            var remaps = new RemapAvailability(1, s_now);

            remaps.IsTimedAvailableAt(s_now).Should().BeTrue();
            remaps.AvailableAt(s_now).Should().Be(2);
            remaps.PlanningBudgetAt(s_now).Should().Be(2);
        }

        [Fact]
        public void NegativeBonusRemaps_CountAsNone()
        {
            var remaps = new RemapAvailability(-3, null);

            remaps.AvailableAt(s_now).Should().Be(1);
        }

        [Fact]
        public void Character_WithoutAttributeData_HasTheTimedRemapOnly()
        {
            var character = PlanTestFixture.CreateTestCharacter();

            character.RemapAvailability.Should().Be(RemapAvailability.TimedOnly);
        }

        [Fact]
        public void Character_ReportsTheCooldownDateFromEsi()
        {
            var character = PlanTestFixture.CreateTestCharacter();
            DateTime cooldownEnds = new(2027, 3, 3, 8, 30, 0, DateTimeKind.Utc);

            string json = "{\"intelligence\":20,\"perception\":20,\"charisma\":19,\"willpower\":20,"
                + "\"memory\":20,\"bonus_remaps\":2,"
                + $"\"accrued_remap_cooldown_date\":\"{cooldownEnds:yyyy-MM-ddTHH:mm:ssZ}\"}}";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var attributes = (EsiAPIAttributes)new DataContractJsonSerializer(typeof(EsiAPIAttributes))
                .ReadObject(stream)!;

            character.Import(attributes);

            character.RemapAvailability.BonusRemaps.Should().Be(2);
            character.RemapAvailability.NextTimedRemap.Should().Be(cooldownEnds);
            character.RemapAvailability.IsTimedAvailableAt(cooldownEnds.AddSeconds(-1)).Should().BeFalse();
            character.RemapAvailability.IsTimedAvailableAt(cooldownEnds).Should().BeTrue();
        }
    }
}
