// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Extensions;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Extensions
{
    /// <summary>
    /// Tests for <see cref="StringExtensions.RedactUserName"/>, the scrub applied to
    /// every log line that enters EveLens from a third-party library.
    /// </summary>
    /// <remarks>
    /// These exist because Velopack logs absolute paths — which embed the OS account
    /// name — and those lines flow into the trace log (which users paste into GitHub
    /// issues), the TCP diagnostic stream, and the update failure dialog. If any of
    /// these assertions break, an OS username is being sent off the user's machine.
    /// </remarks>
    public class RedactUserNameTests
    {
        [Theory]
        [InlineData(@"C:\Users\alia\AppData\Local\EveLens\packages\EveLens-1.5.0-full.nupkg",
                    @"C:\Users\[REDACTED]\AppData\Local\EveLens\packages\EveLens-1.5.0-full.nupkg")]
        [InlineData(@"D:\Users\alia\Downloads", @"D:\Users\[REDACTED]\Downloads")]
        public void RedactsWindowsHomePaths(string input, string expected)
        {
            input.RedactUserName().Should().Be(expected);
        }

        [Fact]
        public void RedactsWindowsPathEndingAtTheUsername()
        {
            // The DiagnosticReportBuilder regex requires a trailing backslash and
            // misses this shape; the extension must not.
            @"Access to 'C:\Users\alia' was denied".RedactUserName()
                .Should().Be(@"Access to 'C:\Users\[REDACTED]' was denied");
        }

        [Theory]
        [InlineData("/Users/alia/Library/Caches/velopack/EveLens/packages",
                    "/Users/[REDACTED]/Library/Caches/velopack/EveLens/packages")]
        [InlineData("Replacing bundle at /Users/alia/Downloads/EveLens.app",
                    "Replacing bundle at /Users/[REDACTED]/Downloads/EveLens.app")]
        [InlineData("/home/alia/.local/share/EveLens", "/home/[REDACTED]/.local/share/EveLens")]
        public void RedactsUnixHomePaths(string input, string expected)
        {
            // macOS is where the in-place updater runs — this is the path shape
            // that the old Windows-only redaction let straight through.
            input.RedactUserName().Should().Be(expected);
        }

        [Fact]
        public void RedactsPathsInsideQuotes()
        {
            "could not rename \"/Users/alia/Applications/EveLens.app\"".RedactUserName()
                .Should().Be("could not rename \"/Users/[REDACTED]/Applications/EveLens.app\"");
        }

        [Fact]
        public void RedactsTheCurrentOsUserNameOutsideAnyPath()
        {
            string userName = Environment.UserName;
            // Short usernames are deliberately left alone (see next test) —
            // nothing to assert against on such a machine.
            if (userName.Length < 3)
                return;

            $"elevation requested for {userName}".RedactUserName()
                .Should().NotContain(userName).And.Contain("[REDACTED]");
        }

        [Fact]
        public void LeavesOrdinaryTextAlone()
        {
            const string text = "EveLens 1.5.0 downloaded update from " +
                "https://github.com/aliacollins/evelens (200 OK)";
            // The current OS username could legitimately occur inside ordinary
            // words; only assert no damage when it does not.
            if (text.Contains(Environment.UserName))
                return;

            text.RedactUserName().Should().Be(text);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void PassesThroughNullAndEmpty(string? input)
        {
            input!.RedactUserName().Should().Be(input);
        }
    }
}
