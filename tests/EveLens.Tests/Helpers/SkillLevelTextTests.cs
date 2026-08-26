// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Helpers;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Helpers
{
    /// <summary>
    /// The shared skill-level token parser: the game copies roman numerals, players
    /// type digits, and both import paths (plan editor clipboard, Doctrine Designer)
    /// must agree (#137 follow-up: "Amarr Titan V" was rejected).
    /// </summary>
    public class SkillLevelTextTests
    {
        [Theory]
        [InlineData("1", 1)]
        [InlineData("5", 5)]
        [InlineData("I", 1)]
        [InlineData("III", 3)]
        [InlineData("V", 5)]
        [InlineData("iv", 4)]
        [InlineData(" V ", 5)]
        public void TryParse_AcceptsDigitsAndRomans(string token, int expected)
        {
            SkillLevelText.TryParse(token, out int level).Should().BeTrue();
            level.Should().Be(expected);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("6")]
        [InlineData("VI")]
        [InlineData("X")]
        [InlineData("Titan")]
        [InlineData("")]
        [InlineData(null)]
        public void TryParse_RejectsEverythingElse(string token)
        {
            SkillLevelText.TryParse(token, out _).Should().BeFalse();
        }
    }
}
