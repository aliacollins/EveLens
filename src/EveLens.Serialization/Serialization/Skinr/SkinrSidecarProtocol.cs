// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EveLens.Common.Serialization.Skinr
{
    /// <summary>
    /// The JSON-lines contract between EveLens and the Trinity render sidecar.
    /// </summary>
    /// <remarks>
    /// Law 13 applies to this as much as it does to the ESI DTOs: it is a wire format between
    /// two processes that ship together but are written in different languages, and nothing in
    /// the compiler connects the two ends. A renamed field here does not break the build, it
    /// makes the sidecar silently fall back to a default — which for
    /// <see cref="SkinrSidecarRequest.MaterialIndex">materialIndex</see> means painting the
    /// hull's primary colour instead of the pattern, i.e. a render that looks deliberate.
    ///
    /// One request per line on stdin, one response per line on stdout, correlated by
    /// <see cref="SkinrSidecarRequest.Id"/>. Lines carrying <see cref="SkinrSidecarResponse.Event"/>
    /// are unsolicited and have no id — the sidecar emits <c>ready</c> once at startup and
    /// progress events during long builds. stderr carries structured diagnostics, never
    /// protocol.
    ///
    /// The sidecar reads its request as a flat dictionary, so this is deliberately one wide
    /// request type rather than a class per op: the shape mirrors what the other end actually
    /// does, and a per-op hierarchy would imply a strictness the protocol does not have.
    /// Every op-specific field is nullable and omitted when null, so an op only sends what it
    /// means.
    /// </remarks>
    public sealed class SkinrSidecarRequest
    {
        /// <summary>Monotonic correlation id. The response echoes it.</summary>
        [JsonPropertyName("id")]
        public long Id { get; set; }

        /// <summary>
        /// One of <c>ping</c>, <c>capabilities</c>, <c>scene</c>, <c>quality</c>,
        /// <c>resolve</c>, <c>build</c>, <c>camera</c>, <c>render</c>, <c>clear</c>,
        /// <c>shutdown</c>.
        /// </summary>
        [JsonPropertyName("op")]
        public string Op { get; set; } = string.Empty;

        // --- resolve ---------------------------------------------------------

        /// <summary>SOF hull name, e.g. <c>mf4_t1</c>. Not the ship type id.</summary>
        [JsonPropertyName("hull")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Hull { get; set; }

        [JsonPropertyName("faction")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Faction { get; set; }

        [JsonPropertyName("race")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Race { get; set; }

        /// <summary>
        /// Material tokens to check against the live SOF library. The <c>none</c> sentinel is
        /// skipped by the sidecar rather than reported missing.
        /// </summary>
        [JsonPropertyName("materials")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<string>? Materials { get; set; }

        /// <summary>Pattern names to check — SOF pattern names, not SKINR <c>.dds</c> tokens.</summary>
        [JsonPropertyName("patterns")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<string>? Patterns { get; set; }

        // --- geometry-map ----------------------------------------------------

        /// <summary>
        /// Additional <c>.gr2</c> → <c>.cmf</c> mappings for the <c>geometry-map</c> op, merged
        /// into the sidecar's boot map. Sent after a build reports
        /// <see cref="SkinrSidecarResponse.ShipGeometry"/> unmapped paths.
        /// </summary>
        [JsonPropertyName("entries")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string>? GeometryEntries { get; set; }

        // --- build -----------------------------------------------------------

        /// <summary>The SpaceObjectFactory DNA string. Required by <c>build</c>.</summary>
        [JsonPropertyName("dna")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Dna { get; set; }

        /// <summary>
        /// Where the converted <c>.cmf</c> geometry lives in the res-override tree. CCP
        /// publishes hull geometry as Granny <c>.gr2</c>, which Trinity cannot read, so the
        /// host converts it and repoints the mesh here.
        /// </summary>
        [JsonPropertyName("geometryResPath")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? GeometryResPath { get; set; }

        /// <summary>Mask textures to bind, by shader slot name.</summary>
        [JsonPropertyName("patternTextures")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<SkinrSidecarTexture>? PatternTextures { get; set; }

        /// <summary>The pattern transforms, one per layer.</summary>
        [JsonPropertyName("masks")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<SkinrSidecarMask>? Masks { get; set; }

        /// <summary>
        /// ESI's <c>pattern_blend_mode</c>, verbatim. The sidecar selects the matching
        /// <c>BLEND_MODE_*</c> shader permutation on every hull-area effect — the per-pixel
        /// compositing (exclusion's XOR, the nested clips) is compiled into CCP's shader
        /// binaries and cannot be reproduced by any mask or material remap on this side.
        /// </summary>
        [JsonPropertyName("patternBlendMode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PatternBlendMode { get; set; }

        /// <summary>Darkhull painting order, or null for hulls where the tech slot is
        /// irrelevant. The sidecar resolves whether the hull actually has Darkhull areas.</summary>
        [JsonPropertyName("darkhull")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SkinrSidecarDarkhull? Darkhull { get; set; }

        /// <summary>Frame the camera on the hull's bounding sphere. Defaults true.</summary>
        [JsonPropertyName("autoFrame")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? AutoFrame { get; set; }

        // --- camera ----------------------------------------------------------

        [JsonPropertyName("yaw")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Yaw { get; set; }

        [JsonPropertyName("pitch")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Pitch { get; set; }

        [JsonPropertyName("distance")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Distance { get; set; }

        [JsonPropertyName("fov")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Fov { get; set; }

        /// <summary>
        /// POV camera position (the Garage balcony). Setting it switches the sidecar
        /// from orbit to look-from; an EMPTY array explicitly clears it back to orbit
        /// (null means "don't touch", per the sticky-camera contract).
        /// </summary>
        [JsonPropertyName("eye")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<double>? Eye { get; set; }

        /// <summary>Look-at target, world units. Null means "don't touch".</summary>
        [JsonPropertyName("at")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<double>? At { get; set; }

        // --- resize ----------------------------------------------------------

        /// <summary>
        /// Requested output width for the <c>resize</c> op, in pixels. Omitted means "keep".
        /// </summary>
        /// <remarks>
        /// <para>These belong on a request rather than only on a launch argument because the render
        /// size is <em>not</em> fixed at device creation, which is what the code here assumed for
        /// most of this feature's life. Trinity's driver reads its destination target's dimensions
        /// every frame and pulls every internal buffer out of a size-keyed pool, so changing the
        /// target is enough — no new device, no cold boot. The sidecar's <c>resize</c> op does the
        /// four fix-ups that are ours (target, depth-stencil, render job, scratch bitmap) and
        /// re-runs the warm-up frames, because TAA's history is at the old size.</para>
        /// </remarks>
        [JsonPropertyName("width")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Width { get; set; }

        /// <inheritdoc cref="Width"/>
        [JsonPropertyName("height")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Height { get; set; }

        /// <summary>
        /// Supersample factor for the <c>resize</c> op, 1-4. Omitted means "keep". Costs the
        /// square, and the sidecar reduces it before it reduces the resolution: a user who asked
        /// for 1080p wants 1080p, and 1080p without supersampling beats 810p with it.
        /// </summary>
        [JsonPropertyName("supersample")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Supersample { get; set; }

        // --- render ----------------------------------------------------------

        /// <summary>
        /// Wait for TAA to converge before capturing. Defaults true; set false for the
        /// low-latency frames emitted while the user is dragging the camera.
        /// </summary>
        [JsonPropertyName("settle")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Settle { get; set; }

        /// <summary>Frames to render when <see cref="Settle"/> is false.</summary>
        [JsonPropertyName("frames")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Frames { get; set; }

        /// <summary>Write a PNG here. For exports; the live view uses <see cref="Raw"/>.</summary>
        [JsonPropertyName("png")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Png { get; set; }

        /// <summary>
        /// Write raw B8G8R8A8 pixels here. The sidecar writes <c>&lt;path&gt;.part</c> and
        /// renames, so the host never maps a half-written frame.
        /// </summary>
        [JsonPropertyName("raw")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Raw { get; set; }

        /// <summary>
        /// Base name of a shared-memory mapping to write the frame into instead of the
        /// <see cref="Raw"/> file. The sidecar appends <c>_{width}x{height}</c> and returns
        /// the full name in the response; the raw file is skipped when the mapping succeeds
        /// and written as a fallback when it does not. The animation hot path: one memcpy
        /// instead of a 5&#160;MB filesystem round-trip per frame.
        /// </summary>
        [JsonPropertyName("shm")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Shm { get; set; }

        /// <summary>
        /// Set <c>false</c> to skip the SHA-256 frame digest (~10&#160;ms over a 5&#160;MB
        /// frame). Animation frames don't dedup, so they don't pay for it.
        /// </summary>
        [JsonPropertyName("digest")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Digest { get; set; }

        /// <summary>
        /// Reject the frame if mean luma falls below this. The sidecar's black-frame guard:
        /// a black render writes a valid PNG and hashes stably, so brightness is the only
        /// thing that catches it.
        /// </summary>
        [JsonPropertyName("minLuma")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? MinLuma { get; set; }

        // --- scene -----------------------------------------------------------

        /// <summary>
        /// Backdrop mode for the <c>scene</c> op: <c>room</c> (CCP's studio), <c>dome</c>,
        /// <c>nebula</c>, <c>studio</c> or <c>transparent</c>. The sidecar ignores names it
        /// does not know and reports the mode that actually took.
        /// </summary>
        [JsonPropertyName("backdrop")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Backdrop { get; set; }

        /// <summary>
        /// Sun colour override for the <c>scene</c> op, RGBA in linear light. Null keeps
        /// the scene's authored value.
        /// </summary>
        [JsonPropertyName("sunColor")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<double>? SunColor { get; set; }

        /// <summary>
        /// Sun direction override for the <c>scene</c> op — XYZ, the direction the light
        /// travels. Null keeps the scene's authored top light.
        /// </summary>
        [JsonPropertyName("sunDirection")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<double>? SunDirection { get; set; }
    }

    /// <summary>A texture to attach to every mesh-area effect, by shader slot name.</summary>
    public sealed class SkinrSidecarTexture
    {
        /// <summary><c>PatternMask1Map</c> or <c>PatternMask2Map</c>.</summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>A <c>res:/</c> path. The sidecar preloads it before binding.</summary>
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;
    }

    /// <summary>
    /// One pattern layer's placement — a field-for-field match for <c>EveCustomMask</c>, and
    /// therefore for ESI's pattern configuration.
    /// </summary>
    /// <remarks>
    /// The sidecar does not re-derive any of this. In particular it does not map projection
    /// types to <see cref="ClampU"/>/<see cref="ClampV"/>: the resolver owns that, because
    /// CCP collapses three projection types onto a bool with an equality test against
    /// <c>TA_CLAMP</c>, so <c>clamp-to-border</c> is false exactly like <c>repeat</c>.
    /// </remarks>
    public sealed class SkinrSidecarMask
    {
        /// <summary>0 for the primary pattern, 1 for the secondary. Positional fallback only.</summary>
        [JsonPropertyName("layer")]
        public int Layer { get; set; }

        /// <summary>
        /// 4 for layer 0, 5 for layer 1 — <c>SOURCE_PATTERN1</c> and <c>SOURCE_PATTERN2</c>.
        /// This is how the sidecar matches a spec to the mask the engine already created, so
        /// it is the field that decides whether we redirect a mask or append a fighting one.
        /// </summary>
        [JsonPropertyName("materialIndex")]
        public int MaterialIndex { get; set; }

        /// <summary>x, y, z.</summary>
        [JsonPropertyName("position")]
        public IReadOnlyList<double> Position { get; set; } = new double[3];

        /// <summary>x, y, z.</summary>
        [JsonPropertyName("scaling")]
        public IReadOnlyList<double> Scaling { get; set; } = new double[] { 1, 1, 1 };

        /// <summary>x, y, z, w.</summary>
        [JsonPropertyName("rotation")]
        public IReadOnlyList<double> Rotation { get; set; } = new double[] { 0, 0, 0, 1 };

        /// <summary>
        /// Which of the four hull materials the pattern paints over, in DNA material order —
        /// the faction remap is already applied host-side.
        /// </summary>
        [JsonPropertyName("targetMaterials")]
        public IReadOnlyList<double> TargetMaterials { get; set; } = new double[4];

        [JsonPropertyName("isMirrored")]
        public bool IsMirrored { get; set; }

        [JsonPropertyName("clampU")]
        public bool ClampU { get; set; }

        [JsonPropertyName("clampV")]
        public bool ClampV { get; set; }

        /// <summary>
        /// Texture address mode for this layer's <c>PatternMask{N}MapSampler</c> override:
        /// clamp-to-edge 4, repeat 1, clamp-to-border 3 — CCP's own value map from the
        /// decompiled studio. This is what makes a stripe extend past its projection box.
        /// </summary>
        [JsonPropertyName("samplerU")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? SamplerU { get; set; }

        /// <inheritdoc cref="SamplerU"/>
        [JsonPropertyName("samplerV")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? SamplerV { get; set; }
    }

    /// <summary>
    /// The Darkhull painting order: which constant-index texture replaces the areas'
    /// <c>MaterialMap</c>, and which coating's constants get copied over all four material
    /// slots. Mirrors <c>cosmeticsManager.SetMaterialOnDarkhull</c> — without it every
    /// Triglavian hull renders its body black regardless of the design.
    /// </summary>
    public sealed class SkinrSidecarDarkhull
    {
        /// <summary><c>black/darkGray/lightGray/white.dds</c> by the tech slot's remapped
        /// material position — the map selects which Mtl index the area reads.</summary>
        [JsonPropertyName("materialMap")]
        public string MaterialMap { get; set; } = string.Empty;

        /// <summary>The tech coating's <c>.red</c>, or null to only swap the map.</summary>
        [JsonPropertyName("material")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Material { get; set; }
    }

    /// <summary>
    /// One response line. Union of every op's payload, because the sidecar returns a flat
    /// dictionary and a discriminated hierarchy would have to be reconstructed from
    /// <see cref="Op"/> anyway.
    /// </summary>
    public sealed class SkinrSidecarResponse
    {
        /// <summary>Echoes the request id. Null on unsolicited <see cref="Event"/> lines.</summary>
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        /// <summary>
        /// Set on unsolicited lines: <c>ready</c> at startup, progress during long builds.
        /// A line with this set is never a response to anything and must not consume a
        /// pending request.
        /// </summary>
        [JsonPropertyName("event")]
        public string? Event { get; set; }

        [JsonPropertyName("ok")]
        public bool? Ok { get; set; }

        [JsonPropertyName("op")]
        public string? Op { get; set; }

        /// <summary>Set when <see cref="Ok"/> is false. Already includes the named culprit
        /// for DNA validation failures.</summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        /// <summary>
        /// Which stage of the sidecar's life a <c>fatal</c> event came from, e.g. <c>boot</c>.
        /// Only meaningful alongside <see cref="Event"/>.
        /// </summary>
        [JsonPropertyName("phase")]
        public string? Phase { get; set; }

        /// <summary>Milliseconds the current operation has been running. On heartbeats.</summary>
        [JsonPropertyName("ms")]
        public long? ElapsedMilliseconds { get; set; }

        /// <summary>
        /// Resources the engine has downloaded so far this session, on a <c>working</c>
        /// heartbeat. Cumulative across the process, not per operation.
        /// </summary>
        /// <remarks>
        /// These two carry more weight than their size suggests: they are the only evidence
        /// available to distinguish "downloading a hull's textures on a cold cache" from
        /// "deadlocked inside a native call". The transport's inactivity deadline depends on the
        /// heartbeat arriving at all; the UI's honesty depends on these numbers moving.
        /// </remarks>
        [JsonPropertyName("files")]
        public long? FilesDownloaded { get; set; }

        /// <summary>Bytes downloaded so far this session. See <see cref="FilesDownloaded"/>.</summary>
        [JsonPropertyName("bytes")]
        public long? BytesDownloaded { get; set; }

        /// <summary>
        /// Resources the engine still has queued — loads plus prepares — on a heartbeat emitted
        /// from inside a resource wait.
        /// </summary>
        /// <remarks>
        /// This is the engine's own definition of "not done yet", read off
        /// <c>resMan.pendingLoads</c> and <c>resMan.pendingPrepares</c>. It replaces a
        /// <c>resMan.Wait()</c> call that told the host nothing at all, and it is what lets the UI
        /// say "42 resources remaining" instead of only counting bytes — a cold hull fetch spends
        /// most of its time on many small files, where a byte counter barely moves and a file
        /// count moves constantly.
        /// </remarks>
        [JsonPropertyName("pending")]
        public int? PendingResources { get; set; }

        /// <summary>
        /// How long <see cref="PendingResources"/> has sat at the same value, in milliseconds.
        /// Zero while the queue is draining.
        /// </summary>
        /// <remarks>
        /// The distinction that matters during a slow fetch: a queue of 40 that is falling is
        /// healthy however long it takes, and a queue of 40 that has not moved in two minutes is
        /// a stalled transfer. Elapsed time alone cannot tell those apart, which is why the
        /// sidecar tracks the stall rather than leaving the host to guess from timestamps.
        /// </remarks>
        [JsonPropertyName("stalledMs")]
        public long? StalledMilliseconds { get; set; }

        /// <summary>
        /// How long the sidecar expects to be unable to say anything at all, in milliseconds.
        /// Applies to the next silence only.
        /// </summary>
        /// <remarks>
        /// <para>The honest admission in this protocol. Heartbeats work while the engine is
        /// pumping, and not at all while it is inside a Blue binding that holds the interpreter
        /// lock — <c>BuildFromDNA</c> spends its shader compilation there, during which no Python
        /// code runs on any thread in the process and no liveness signal exists to be sent.</para>
        ///
        /// <para>So the sidecar declares it up front instead of leaving the host to infer a wedge
        /// from silence it caused itself. The host raises its inactivity budget for exactly one
        /// read and then drops back, which keeps the loose deadline scoped to the one call that
        /// needs it. The number is the sidecar's to compute because it depends on the device: WARP
        /// compiles the same shaders an order of magnitude slower than a GPU does, and the host has
        /// no way to know which it got.</para>
        /// </remarks>
        [JsonPropertyName("quietMs")]
        public long? QuietBudgetMilliseconds { get; set; }

        // --- ready / capabilities -------------------------------------------

        /// <summary>Wire version. The host refuses to drive a sidecar it does not know.</summary>
        [JsonPropertyName("protocol")]
        public int? Protocol { get; set; }

        [JsonPropertyName("sidecar")]
        public string? Sidecar { get; set; }

        [JsonPropertyName("ops")]
        public IReadOnlyList<string>? Ops { get; set; }

        /// <summary>The SOF pattern the sidecar expects in <c>pattern?</c>.</summary>
        [JsonPropertyName("carrierPattern")]
        public string? CarrierPattern { get; set; }

        [JsonPropertyName("device")]
        public string? Device { get; set; }

        // --- ping ------------------------------------------------------------

        [JsonPropertyName("builds")]
        public int? Builds { get; set; }

        // --- resolve ---------------------------------------------------------

        [JsonPropertyName("hullKnown")]
        public bool? HullKnown { get; set; }

        [JsonPropertyName("factionKnown")]
        public bool? FactionKnown { get; set; }

        [JsonPropertyName("raceKnown")]
        public bool? RaceKnown { get; set; }

        /// <summary>
        /// The hull's Granny geometry, as the SOF library names it. The host must convert this
        /// to <c>.cmf</c> and pass the result back as
        /// <see cref="SkinrSidecarRequest.GeometryResPath"/>.
        /// </summary>
        [JsonPropertyName("geometryResFilePath")]
        public string? GeometryResFilePath { get; set; }

        /// <summary>x, y, z, radius.</summary>
        [JsonPropertyName("boundingSphere")]
        public IReadOnlyList<double>? BoundingSphere { get; set; }

        [JsonPropertyName("isSkinned")]
        public bool? IsSkinned { get; set; }

        /// <summary>
        /// The faction's <c>materialUsageMtl1..4</c>. Diagnostic only: the engine applies this
        /// remap itself when the DNA is built, so a host that applied it too would apply it
        /// twice.
        /// </summary>
        [JsonPropertyName("materialUsage")]
        public IReadOnlyList<int>? MaterialUsage { get; set; }

        /// <summary>
        /// Tokens the live SOF library does not have. The SDE and the client's
        /// <c>data.black</c> are separate release trains and can disagree.
        /// </summary>
        [JsonPropertyName("missing")]
        public IReadOnlyList<string>? Missing { get; set; }

        /// <summary>
        /// Whether the carrier pattern is legal on this hull. It covers about 309 of the SOF
        /// library's hulls, so a design on anything else cannot carry <c>pattern?</c> and the
        /// UI should say so before a build fails.
        /// </summary>
        [JsonPropertyName("carrierSupportsHull")]
        public bool? CarrierSupportsHull { get; set; }

        [JsonPropertyName("dnaValid")]
        public bool? DnaValid { get; set; }

        [JsonPropertyName("dnaError")]
        public string? DnaError { get; set; }

        // --- build -----------------------------------------------------------

        [JsonPropertyName("radius")]
        public double? Radius { get; set; }

        [JsonPropertyName("masks")]
        public SkinrSidecarMaskReport? Masks { get; set; }

        [JsonPropertyName("patternTextures")]
        public int? PatternTextureCount { get; set; }

        [JsonPropertyName("textureBinding")]
        public SkinrSidecarTextureBinding? TextureBinding { get; set; }

        [JsonPropertyName("unskinnedEffects")]
        public int? UnskinnedEffects { get; set; }

        /// <summary>
        /// What happened to every mesh under the built ship, not just the hull.
        /// <see cref="SkinrSidecarGeometryReport.Unmapped"/> is the actionable field.
        /// </summary>
        [JsonPropertyName("shipGeometry")]
        public SkinrSidecarGeometryReport? ShipGeometry { get; set; }

        /// <summary>
        /// The studio environment CCP's SKINR scene draws behind the ship — what attached, and
        /// crucially whether its geometry can draw.
        /// </summary>
        /// <remarks>
        /// This is per-session, not per-build: the room attaches once when the scene boots. It is
        /// surfaced on the build result anyway because its <c>geometry.unmapped</c> list is the
        /// only place the host can learn that the backdrop is missing files, and it cannot be
        /// known before boot. The room's <c>cylinder_01a_ds</c> primitive is CCP's
        /// BackgroundGradient — the grey gradient behind the ship in the game's own studio — so an
        /// unmapped entry here is a black viewport, not a missing detail.
        /// </remarks>
        [JsonPropertyName("lightEnv")]
        public SkinrSidecarLightEnvReport? LightEnv { get; set; }

        /// <summary>
        /// What the <c>geometry-map</c> op's re-repoint did to the already-attached studio room.
        /// </summary>
        [JsonPropertyName("lightEnvGeometry")]
        public SkinrSidecarGeometryReport? LightEnvGeometry { get; set; }

        /// <summary>
        /// The hangar bay's geometry accounting, from <c>build</c> and <c>geometry-map</c>.
        /// The bay attaches hidden at boot; its unmapped <c>.gr2</c> paths convert in the
        /// same pass as the ship's own fx meshes, so the Hangar preset is warm by the
        /// time anyone clicks it.
        /// </summary>
        [JsonPropertyName("hangarGeometry")]
        public SkinrSidecarGeometryReport? HangarGeometry { get; set; }

        /// <summary>The hull's SOF category — <c>frigate</c>, <c>battleship</c>, and so on.</summary>
        [JsonPropertyName("category")]
        public string? Category { get; set; }

        /// <summary>
        /// The faction's DNA-position-to-shader-material remap, reported for diagnostics only.
        /// The engine applies it itself when the DNA is built, so the host must not.
        /// </summary>
        [JsonPropertyName("defaultPatternMaterials")]
        public IReadOnlyList<string>? DefaultPatternMaterials { get; set; }

        [JsonPropertyName("resPathInsert")]
        public string? ResPathInsert { get; set; }

        // --- render ----------------------------------------------------------

        /// <summary>
        /// The captured frame's width. With supersampling this is the resolved size, which is
        /// not necessarily the requested output size — see <see cref="DownsampleRequired"/>.
        /// </summary>
        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }

        /// <summary>Render-target size before any supersample resolve.</summary>
        [JsonPropertyName("renderWidth")]
        public int? RenderWidth { get; set; }

        /// <inheritdoc cref="RenderWidth"/>
        [JsonPropertyName("renderHeight")]
        public int? RenderHeight { get; set; }

        /// <summary>Size the sidecar was configured to produce.</summary>
        [JsonPropertyName("outputWidth")]
        public int? OutputWidth { get; set; }

        /// <inheritdoc cref="OutputWidth"/>
        [JsonPropertyName("outputHeight")]
        public int? OutputHeight { get; set; }

        [JsonPropertyName("supersample")]
        public int? Supersample { get; set; }

        /// <summary>
        /// Whether a <c>resize</c> actually changed anything. False is not an error: the host
        /// resizes on every window drag, and it needs to tell "already there" from "applied" to
        /// decide whether to spend a render. Without it a debounce cannot be written safely.
        /// </summary>
        [JsonPropertyName("changed")]
        public bool? Changed { get; set; }

        /// <summary>
        /// The largest render size, in pixels after supersampling, the running sidecar will honour.
        /// Reported by <c>capabilities</c> so the picker can grey a resolution out rather than
        /// offer it and quietly hand back something smaller.
        /// </summary>
        [JsonPropertyName("maxRenderPixels")]
        public long? MaxRenderPixels { get; set; }

        /// <summary>How much of the supersample the engine resolved itself, 1 meaning none.</summary>
        [JsonPropertyName("resolvedInEngine")]
        public int? ResolvedInEngine { get; set; }

        /// <summary>
        /// True when the frame still needs scaling down to the requested output size. The host
        /// does that, because it already has to blit into a bitmap.
        /// </summary>
        [JsonPropertyName("downsampleRequired")]
        public bool? DownsampleRequired { get; set; }

        /// <summary>Whether the settle loop reached a stable image before the frame was taken.</summary>
        /// <remarks>
        /// A verdict, and only a verdict. It was briefly both this and the report below under one
        /// name, which cost a run: the sidecar sent an object, this side declared a bool, and a
        /// build that had entirely succeeded came back as an unreadable line while the host waited
        /// out its deadline on a reply that had already arrived. Protocol version 5 split them.
        /// </remarks>
        [JsonPropertyName("settled")]
        public bool? Settled { get; set; }

        /// <summary>Why <see cref="Settled"/> says what it says. Absent when settling was skipped.</summary>
        [JsonPropertyName("settle")]
        public SkinrSidecarSettleReport? Settle { get; set; }

        /// <summary>Frames rendered since boot. Also the <c>ping</c> liveness counter.</summary>
        [JsonPropertyName("frames")]
        public int? Frames { get; set; }

        /// <summary>
        /// Fraction of sampled pixels that are near-black. With <see cref="MeanLuma"/>, this is
        /// what distinguishes a dark design from a failed render — a black frame passes every
        /// other check, hashes stably and writes a valid PNG.
        /// </summary>
        [JsonPropertyName("darkFraction")]
        public double? DarkFraction { get; set; }

        [JsonPropertyName("pngBytes")]
        public long? PngBytes { get; set; }

        /// <summary>Set when a render captured nothing because neither png nor raw was asked for.</summary>
        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("stride")]
        public int? Stride { get; set; }

        /// <summary>Always <c>B8G8R8A8</c> today. Checked, not assumed.</summary>
        [JsonPropertyName("format")]
        public string? Format { get; set; }

        [JsonPropertyName("raw")]
        public string? Raw { get; set; }

        /// <summary>Full name of the shared-memory mapping holding this frame, dims appended.</summary>
        [JsonPropertyName("shm")]
        public string? Shm { get; set; }

        [JsonPropertyName("rawBytes")]
        public int? RawBytes { get; set; }

        [JsonPropertyName("png")]
        public string? Png { get; set; }

        /// <summary>Mean luma, 0-255. The black-frame tell.</summary>
        [JsonPropertyName("meanLuma")]
        public double? MeanLuma { get; set; }

        /// <summary>True when a post-process chain is attached, which is what enables TAA.</summary>
        [JsonPropertyName("antiAliased")]
        public bool? AntiAliased { get; set; }

        [JsonPropertyName("sha256")]
        public string? Sha256 { get; set; }

        // --- camera ----------------------------------------------------------

        /// <summary>
        /// Where the camera actually ended up. Read back rather than assumed: the sidecar
        /// clamps pitch and distance against the hull's bounding sphere, so what the UI asked
        /// for and what it got can differ, and the orbit control needs the truth to stay
        /// consistent with the image.
        /// </summary>
        [JsonPropertyName("camera")]
        public SkinrSidecarCamera? Camera { get; set; }
    }

    /// <summary>
    /// The fate of every mesh under a built ship — because a ship is not one mesh, and treating
    /// it as one cost us the entire self-illuminated layer of every render we ever made.
    /// </summary>
    /// <remarks>
    /// A live census of a built Rifter found nineteen meshes and exactly one with vertices: the
    /// hull. The other eighteen — exhaust plumes, reactor glow, tube glow and fourteen additive
    /// glow billboards — each had a resolved shader and <c>display=True</c>, each still pointed
    /// at a Granny <c>.gr2</c> Trinity cannot read, and each drew nothing while reporting itself
    /// perfectly healthy.
    ///
    /// <para><see cref="Unmapped"/> is why this type exists. It names the <c>.gr2</c> files the
    /// host has not converted yet, so the host can convert exactly those instead of guessing a
    /// map per hull family. The list cannot be known before a build — these meshes appear nowhere
    /// in CCP's data the host can read, only on the assembled ship — so the honest flow is build,
    /// read this, convert, extend the map, rebuild.</para>
    /// </remarks>
    public sealed class SkinrSidecarGeometryReport
    {
        /// <summary>Meshes successfully repointed at a converted <c>.cmf</c>.</summary>
        [JsonPropertyName("repointed")]
        public int Repointed { get; set; }

        /// <summary>Meshes already pointing at something Trinity can load.</summary>
        [JsonPropertyName("native")]
        public int Native { get; set; }

        /// <summary>Total meshes walked under the ship.</summary>
        [JsonPropertyName("meshes")]
        public int Meshes { get; set; }

        /// <summary>
        /// Meshes holding a live geometry resource once the loads were pumped — the only count here
        /// that proves anything.
        /// </summary>
        /// <remarks>
        /// <see cref="Repointed"/> says a string was written to a property, and a string is not
        /// vertices. A mesh reads <c>geometry: null, isLoading: false</c> both before its geometry
        /// arrives and after the load failed, so distinguishing those two is the whole job.
        /// </remarks>
        [JsonPropertyName("loaded")]
        public int Loaded { get; set; }

        /// <summary>
        /// The <c>.gr2</c> paths with no conversion available. Non-empty means the ship on screen
        /// is incomplete, and it names precisely what to fix.
        /// </summary>
        [JsonPropertyName("unmapped")]
        public List<string> Unmapped { get; set; } = new();

        /// <summary>
        /// Meshes with a resolved path that still hold no geometry — the complement of
        /// <see cref="Unmapped"/>, and the only field that can catch a missing <c>.cmf</c>.
        /// </summary>
        /// <remarks>
        /// <para><see cref="Unmapped"/> can only ever name a <c>.gr2</c>, because that is the
        /// extension the repointer looks for. A mesh already pointing at a <c>.cmf</c> that is not
        /// in the resource tree therefore counts as <see cref="Native"/>, contributes nothing to
        /// <see cref="Unmapped"/>, and draws nothing — a ship reported healthy with no hull in
        /// it.</para>
        ///
        /// <para>An Astero is what found this: two native meshes, an empty
        /// <see cref="Unmapped"/> list, and a viewport containing only its spotlights. A
        /// <c>.gr2</c> listed here needs converting; anything else is a file the resource tree does
        /// not have, which is a packaging fault rather than a conversion one.</para>
        /// </remarks>
        [JsonPropertyName("notLoaded")]
        public List<string> NotLoaded { get; set; } = new();
    }

    /// <summary>
    /// The studio environment behind the ship: whether it attached, and whether it can draw.
    /// </summary>
    /// <remarks>
    /// <para><see cref="Attached"/> and "visible" are different facts, and conflating them cost a
    /// long investigation. The room can load, append to the scene's render list, register as a
    /// secondary light source and report every field here as healthy while contributing not one
    /// pixel — because its four primitives are Granny <c>.gr2</c> files and
    /// <see cref="Geometry"/><c>.Unmapped</c> is non-empty.</para>
    ///
    /// <para>The numbers, measured on CCP's <c>skinrenv_holographic_01a</c>: unconverted, the
    /// empty viewport reads mean luma 1.08 with a top-to-bottom ramp of −0.15, i.e. black.
    /// Converted, 22.55 with a +17.52 ramp — against CCP's own SKINR viewport at 22.02. The grey
    /// gradient users see in the game is this room's <c>cylinder_01a_ds</c> BackgroundGradient,
    /// authored at scale 3e7, and nothing else.</para>
    /// </remarks>
    public sealed class SkinrSidecarLightEnvReport
    {
        /// <summary>Whether the environment loaded and joined the scene's render list.</summary>
        [JsonPropertyName("attached")]
        public bool Attached { get; set; }

        /// <summary>The <c>res:/</c> path of the environment resource.</summary>
        [JsonPropertyName("path")]
        public string? Path { get; set; }

        /// <summary>Why it did not attach, when it did not.</summary>
        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        /// <summary>Which scene list took it — <c>objects</c> or <c>backgroundObjects</c>.</summary>
        [JsonPropertyName("list")]
        public string? List { get; set; }

        /// <summary>Child objects the environment brought. Zero means an empty room.</summary>
        [JsonPropertyName("objects")]
        public int Objects { get; set; }

        /// <summary>
        /// The fact that decides whether any of the rest matters: an environment with unmapped
        /// geometry is an environment that will not draw, however healthy it otherwise looks.
        /// </summary>
        [JsonPropertyName("geometry")]
        public SkinrSidecarGeometryReport? Geometry { get; set; }
    }

    /// <summary>
    /// What it took to reach a stable image, and if it never did, the shape of the failure.
    /// </summary>
    /// <remarks>
    /// <para>TAA converges by accumulating jittered samples, so an image is only trustworthy
    /// once consecutive frames stop differing. A capture taken too early is not an error — it is
    /// a soft, slightly wrong picture that passes every structural check, which is exactly the
    /// class of defect worth reporting numerically rather than logging.</para>
    ///
    /// <para><see cref="DeltaTail"/> is present only on failure, and its shape is the diagnosis:
    /// a plateau means <see cref="Epsilon"/> is wrong for this framing, a slow descent means it
    /// needed more frames, and a sawtooth means something is still injecting per-frame noise.
    /// That turns "the render looks soft" into a question with an answer instead of a bisect.</para>
    /// </remarks>
    public sealed class SkinrSidecarSettleReport
    {
        /// <summary>Frames rendered inside the settle loop.</summary>
        [JsonPropertyName("frames")]
        public int Frames { get; set; }

        [JsonPropertyName("converged")]
        public bool Converged { get; set; }

        /// <summary>Difference between the last two frames. Null when only one was rendered.</summary>
        [JsonPropertyName("lastDelta")]
        public double? LastDelta { get; set; }

        /// <summary>The convergence threshold in force for this settle.</summary>
        [JsonPropertyName("epsilon")]
        public double Epsilon { get; set; }

        [JsonPropertyName("ms")]
        public long ElapsedMilliseconds { get; set; }

        /// <summary>The last dozen frame deltas. Only sent when convergence failed.</summary>
        [JsonPropertyName("deltaTail")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<double>? DeltaTail { get; set; }
    }

    /// <summary>The camera state the sidecar settled on.</summary>
    public sealed class SkinrSidecarCamera
    {
        [JsonPropertyName("yaw")]
        public double Yaw { get; set; }

        [JsonPropertyName("pitch")]
        public double Pitch { get; set; }

        [JsonPropertyName("distance")]
        public double Distance { get; set; }

        [JsonPropertyName("fov")]
        public double Fov { get; set; }

        [JsonPropertyName("eye")]
        public IReadOnlyList<double>? Eye { get; set; }

        [JsonPropertyName("at")]
        public IReadOnlyList<double>? At { get; set; }

        [JsonPropertyName("up")]
        public IReadOnlyList<double>? Up { get; set; }

        /// <summary>
        /// Near plane, scaled to the orbit radius. A fixed near plane z-fights on capitals and
        /// clips detail on frigates, so it is derived rather than constant.
        /// </summary>
        [JsonPropertyName("near")]
        public double Near { get; set; }
    }

    /// <summary>
    /// What the sidecar did with the mask specs. The counts are the render contract's own
    /// self-check, so they are part of the protocol rather than a log line.
    /// </summary>
    /// <remarks>
    /// <c>overridden</c> is the healthy number: the engine created two masks from the carrier
    /// pattern and we redirected both. A non-zero <c>appended</c> means the engine made none
    /// (the hull is outside the carrier's projection list) and we are on the fallback path.
    /// <c>unclaimed</c> means a mask we expected to drive is still pointing at
    /// <c>black.dds</c> — a resolver bug, not a render one.
    /// </remarks>
    public sealed class SkinrSidecarMaskReport
    {
        [JsonPropertyName("preexisting")]
        public int Preexisting { get; set; }

        [JsonPropertyName("overridden")]
        public int Overridden { get; set; }

        [JsonPropertyName("appended")]
        public int Appended { get; set; }

        [JsonPropertyName("failed")]
        public int Failed { get; set; }

        [JsonPropertyName("unclaimed")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<int>? Unclaimed { get; set; }
    }

    /// <summary>
    /// What happened when the pattern-mask textures were bound to the hull's shaders. This is
    /// the answer to "can this hull wear a SKINR design at all", and it is measured rather
    /// than predicted.
    /// </summary>
    /// <remarks>
    /// A hull whose shaders support SKINR ships with <c>PatternMask1Map</c> and
    /// <c>PatternMask2Map</c> already declared — the carrier pattern binds them to
    /// <c>black.dds</c>, which paints nothing. So the healthy path is
    /// <see cref="Rebound"/> &gt; 0: we redirected samplers CCP already declared. On the
    /// Slasher that reads <c>effects: 14, rebound: 28</c> — every mesh area, both layers.
    ///
    /// <see cref="Added"/> &gt; 0 is the warning sign, not a success: it means the shader has
    /// no declaration for the sampler, so the engine's resource mapping finds nothing to bind
    /// it to. The render then completes and looks like a design the user did not ask for,
    /// which is worse than an error. <see cref="Rebound"/> == 0 with a non-empty texture list
    /// means the hull cannot show patterns and the UI should say so instead of rendering.
    ///
    /// <see cref="EffectFiles"/> costs nothing to collect and explains a lot after the fact:
    /// <c>quadsailsv5</c> and <c>quadglassv5</c> do not respond to a hull pattern the way
    /// <c>quadv5</c> does, so "the pattern is missing from the wings" has an answer here
    /// rather than in a bisect.
    /// </remarks>
    public sealed class SkinrSidecarTextureBinding
    {
        /// <summary>Mesh-area effects the textures were offered to.</summary>
        [JsonPropertyName("effects")]
        public int Effects { get; set; }

        /// <summary>Sampler slots that already existed and were redirected. The healthy count.</summary>
        [JsonPropertyName("rebound")]
        public int Rebound { get; set; }

        /// <summary>Sampler slots we had to invent because the shader lacked them.</summary>
        [JsonPropertyName("added")]
        public int Added { get; set; }

        /// <summary>Effects with no resource prototype to clone, so nothing could be bound.</summary>
        [JsonPropertyName("noProto")]
        public int NoProto { get; set; }

        /// <summary>Distinct shader filenames on this hull, e.g. <c>quadv5.fx</c>.</summary>
        [JsonPropertyName("effectFiles")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<string>? EffectFiles { get; set; }
    }
}
