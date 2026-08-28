// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Text.Json.Serialization;

namespace EveLens.Common.Serialization.Skinr
{
    /// <summary>
    /// One published render-runtime release, as served by
    /// <c>https://hub.evelens.dev/runtime/latest.json</c>. The runtime is a separate,
    /// proprietary add-on — EveLens downloads it on first SKINR use, verifies it
    /// (zip hash here, then the signed per-file manifest inside), and never bundles it.
    /// </summary>
    public sealed class SkinrRuntimeRelease
    {
        /// <summary>Release version, e.g. <c>1.0.0</c>.</summary>
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        /// <summary>Absolute download URL of the runtime zip.</summary>
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        /// <summary>SHA-256 of the zip, lowercase hex — checked before extraction.</summary>
        [JsonPropertyName("zipSha256")]
        public string? ZipSha256 { get; set; }

        /// <summary>Zip size in bytes, for download progress.</summary>
        [JsonPropertyName("sizeBytes")]
        public long SizeBytes { get; set; }

        /// <summary>
        /// The sidecar protocol generation this release speaks. EveLens refuses a
        /// release whose protocol it does not implement, with words instead of a hang.
        /// </summary>
        [JsonPropertyName("protocolVersion")]
        public int ProtocolVersion { get; set; }
    }
}
