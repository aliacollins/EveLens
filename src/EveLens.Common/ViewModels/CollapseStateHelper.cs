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

        private static string FormatKey(long characterId, string viewName)
            => $"{characterId}_{viewName}";
    }
}
