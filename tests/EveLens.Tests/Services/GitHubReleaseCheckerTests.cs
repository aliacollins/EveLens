// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Services;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services
{
    /// <summary>
    /// The mac/linux update path compares versions numerically. The inline code
    /// it replaced compared strings, which orders beta.10 before beta.4 and read
    /// the channel off the numeric file version (which has none) — both bugs
    /// that only detonate later. These tests pin the correct behavior.
    /// </summary>
    public sealed class GitHubReleaseCheckerTests
    {
        [Theory]
        [InlineData("1.5.0-beta.4", 1, 5, 0, "beta", 4)]
        [InlineData("v1.5.0-beta.4", 1, 5, 0, "beta", 4)]
        [InlineData("1.5.0", 1, 5, 0, "stable", 0)]
        [InlineData("1.5.0-alpha.12", 1, 5, 0, "alpha", 12)]
        [InlineData("1.5.0-beta.4+abc123", 1, 5, 0, "beta", 4)] // SourceLink suffix
        public void Parse_ReadsEveryShapeWeStamp(string input, int major, int minor,
            int patch, string channel, int build)
        {
            var v = GitHubReleaseChecker.Parse(input);
            v.Should().NotBeNull();
            (v!.Value.Major, v.Value.Minor, v.Value.Patch, v.Value.Channel, v.Value.Build)
                .Should().Be((major, minor, patch, channel, build));
        }

        [Theory]
        [InlineData("1.5.0.4")]      // numeric file version carries no channel
        [InlineData("")]
        [InlineData("not-a-version")]
        public void Parse_RejectsWhatIsNotAReleaseVersion(string input) =>
            GitHubReleaseChecker.Parse(input).Should().BeNull();

        [Theory]
        [InlineData("1.5.0-beta.4", "1.5.0-beta.10", true)]  // THE string-compare bug
        [InlineData("1.5.0-beta.10", "1.5.0-beta.4", false)]
        [InlineData("1.5.0-beta.4", "1.5.0-beta.4", false)]
        [InlineData("1.5.0-beta.4", "1.5.0", true)]          // stable finishes its betas
        [InlineData("1.5.0", "1.5.0-beta.9", false)]         // never downgrade to a pre
        [InlineData("1.5.0", "1.5.1", true)]
        [InlineData("1.5.0-alpha.3", "1.5.0-beta.1", true)]  // beta outranks alpha
        [InlineData("1.4.9", "1.5.0-beta.1", true)]
        public void IsNewer_ComparesNumbersNotStrings(string current, string candidate,
            bool expected)
        {
            GitHubReleaseChecker.IsNewer(
                    GitHubReleaseChecker.Parse(current)!.Value,
                    GitHubReleaseChecker.Parse(candidate)!.Value)
                .Should().Be(expected, $"{candidate} vs {current}");
        }

        [Theory]
        [InlineData("stable", "stable", true)]
        [InlineData("stable", "beta", false)]
        [InlineData("beta", "beta", true)]
        [InlineData("beta", "stable", true)]   // beta users take the finished release
        [InlineData("beta", "alpha", false)]
        [InlineData("alpha", "alpha", true)]
        [InlineData("alpha", "stable", true)]
        public void ChannelAccepts_OffersOnlyWhatTheChannelShouldSee(string channel,
            string release, bool expected) =>
            GitHubReleaseChecker.ChannelAccepts(channel, release).Should().Be(expected);
    }
}
