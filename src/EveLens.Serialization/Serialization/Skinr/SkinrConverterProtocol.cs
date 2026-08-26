// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Text.Json.Serialization;

namespace EveLens.Common.Serialization.Skinr
{
    /// <summary>
    /// The single JSON object <c>convert.mjs</c> writes to stdout, success or failure.
    /// </summary>
    /// <remarks>
    /// Law 13 territory, same as <see cref="SkinrSidecarResponse"/>: a wire contract between two
    /// processes in two languages with nothing in the compiler tying them together.
    ///
    /// The converter's own exit code already says whether it worked (0 success, 2 usage, 3
    /// rejected by preflight, 4 parse failure, 5 non-deterministic), so this type exists for
    /// what the exit code cannot carry: <see cref="Path"/>, which the renderer needs, and
    /// <see cref="CacheHit"/> plus <see cref="DurationMs"/>, which are how we know whether the
    /// content-addressed cache is doing its job instead of silently reconverting every load.
    ///
    /// <see cref="Sha256"/> is the output digest, not the input's. Conversion is required to be
    /// deterministic — <c>--selftest</c> converts twice and demands byte equality — so the same
    /// hull always produces the same digest, and a change in it means the converter or the
    /// resource library changed under us.
    /// </remarks>
    public sealed class SkinrConverterResult
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        /// <summary>True when the content-addressed cache answered without reconverting.</summary>
        [JsonPropertyName("cacheHit")]
        public bool CacheHit { get; set; }

        /// <summary>The cache key: input digest plus converter and library versions.</summary>
        [JsonPropertyName("key")]
        public string? Key { get; set; }

        /// <summary>Where the <c>.cmf</c> was written. What the renderer is handed.</summary>
        [JsonPropertyName("path")]
        public string? Path { get; set; }

        /// <summary>The copy inside the shared cache, which may differ from <see cref="Path"/>.</summary>
        [JsonPropertyName("cachePath")]
        public string? CachePath { get; set; }

        [JsonPropertyName("bytes")]
        public long Bytes { get; set; }

        /// <summary>Digest of the produced <c>.cmf</c>, not of the source <c>.gr2</c>.</summary>
        [JsonPropertyName("sha256")]
        public string? Sha256 { get; set; }

        [JsonPropertyName("converterVersion")]
        public string? ConverterVersion { get; set; }

        [JsonPropertyName("durationMs")]
        public double DurationMs { get; set; }

        /// <summary>Present only on failure.</summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        /// <summary>
        /// Mesh and vertex counts from the parse. Diagnostic: a hull that suddenly reports a
        /// different mesh count is a converter regression, not a CCP art change.
        /// </summary>
        [JsonPropertyName("stats")]
        public SkinrConverterStats? Stats { get; set; }
    }

    /// <summary>What the converter saw inside the geometry.</summary>
    public sealed class SkinrConverterStats
    {
        [JsonPropertyName("meshes")]
        public int Meshes { get; set; }

        [JsonPropertyName("vertices")]
        public long Vertices { get; set; }

        [JsonPropertyName("indices")]
        public long Indices { get; set; }
    }
}
