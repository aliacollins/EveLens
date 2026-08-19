// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Linq;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.Helpers;
using EveLens.Common.Models;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Helpers
{
    /// <summary>
    /// Character ordering within overview groups (Issue #72 rework / Discussion #46).
    /// </summary>
    public class OverviewSortHelperTests
    {
        private static Character Char(long id, string name) =>
            new CCPCharacter(new CharacterIdentity(id, name), new NullCharacterServices());

        [Fact]
        public void Custom_PreservesInputOrder()
        {
            var chars = new[] { Char(1, "Zulu"), Char(2, "Alpha"), Char(3, "Mike") };

            var sorted = OverviewSortHelper.Sort(chars, OverviewSortMode.Custom);

            sorted.Select(c => c.Name).Should().Equal("Zulu", "Alpha", "Mike");
        }

        [Fact]
        public void Name_SortsAlphabetically_CaseInsensitive()
        {
            var chars = new[] { Char(1, "zulu"), Char(2, "Alpha"), Char(3, "mike") };

            var sorted = OverviewSortHelper.Sort(chars, OverviewSortMode.Name);

            sorted.Select(c => c.Name).Should().Equal("Alpha", "mike", "zulu");
        }

        [Fact]
        public void SkillPoints_TiesBreakByName()
        {
            // Fresh test characters all have 0 SP — the deterministic name tiebreak
            // is what keeps equal-SP characters from shuffling between refreshes.
            var chars = new[] { Char(1, "Zulu"), Char(2, "Alpha") };

            var sorted = OverviewSortHelper.Sort(chars, OverviewSortMode.SkillPoints);

            sorted.Select(c => c.Name).Should().Equal("Alpha", "Zulu");
        }

        [Fact]
        public void TrainingUrgency_PutsIdleCharactersFirst_ByName()
        {
            // Test characters are never training, so all are "idle" — the contract
            // is that idle characters lead and order deterministically by name.
            var chars = new[] { Char(1, "Zulu"), Char(2, "Alpha") };

            var sorted = OverviewSortHelper.Sort(chars, OverviewSortMode.TrainingUrgency);

            sorted.Select(c => c.Name).Should().Equal("Alpha", "Zulu");
        }
    }
}
