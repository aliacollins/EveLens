// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Architecture
{
    /// <summary>
    /// Game rules that have a single owner in the model layer must not be worked out
    /// again in the UI project. Each entry names the raw model members the UI may not read.
    /// </summary>
    public class DomainRuleArchitectureTests
    {
        /// <summary>Raw member, and the model type the UI asks instead.</summary>
        private static readonly (string RawMember, string Owner)[] s_ownedRules =
        {
            ("LastReMapTimed", "Character.RemapAvailability"),
            ("AvailableReMaps", "Character.RemapAvailability"),
        };

        [Fact]
        public void UiProject_DoesNotReadRawMembersOfOwnedRules()
        {
            string? uiDir = FindDirectory(Path.Combine("src", "EveLens.Avalonia"));
            if (uiDir == null)
                return;

            var violations = new List<string>();
            foreach (string file in Directory.EnumerateFiles(uiDir, "*.cs", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(uiDir, file);
                if (relative.StartsWith("bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                    || relative.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                    continue;

                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    foreach (var (rawMember, owner) in s_ownedRules)
                    {
                        if (lines[i].Contains("." + rawMember, StringComparison.Ordinal))
                            violations.Add($"{relative}:{i + 1} reads {rawMember}; use {owner}");
                    }
                }
            }

            violations.Should().BeEmpty();
        }

        private static string? FindDirectory(string relativePath)
        {
            string? dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            for (int i = 0; i < 8 && dir != null; i++)
            {
                string candidate = Path.Combine(dir, relativePath);
                if (Directory.Exists(candidate))
                    return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }
    }
}
