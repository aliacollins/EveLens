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
    /// Guardrail against the localization regression where UI views display an EVE entity's raw
    /// English <c>.Name</c> instead of the translated <c>.LocalizedName</c>. Korean/Chinese users
    /// then see English skill/item/ship names even though translations are loaded.
    ///
    /// The Skills tab used <c>.LocalizedName</c> correctly while the Plan editor (and ~16 other
    /// surfaces) used <c>.Name</c> — inconsistent, and invisible to behavioural tests. This test
    /// scans UI display sites and fails if a known-localizable entity's <c>.Name</c> is shown.
    ///
    /// SCOPE: this guards the high-traffic regression — <c>Skill</c>/<c>Blueprint</c> identifiers'
    /// <c>.Name</c> in display assignments/bindings (what bit the Plan editor). It deliberately does
    /// NOT flag generic <c>item.Name</c>, because display-wrapper types (e.g. ImplantDisplayEntry)
    /// expose a <c>.Name</c> that is already localized, and name-based heuristics can't tell them
    /// apart from a raw <c>Item</c> without false positives. The behavioural regression test
    /// (LocalizationTests) locks in the item/skill VM guarantees end-to-end as the complement.
    ///
    /// Allowed (language-stable) uses of <c>.Name</c> — CSV/EFT export, clipboard, lookups,
    /// equality, persistence, character/group/station names — are excluded by an explicit allowlist
    /// of files and an inline-suppression marker. If a legitimate new display site is flagged,
    /// either switch it to <c>.LocalizedName</c> (preferred) or add a justified suppression.
    /// </summary>
    public class LocalizationArchitectureTests
    {
        private static readonly string ProjectRoot = FindProjectRoot();

        // Variable-name fragments that denote a RAW EVE entity (StaticSkill/Item/Skill wrapper)
        // whose `.Name` is English. We flag `.Name` on these. We deliberately do NOT include
        // generic words like "item"/"implant" that also name display wrappers (e.g.
        // ImplantDisplayEntry) whose `.Name` is already localized — those are not raw entities.
        // If a real raw-entity site uses an unusual variable name, switch it to LocalizedName
        // (preferred) or add the suppression marker.
        private static readonly string[] EntityExprs =
        {
            "Skill", "skill",            // StaticSkill / Skill wrapper (capturedSkill, dep.Skill, ...)
            "ProducesItem", "producesItem",
            "Blueprint", "blueprint",
        };

        // C# display assignments: Text = X.Name / Content = X.Name / Header = X.Name
        private static readonly Regex CsDisplayNameRegex = new(
            @"\b(?:Text|Content|Header)\s*=\s*[^;,}]*?\b(\w+)\.Name\b",
            RegexOptions.Compiled);

        // XAML display bindings: Text="{Binding X.Name}" / Content / Header / etc.
        private static readonly Regex AxamlBindingNameRegex = new(
            @"(?:Text|Content|Header)=""\{Binding\s+[^}]*?\b(\w+)\.Name\}""",
            RegexOptions.Compiled);

        // Files where raw .Name is legitimate (export/persistence/lookup), or where the entity
        // type genuinely has no LocalizedName today (geography). Justify each addition.
        private static readonly HashSet<string> AllowedFiles = new()
        {
            // CSV / EFT / clipboard exports — must stay English for portability / game paste
            "CharacterSkillsView.axaml.cs",   // CSV export uses skill.Name (line ~494)
            // Persistence — GlobalPlanTemplateEntry serializes English skill names
            "GlobalPlanDashboardViewModel.cs",// display row uses LocalizedName; persisted entries keep .Name
        };

        // Inline marker to suppress a specific intentional .Name display (rare).
        private const string SuppressMarker = "// loc-ok";

        [Fact]
        public void NoRawEntityName_InCodeBehindDisplay()
        {
            var roots = new[]
            {
                Path.Combine(ProjectRoot, "src", "EveLens.Avalonia"),
                Path.Combine(ProjectRoot, "src", "EveLens.Common", "ViewModels"),
            };

            var violations = new List<string>();

            foreach (var root in roots.Where(Directory.Exists))
            {
                foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
                {
                    var fileName = Path.GetFileName(file);
                    if (AllowedFiles.Contains(fileName)) continue;
                    if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;

                    var lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        if (line.Contains(SuppressMarker)) continue;

                        var match = CsDisplayNameRegex.Match(line);
                        if (!match.Success) continue;

                        var receiver = match.Groups[1].Value;
                        if (!IsEntityExpr(receiver)) continue;

                        violations.Add($"{Path.GetRelativePath(ProjectRoot, file)}:{i + 1} — " +
                            $"'{receiver}.Name' shown to user (use {receiver}.LocalizedName, or add '{SuppressMarker}' if intentional)");
                    }
                }
            }

            violations.Should().BeEmpty(
                "UI display sites must use .LocalizedName so non-English users see translated names. " +
                "Violations:\n" + string.Join("\n", violations));
        }

        [Fact]
        public void NoRawEntityName_InAxamlBindings()
        {
            var viewsDir = Path.Combine(ProjectRoot, "src", "EveLens.Avalonia", "Views");
            if (!Directory.Exists(viewsDir)) return;

            var violations = new List<string>();

            foreach (var file in Directory.EnumerateFiles(viewsDir, "*.axaml", SearchOption.AllDirectories))
            {
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.Contains(SuppressMarker)) continue;

                    var match = AxamlBindingNameRegex.Match(line);
                    if (!match.Success) continue;

                    var receiver = match.Groups[1].Value;
                    if (!IsEntityExpr(receiver)) continue;

                    violations.Add($"{Path.GetRelativePath(ProjectRoot, file)}:{i + 1} — " +
                        $"binding '{receiver}.Name' (bind {receiver}.LocalizedName instead)");
                }
            }

            violations.Should().BeEmpty(
                "XAML display bindings must bind .LocalizedName for EVE entity names. " +
                "Violations:\n" + string.Join("\n", violations));
        }

        private static bool IsEntityExpr(string receiver)
        {
            // The regex captures the identifier immediately before `.Name`. Match it against
            // entity fragments (whole-word-ish): exact, or ends with the fragment (e.g. "dep"+"Skill"
            // won't match, but "capturedSkill" ends with "Skill"). We check case-sensitively on the
            // known fragments and also a suffix match for compound names like "capturedSkill".
            foreach (var frag in EntityExprs)
            {
                if (receiver == frag) return true;
                // suffix match for compound identifiers: capturedSkill, outputItem, producesItem...
                if (receiver.Length > frag.Length &&
                    receiver.EndsWith(frag, System.StringComparison.Ordinal))
                    return true;
            }
            return false;
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
