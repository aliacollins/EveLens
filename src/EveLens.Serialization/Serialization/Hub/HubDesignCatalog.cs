// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EveLens.Common.Serialization.Hub
{
    /// <summary>
    /// The pre-resolved Paragon Hub catalog served at
    /// <c>https://hub.evelens.dev/designs.json</c>: one entry per market design with
    /// its identity (name, hull, class, faction, creator, tier) already resolved by
    /// the hub's collector. One GET replaces the per-client ESI recipe walk that
    /// used to identify thousands of designs one request at a time.
    /// </summary>
    /// <remarks>
    /// Parsed with System.Text.Json (the file is cached to disk verbatim and
    /// re-parsed, so the DTO and the disk format are the same thing). Every field
    /// is optional-tolerant: a hub running older collector output must degrade to
    /// "less identity", never to a parse failure.
    /// </remarks>
    public sealed class HubDesignCatalog
    {
        [JsonPropertyName("generated")]
        public string Generated { get; set; } = string.Empty;

        [JsonPropertyName("designs")]
        public List<HubDesignInfo> Designs { get; set; } = new();
    }

    /// <summary>One market design's identity, as the hub's collector resolved it.</summary>
    public sealed class HubDesignInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("hull")]
        public string Hull { get; set; } = string.Empty;

        [JsonPropertyName("klass")]
        public string Klass { get; set; } = string.Empty;

        [JsonPropertyName("faction")]
        public string Faction { get; set; } = string.Empty;

        [JsonPropertyName("creator")]
        public string Creator { get; set; } = string.Empty;

        [JsonPropertyName("tier")]
        public int Tier { get; set; }

        [JsonPropertyName("plex")]
        public long Plex { get; set; }

        [JsonPropertyName("listings")]
        public int Listings { get; set; }
    }
}
