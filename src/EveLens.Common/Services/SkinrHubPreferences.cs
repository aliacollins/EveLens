// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EveLens.Common.Services
{
    /// <summary>
    /// The Hub's remembered choices — currently one: whether the app may fetch
    /// ready-made previews from evelens.dev. Null means never asked, which is what
    /// makes the one-time consent banner appear; the choice is revisitable in the
    /// render-settings flyout. Transparency law: the app never contacts a non-CCP
    /// server without an explicit, remembered, revisitable yes.
    /// </summary>
    public sealed class SkinrHubPreferences
    {
        [JsonPropertyName("communityPreviews")]
        public bool? CommunityPreviews { get; set; }

        private static string DefaultPath => Path.Combine(
            AppServices.ApplicationPaths.DataDirectory, "cache", "skinr",
            "hub-prefs.json");

        public static SkinrHubPreferences Load(string? path = null)
        {
            try
            {
                string file = path ?? DefaultPath;
                if (File.Exists(file))
                {
                    var loaded = JsonSerializer.Deserialize<SkinrHubPreferences>(
                        File.ReadAllText(file));
                    if (loaded != null)
                        return loaded;
                }
            }
            catch (Exception)
            {
                // A corrupt prefs file re-asks one question; never worth failing over.
            }
            return new SkinrHubPreferences();
        }

        public void Save(string? path = null)
        {
            try
            {
                string file = path ?? DefaultPath;
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                File.WriteAllText(file, JsonSerializer.Serialize(this));
            }
            catch (Exception)
            {
                // Losing the answer means asking again next session — acceptable.
            }
        }
    }
}
