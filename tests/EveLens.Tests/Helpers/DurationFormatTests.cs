// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using EveLens.Common.Enumerations;
using EveLens.Common.Helpers;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Helpers
{
    public class DurationFormatTests
    {
        private static TimeSpan Span(int days, int hours, int minutes, int seconds)
            => new(days, hours, minutes, seconds);

        [Theory]
        [InlineData(3, 14, 25, 9, "3d 14h")]
        [InlineData(1, 0, 0, 0, "1d 0h")]
        [InlineData(0, 23, 59, 59, "23h 59m")]
        [InlineData(0, 1, 0, 0, "1h 0m")]
        [InlineData(0, 0, 59, 59, "59m")]
        [InlineData(0, 0, 0, 30, "0m")]
        [InlineData(0, 0, 0, 0, "0m")]
        public void Compact(int d, int h, int m, int s, string expected)
            => TimeFormatHelper.Format(Span(d, h, m, s), DurationStyle.Compact).Should().Be(expected);

        [Theory]
        [InlineData(3, 14, 25, 9, "3d 14h")]
        [InlineData(0, 5, 42, 7, "5h 42m")]
        [InlineData(0, 0, 23, 45, "23m 45s")]
        [InlineData(0, 0, 0, 0, "0m 0s")]
        public void CompactSeconds(int d, int h, int m, int s, string expected)
            => TimeFormatHelper.Format(Span(d, h, m, s), DurationStyle.CompactSeconds).Should().Be(expected);

        [Theory]
        [InlineData(3, 14, 25, 9, "3d 14h 25m")]
        [InlineData(1, 0, 0, 0, "1d 0h 0m")]
        [InlineData(0, 5, 42, 7, "5h 42m")]
        [InlineData(0, 0, 23, 45, "23m")]
        public void Full(int d, int h, int m, int s, string expected)
            => TimeFormatHelper.Format(Span(d, h, m, s), DurationStyle.Full).Should().Be(expected);

        [Theory]
        [InlineData(3, 14, 25, 9, "3d 14h 25m")]
        [InlineData(0, 5, 42, 7, "5h 42m")]
        [InlineData(0, 0, 23, 45, "23m 45s")]
        public void FullSeconds(int d, int h, int m, int s, string expected)
            => TimeFormatHelper.Format(Span(d, h, m, s), DurationStyle.FullSeconds).Should().Be(expected);

        [Theory]
        [InlineData(0)]
        [InlineData(-90)]
        public void ElapsedText_ReplacesZeroAndNegativeDurations(int seconds)
        {
            TimeFormatHelper.Format(TimeSpan.FromSeconds(seconds), DurationStyle.Compact, "Expired")
                .Should().Be("Expired");
        }

        [Fact]
        public void ElapsedText_IsNotUsedForPositiveDurations()
        {
            TimeFormatHelper.Format(TimeSpan.FromSeconds(1), DurationStyle.CompactSeconds, "Done")
                .Should().Be("0m 1s");
        }

        [Fact]
        public void SourceFiles_DoNotBuildDayHourTextThemselves()
        {
            string? src = FindDirectory("src");
            if (src == null)
                return;

            var pattern = new Regex(@"\(int\)[\w.]+\.TotalDays\}d \{[\w.]+\.Hours\}h");
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "TimeFormatHelper.cs",
                "BasePlan.cs",
                "BlueprintBrowserViewModel.cs",
            };
            var violations = new List<string>();
            foreach (string file in Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(src, file);
                string[] parts = relative.Split(Path.DirectorySeparatorChar);
                if (parts.Contains("bin") || parts.Contains("obj") || allowed.Contains(Path.GetFileName(file)))
                    continue;

                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (pattern.IsMatch(lines[i]))
                        violations.Add($"{relative}:{i + 1}");
                }
            }

            violations.Should().BeEmpty("durations are formatted by TimeFormatHelper.Format");
        }

        private static string? FindDirectory(string relativePath)
        {
            string? dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            for (int i = 0; i < 8 && dir != null; i++)
            {
                string candidate = Path.Combine(dir, relativePath);
                if (Directory.Exists(Path.Combine(candidate, "EveLens.Common")))
                    return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }
    }
}
