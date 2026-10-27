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
    /// One existing group as offered in a character card's "Add to group" submenu.
    /// </summary>
    /// <param name="Name">The group name (groups are identified by name).</param>
    /// <param name="IsCurrent">True when the character is already in this group --
    /// shown checked and disabled rather than as a no-op click.</param>
    public sealed record CharacterGroupMenuEntry(string Name, bool IsCurrent);

    /// <summary>
    /// What a character card's right-click menu offers for grouping: every existing
    /// group in display order (marking the character's own), and whether "Remove
    /// from group" applies. Pure decision logic so the view only builds MenuItems
    /// from it -- the menu is the discoverable path to what drag-to-group does.
    /// </summary>
    public sealed class CharacterGroupMenuModel
    {
        private CharacterGroupMenuModel(IReadOnlyList<CharacterGroupMenuEntry> groups, string? currentGroupName)
        {
            Groups = groups;
            CurrentGroupName = currentGroupName;
        }

        /// <summary>Existing groups in display (settings) order.</summary>
        public IReadOnlyList<CharacterGroupMenuEntry> Groups { get; }

        /// <summary>The first group (display order) holding the character, or null.</summary>
        public string? CurrentGroupName { get; }

        /// <summary>"Remove from group" is offered only for grouped characters.</summary>
        public bool CanRemoveFromGroup => CurrentGroupName != null;

        /// <summary>
        /// Builds the menu model for <paramref name="character"/> against the
        /// configured <paramref name="groups"/>. Groups with a blank name are skipped
        /// (they can't be targeted by name).
        /// </summary>
        public static CharacterGroupMenuModel Build(IEnumerable<CharacterGroupSettings> groups, Guid character)
        {
            var entries = (groups ?? Enumerable.Empty<CharacterGroupSettings>())
                .Where(g => g != null && !string.IsNullOrWhiteSpace(g.Name))
                .Select(g => new CharacterGroupMenuEntry(g.Name, g.CharacterGuids.Contains(character)))
                .ToList();

            string? current = entries.FirstOrDefault(e => e.IsCurrent)?.Name;
            return new CharacterGroupMenuModel(entries, current);
        }
    }
}
