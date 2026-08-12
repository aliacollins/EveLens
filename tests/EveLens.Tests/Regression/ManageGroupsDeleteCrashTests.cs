// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Regression
{
    /// <summary>
    /// Regression tests for Issue #78: deleting a character group crashed the Manage Groups
    /// window. The delete (✕) and rename (✎) buttons are nested inside the group chip button;
    /// without <c>e.Handled = true</c> their Click events bubble up to
    /// <c>OnGroupChipClicked</c>, whose expand/collapse toggle re-set <c>_expandedGroup</c> to
    /// the group that was just removed — so <c>BuildReorderPanel</c> then operated on a group
    /// with index -1 and crashed.
    ///
    /// The crash lives in Avalonia event routing, which cannot run inside plain xUnit, so these
    /// tests pin the two protections at source level (same approach as the architecture suite):
    /// nested chip handlers must mark the event handled, and BuildReorderPanel must tolerate an
    /// expanded group that no longer exists.
    /// </summary>
    public class ManageGroupsDeleteCrashTests
    {
        private static string SourcePath => Path.Combine(
            FindProjectRoot(), "src", "EveLens.Avalonia", "Views", "Dialogs", "ManageGroupsWindow.axaml.cs");

        private static string ReadSource()
        {
            File.Exists(SourcePath).Should().BeTrue($"ManageGroupsWindow.axaml.cs should exist at {SourcePath}");
            return File.ReadAllText(SourcePath);
        }

        [Fact]
        public void DeleteButtonHandler_MarksEventHandled()
        {
            // deleteBtn.Click must set e.Handled = true before mutating state, otherwise the
            // click bubbles to OnGroupChipClicked and re-expands the deleted group (Issue #78).
            var source = ReadSource();
            var handler = ExtractHandlerBody(source, "deleteBtn");

            handler.Should().Contain("e.Handled = true",
                "the delete button is nested inside the chip button; without e.Handled the click " +
                "bubbles to OnGroupChipClicked and re-expands the group that was just deleted");
        }

        [Fact]
        public void RenameButtonHandler_MarksEventHandled()
        {
            // Same routing problem: renaming should not also toggle the chip expand state.
            var source = ReadSource();
            var handler = ExtractHandlerBody(source, "renameBtn");

            handler.Should().Contain("e.Handled = true",
                "the rename button is nested inside the chip button; without e.Handled the click " +
                "also toggles the group's expanded state");
        }

        [Fact]
        public void BuildReorderPanel_GuardsAgainstRemovedGroup()
        {
            // Defense in depth: even if some future path leaves _expandedGroup pointing at a
            // group no longer in Settings.CharacterGroups, BuildReorderPanel must collapse the
            // panel instead of indexing with -1.
            var source = ReadSource();

            int start = source.IndexOf("private void BuildReorderPanel()", System.StringComparison.Ordinal);
            start.Should().BeGreaterThan(-1, "BuildReorderPanel should exist in ManageGroupsWindow");
            string body = ExtractMethodBody(source, start);

            body.Should().MatchRegex(@"groupIndex\s*<\s*0",
                "BuildReorderPanel must check IndexOf's result before using it as an index — " +
                "the expanded group may have been deleted (Issue #78)");
        }

        /// <summary>
        /// Extracts the lambda body attached via <c>{buttonName}.Click += ... =&gt; { ... };</c>.
        /// </summary>
        private static string ExtractHandlerBody(string source, string buttonName)
        {
            var match = Regex.Match(source, Regex.Escape(buttonName) + @"\.Click\s*\+=");
            match.Success.Should().BeTrue($"{buttonName}.Click handler should exist in ManageGroupsWindow");

            int braceStart = source.IndexOf('{', match.Index);
            braceStart.Should().BeGreaterThan(-1);
            return ExtractBracedBlock(source, braceStart);
        }

        private static string ExtractMethodBody(string source, int methodStart)
        {
            int braceStart = source.IndexOf('{', methodStart);
            braceStart.Should().BeGreaterThan(-1);
            return ExtractBracedBlock(source, braceStart);
        }

        private static string ExtractBracedBlock(string source, int openBraceIndex)
        {
            int depth = 0;
            for (int i = openBraceIndex; i < source.Length; i++)
            {
                if (source[i] == '{') depth++;
                else if (source[i] == '}' && --depth == 0)
                    return source.Substring(openBraceIndex, i - openBraceIndex + 1);
            }
            return source.Substring(openBraceIndex);
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
