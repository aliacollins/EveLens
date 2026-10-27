// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.SettingsObjects;

namespace EveLens.Common.Helpers
{
    /// <summary>
    /// Group mutations behind the overview's direct-manipulation gestures (drop a
    /// card on a card to form a group, drop on a folder to join it — Issue #72
    /// rework). Pure list operations on the settings model, testable without UI.
    /// A character belongs to at most one group: joining one leaves the others.
    /// </summary>
    public static class CharacterGroupMutator
    {
        /// <summary>
        /// Fallback base name when the caller supplies none (or a blank one).
        /// </summary>
        public const string DefaultGroupName = "New Group";

        /// <summary>
        /// The overview's section key for characters in no group. Reserved: a real group
        /// with this name would be mistaken for that section.
        /// </summary>
        public const string UngroupedSection = "Ungrouped";

        /// <summary>
        /// Why <paramref name="name"/> can't name a group (a <c>Loc</c> key), or null when it can.
        /// <paramref name="except"/> is the group being renamed, which may keep its own name.
        /// </summary>
        public static string? GroupNameProblem(
            IEnumerable<CharacterGroupSettings> groups, string? name, CharacterGroupSettings? except = null)
        {
            name = name?.Trim() ?? string.Empty;
            if (name.Length == 0)
                return "Overview.GroupNameRequired";
            if (string.Equals(name, UngroupedSection, StringComparison.OrdinalIgnoreCase)
                || groups.Any(g => g != except && string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase)))
                return "Overview.GroupNameTaken";
            return null;
        }

        /// <summary>
        /// Creates a new group containing <paramref name="members"/>, removing them
        /// from any group they were in. The name is <paramref name="baseName"/>
        /// (the caller passes the localized "New Group"; blank falls back to
        /// <see cref="DefaultGroupName"/>), numbered when taken.
        /// Returns the created group (already added to <paramref name="groups"/>).
        /// </summary>
        public static CharacterGroupSettings CreateGroup(
            IList<CharacterGroupSettings> groups, IEnumerable<Guid> members, string? baseName = null)
        {
            var memberList = members.Distinct().ToList();

            string root = string.IsNullOrWhiteSpace(baseName) ? DefaultGroupName : baseName.Trim();
            string name = root;
            for (int i = 2; groups.Any(g => string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase)); i++)
                name = $"{root} {i}";

            foreach (var guid in memberList)
                RemoveFromAllGroups(groups, guid);

            var group = new CharacterGroupSettings { Name = name };
            foreach (var guid in memberList)
                group.CharacterGuids.Add(guid);
            groups.Add(group);
            return group;
        }

        /// <summary>
        /// Moves a character into an existing group (leaving any other group).
        /// Returns false when the group doesn't exist or already holds the character.
        /// </summary>
        public static bool AddToGroup(
            IList<CharacterGroupSettings> groups, string groupName, Guid character)
        {
            var target = groups.FirstOrDefault(
                g => string.Equals(g.Name, groupName, StringComparison.Ordinal));
            if (target == null || target.CharacterGuids.Contains(character))
                return false;

            RemoveFromAllGroups(groups, character);
            target.CharacterGuids.Add(character);
            return true;
        }

        /// <summary>
        /// Removes a character from every group (it becomes ungrouped). Groups left
        /// empty are deleted — an empty folder has no reason to exist.
        /// Returns true when the character was in at least one group.
        /// </summary>
        public static bool RemoveFromAllGroups(
            IList<CharacterGroupSettings> groups, Guid character)
        {
            bool removed = false;
            foreach (var group in groups.ToList())
            {
                removed |= group.CharacterGuids.Remove(character);
                if (group.CharacterGuids.Count == 0)
                    groups.Remove(group);
            }
            return removed;
        }

        /// <summary>
        /// Renames a group. Fails (false) on a blank, reserved or already-used name
        /// (case-insensitive; see <see cref="GroupNameProblem"/>) — the overview identifies groups by name.
        /// </summary>
        public static bool RenameGroup(
            IList<CharacterGroupSettings> groups, string oldName, string newName)
        {
            newName = newName?.Trim() ?? string.Empty;
            var group = groups.FirstOrDefault(g => string.Equals(g.Name, oldName, StringComparison.Ordinal));
            if (group == null)
                return false;
            if (string.Equals(oldName, newName, StringComparison.Ordinal))
                return true;
            if (GroupNameProblem(groups, newName, except: group) != null)
                return false;

            group.Name = newName;
            return true;
        }

        /// <summary>
        /// Moves a group left (-1) or right (+1) in display order. No-ops at the ends.
        /// </summary>
        public static bool MoveGroup(
            IList<CharacterGroupSettings> groups, string name, int delta)
        {
            var group = groups.FirstOrDefault(g => string.Equals(g.Name, name, StringComparison.Ordinal));
            if (group == null) return false;

            int from = groups.IndexOf(group);
            int to = from + delta;
            if (to < 0 || to >= groups.Count || to == from)
                return false;

            groups.RemoveAt(from);
            groups.Insert(to, group);
            return true;
        }

        /// <summary>
        /// Deletes a group; its members simply become ungrouped.
        /// </summary>
        public static bool DeleteGroup(IList<CharacterGroupSettings> groups, string name)
        {
            var group = groups.FirstOrDefault(g => string.Equals(g.Name, name, StringComparison.Ordinal));
            return group != null && groups.Remove(group);
        }
    }
}
