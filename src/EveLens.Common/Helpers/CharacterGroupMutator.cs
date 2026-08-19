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
        /// Creates a new group containing <paramref name="members"/>, removing them
        /// from any group they were in. The name is "New Group", numbered when taken.
        /// Returns the created group (already added to <paramref name="groups"/>).
        /// </summary>
        public static CharacterGroupSettings CreateGroup(
            IList<CharacterGroupSettings> groups, IEnumerable<Guid> members)
        {
            var memberList = members.Distinct().ToList();

            string name = "New Group";
            for (int i = 2; groups.Any(g => string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase)); i++)
                name = $"New Group {i}";

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
        /// </summary>
        public static void RemoveFromAllGroups(
            IList<CharacterGroupSettings> groups, Guid character)
        {
            foreach (var group in groups.ToList())
            {
                group.CharacterGuids.Remove(character);
                if (group.CharacterGuids.Count == 0)
                    groups.Remove(group);
            }
        }

        /// <summary>
        /// Renames a group. Fails (false) on a blank name or a name already used by
        /// another group (case-insensitive) — the overview identifies groups by name.
        /// </summary>
        public static bool RenameGroup(
            IList<CharacterGroupSettings> groups, string oldName, string newName)
        {
            newName = newName?.Trim() ?? string.Empty;
            if (newName.Length == 0)
                return false;

            var group = groups.FirstOrDefault(g => string.Equals(g.Name, oldName, StringComparison.Ordinal));
            if (group == null)
                return false;
            if (string.Equals(oldName, newName, StringComparison.Ordinal))
                return true;
            if (groups.Any(g => g != group &&
                    string.Equals(g.Name, newName, StringComparison.OrdinalIgnoreCase)))
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
