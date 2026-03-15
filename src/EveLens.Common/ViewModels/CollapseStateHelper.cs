// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Events;
using EveLens.Common.Services;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// Contract for flat group display objects that support expand/collapse persistence.
    /// Implemented by group display classes in Avalonia views (Notifications, Journal, Mail, Clones).
    /// </summary>
    public interface ICollapsibleGroup
    {
        /// <summary>Group key used for persistence (must be stable across refreshes).</summary>
        string Name { get; }

        /// <summary>Whether the group is currently expanded.</summary>
        bool IsExpanded { get; set; }
    }

    /// <summary>
    /// Reads and writes per-character, per-view expand/collapse state
    /// from <see cref="Settings.UI"/> collapse states dictionary.
    /// </summary>
    public static class CollapseStateHelper
    {
        /// <summary>
        /// Returns true if state has been explicitly saved for this character+view.
        /// Distinguishes "never saved" from "saved as all-collapsed (empty set)".
        /// </summary>
        public static bool HasSavedState(long characterId, string viewName)
        {
            var key = FormatKey(characterId, viewName);
            return Settings.UI.CollapseStates.ContainsKey(key);
        }

        /// <summary>
        /// Loads the persisted expand state for a character and view.
        /// Returns an empty set if no state was previously saved.
        /// Use <see cref="HasSavedState"/> to distinguish "never saved" from "saved as empty".
        /// </summary>
        public static HashSet<string> LoadExpandState(long characterId, string viewName)
        {
            var key = FormatKey(characterId, viewName);
            if (Settings.UI.CollapseStates.TryGetValue(key, out var list))
                return new HashSet<string>(list, StringComparer.Ordinal);
            return new HashSet<string>(StringComparer.Ordinal);
        }

        /// <summary>
        /// Saves the expand state for a character and view.
        /// Uses debounced save via SettingsChangedEvent (no synchronous disk I/O).
        /// </summary>
        public static void SaveExpandState(long characterId, string viewName, HashSet<string> expandedKeys)
        {
            var key = FormatKey(characterId, viewName);
            Settings.UI.CollapseStates[key] = expandedKeys.ToList();
            // In-memory only — no event, no disk I/O, zero lag.
            // Persisted to disk on next normal settings save cycle.
        }

        /// <summary>
        /// Removes all persisted expand state for a character (e.g., on character delete).
        /// </summary>
        public static void RemoveCharacterState(long characterId)
        {
            var prefix = $"{characterId}_";
            var keysToRemove = Settings.UI.CollapseStates.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
                .ToList();

            foreach (var key in keysToRemove)
                Settings.UI.CollapseStates.Remove(key);

            if (keysToRemove.Count > 0)
                AppServices.EventAggregator?.Publish(SettingsChangedEvent.Instance);
        }

        // --- Flat group helpers (Notifications, Journal, Mail, Clones) ---

        /// <summary>
        /// Initializes <see cref="ICollapsibleGroup.IsExpanded"/> on each group from persisted state.
        /// First visit (no saved state): all groups default to expanded.
        /// Subsequent visits: only groups in the saved set are expanded.
        /// </summary>
        public static void InitializeGroups(long characterId, string viewName, IEnumerable<ICollapsibleGroup> groups)
        {
            var state = LoadExpandState(characterId, viewName);
            bool hasSaved = HasSavedState(characterId, viewName);
            foreach (var group in groups)
                group.IsExpanded = !hasSaved || state.Contains(group.Name);
        }

        /// <summary>
        /// Saves the current expand state of all groups to settings.
        /// </summary>
        public static void SaveGroups(long characterId, string viewName, IEnumerable<ICollapsibleGroup> groups)
        {
            var expanded = new HashSet<string>(StringComparer.Ordinal);
            foreach (var group in groups)
            {
                if (group.IsExpanded)
                    expanded.Add(group.Name);
            }
            SaveExpandState(characterId, viewName, expanded);
        }

        private static string FormatKey(long characterId, string viewName)
            => $"{characterId}_{viewName}";
    }
}
