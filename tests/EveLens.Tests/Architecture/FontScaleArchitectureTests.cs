// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Architecture
{
    /// <summary>
    /// Ensures no hardcoded font sizes exist in AXAML views or code-behind files.
    /// All font sizes must use DynamicResource (AXAML) or FontScaleService (C#).
    /// </summary>
    public class FontScaleArchitectureTests
    {
        private static readonly string ProjectRoot = FindProjectRoot();
        private static readonly string ViewsDir = Path.Combine(ProjectRoot, "src", "EveLens.Avalonia", "Views");
        private static readonly string ThemesDir = Path.Combine(ProjectRoot, "src", "EveLens.Avalonia", "Themes");

        // Files that are allowed to define raw font size values
        private static readonly HashSet<string> AllowedFiles = new()
        {
            "FontScaleService.cs",  // Defines the base sizes
            "ConstellationCanvas.cs", // SkiaSharp rendering uses pixel sizes directly
        };

        // AXAML pattern: FontSize="<number>"
        private static readonly Regex AxamlFontSizeRegex = new(
            @"FontSize=""(\d+)""",
            RegexOptions.Compiled);

        // C# pattern: FontSize = <number> (with optional comma, semicolon, or whitespace after)
        private static readonly Regex CsFontSizeRegex = new(
            @"FontSize\s*=\s*(\d+)\s*[,;\s}]",
            RegexOptions.Compiled);

        [Fact]
        public void NoHardcodedFontSizes_InAxamlViews()
        {
            if (!Directory.Exists(ViewsDir))
                return; // Skip if running from a different working directory

            var violations = new List<string>();

            foreach (var file in Directory.EnumerateFiles(ViewsDir, "*.axaml", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(file);
                if (AllowedFiles.Contains(fileName)) continue;

                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    var match = AxamlFontSizeRegex.Match(lines[i]);
                    if (match.Success)
                    {
                        violations.Add($"{Path.GetRelativePath(ProjectRoot, file)}:{i + 1} — FontSize=\"{match.Groups[1].Value}\" (use DynamicResource EveFontXxx)");
                    }
                }
            }

            violations.Should().BeEmpty(
                "all AXAML font sizes should use {{DynamicResource EveFontXxx}} instead of hardcoded values. " +
                "Violations:\n" + string.Join("\n", violations));
        }

        [Fact]
        public void NoHardcodedFontSizes_InCodeBehind()
        {
            var avaloniaSrcDir = Path.Combine(ProjectRoot, "src", "EveLens.Avalonia");
            if (!Directory.Exists(avaloniaSrcDir))
                return;

            var violations = new List<string>();

            foreach (var file in Directory.EnumerateFiles(avaloniaSrcDir, "*.cs", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(file);
                if (AllowedFiles.Contains(fileName)) continue;

                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    var match = CsFontSizeRegex.Match(lines[i]);
                    if (match.Success)
                    {
                        violations.Add($"{Path.GetRelativePath(ProjectRoot, file)}:{i + 1} — FontSize = {match.Groups[1].Value} (use FontScaleService.Xxx)");
                    }
                }
            }

            violations.Should().BeEmpty(
                "all C# font sizes should use FontScaleService.Xxx instead of hardcoded values. " +
                "Violations:\n" + string.Join("\n", violations));
        }

        [Fact]
        public void AllPalettes_DefineFontResources()
        {
            var palettesDir = Path.Combine(ThemesDir, "Palettes");
            if (!Directory.Exists(palettesDir))
                return;

            var expectedKeys = new[] { "EveFontTiny", "EveFontCaption", "EveFontSmall",
                "EveFontBody", "EveFontSubheading", "EveFontHeading", "EveFontTitle" };

            foreach (var file in Directory.EnumerateFiles(palettesDir, "*.axaml"))
            {
                var content = File.ReadAllText(file);
                var fileName = Path.GetFileName(file);

                foreach (var key in expectedKeys)
                {
                    content.Should().Contain($"x:Key=\"{key}\"",
                        $"palette {fileName} must define font resource '{key}'");
                }
            }
        }

        private static string FindProjectRoot()
        {
            var dir = Directory.GetCurrentDirectory();
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir, "EveLens.sln")))
                    return dir;
                dir = Directory.GetParent(dir)?.FullName;
            }
            return Directory.GetCurrentDirectory();
        }
    }
}
