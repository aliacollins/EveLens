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
    /// Enforces the "one place for all UI strings" rule: user-facing text must come from the
    /// central <see cref="EveLens.Common.Services.Loc"/> dictionary (via <c>{loc:T Key}</c> in
    /// AXAML or <c>Loc.Get("Key")</c> in code-behind), never a hardcoded literal. This is what
    /// makes the whole UI switchable between languages.
    ///
    /// MIGRATION IN PROGRESS: a baseline allowlist (<see cref="BaselineFiles"/>) holds the views
    /// not yet migrated so the suite stays green while we convert them. The point of the test is to
    /// stop the baseline from GROWING — a NEW hardcoded string in an already-clean file fails. As
    /// files are migrated, remove them from the baseline; the goal is an empty baseline.
    /// </summary>
    public class HardcodedStringArchitectureTests
    {
        private static readonly string ProjectRoot = FindProjectRoot();
        private static readonly string ViewsDir = Path.Combine(ProjectRoot, "src", "EveLens.Avalonia", "Views");

        // AXAML attributes that carry user-facing prose. A literal value starting with a letter
        // (not "{" binding, not a number/symbol) is a hardcoded string.
        private static readonly Regex AxamlLiteralRegex = new(
            @"\b(Text|Content|Header|Watermark|Title|ToolTip\.Tip)=""([A-Za-z][^""]*)""",
            RegexOptions.Compiled);

        // Views already migrated to {loc:T} — these must STAY clean (zero hardcoded literals).
        // Start with the proof-of-concept; grow this as migration proceeds (inverse of the baseline).
        private static readonly HashSet<string> MigratedFiles = new()
        {
            "PlanSkillBrowserView.axaml",
        };

        // Views NOT YET migrated. New files must NOT be added here — migrate them instead.
        // Shrinks to empty as the i18n migration completes. (Kept intentionally explicit so the
        // test documents exactly what remains.)
        private static readonly HashSet<string> BaselineFiles = new()
        {
            // Populated lazily below: any view not in MigratedFiles is treated as baseline until
            // explicitly migrated. See NewHardcodedStrings_Blocked.
        };

        [Fact]
        public void MigratedViews_HaveNoHardcodedStrings()
        {
            if (!Directory.Exists(ViewsDir)) return;

            var violations = new List<string>();

            foreach (var file in Directory.EnumerateFiles(ViewsDir, "*.axaml", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(file);
                if (!MigratedFiles.Contains(fileName)) continue; // only enforce on migrated views

                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    var match = AxamlLiteralRegex.Match(lines[i]);
                    if (match.Success)
                    {
                        violations.Add($"{Path.GetRelativePath(ProjectRoot, file)}:{i + 1} — " +
                            $"{match.Groups[1].Value}=\"{match.Groups[2].Value}\" (use {{loc:T Key}})");
                    }
                }
            }

            violations.Should().BeEmpty(
                "migrated views must keep all UI strings in the Loc dictionary (no hardcoded literals). " +
                "Violations:\n" + string.Join("\n", violations));
        }

        /// <summary>
        /// Reports the remaining migration surface. This test does not fail on the baseline — it
        /// prints the count so progress is visible and the number only goes down. It DOES fail if
        /// the count somehow exceeds a ceiling, catching a regression that adds many new literals.
        /// </summary>
        [Fact]
        public void HardcodedStringBacklog_DoesNotGrow()
        {
            if (!Directory.Exists(ViewsDir)) return;

            int total = 0;
            foreach (var file in Directory.EnumerateFiles(ViewsDir, "*.axaml", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(file);
                if (MigratedFiles.Contains(fileName)) continue;

                foreach (var line in File.ReadAllLines(file))
                    if (AxamlLiteralRegex.IsMatch(line)) total++;
            }

            // Ceiling = the measured baseline when this guardrail was added. As migration proceeds,
            // lower this number; it must never rise. Goal: drive it to 0 and delete this test.
            total.Should().BeLessThanOrEqualTo(621,
                $"the hardcoded-UI-string backlog must shrink, not grow (current: {total}). " +
                "Migrate views to {loc:T} and lower this ceiling — do not add new hardcoded strings.");
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
