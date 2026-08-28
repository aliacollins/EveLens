// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;

namespace EveLens.Common.Data
{
    /// <summary>
    /// An ESI SKINR recipe joined against the SDE catalog: named components, computed design
    /// points and tier, and everything the renderer needs to draw the design.
    /// </summary>
    /// <remarks>
    /// This is the boundary type between "what ESI said" and "what we can show". ESI's recipe
    /// is eight integers and a transform; this is the same design with names, prices, icons
    /// and a SpaceObjectFactory DNA string attached.
    ///
    /// The render contract it encodes is not obvious and was established by reading CCP's
    /// engine source, so it is documented on the properties that carry it rather than left
    /// implicit. In particular a design has <em>six</em> materials, not four, and they reach
    /// the shader through two different DNA commands:
    ///
    /// <code>
    /// &lt;hull&gt;:&lt;faction&gt;:&lt;race&gt;
    ///   :material?&lt;m1&gt;;&lt;m2&gt;;&lt;m3&gt;;&lt;m4&gt;              slots 1-4, faction-remapped
    ///   :pattern?cosm_blank_projection;&lt;PMtl1&gt;;&lt;PMtl2&gt;   slots 6 and 8
    /// </code>
    ///
    /// The engine's parameter prefixes are <c>Mtl1..Mtl4</c> and <c>PMtl1..PMtl2</c> — four
    /// plus two, matching the SDE's eight slots exactly. <c>material?</c> validates at arity
    /// four and nothing else; the two pattern materials are arguments of <c>pattern?</c>.
    ///
    /// <see cref="IsRenderable"/> is false whenever the DNA could not be built — an
    /// unpublished hull, a hull with no SOF identity, or a catalog that failed to load. The
    /// UI can still show every name and price in that case, which is why resolution never
    /// throws: a design we cannot draw is still a design we can describe.
    ///
    /// Nothing here is mutated after construction. <see cref="Warnings"/> carries anything the
    /// resolver had to paper over, so an unknown component surfaces in the UI instead of
    /// silently becoming a blank slot.
    /// </remarks>
    public sealed class SkinrResolvedDesign
    {
        /// <summary>
        /// The SOF pattern that carries a SKINR design's two pattern layers.
        /// </summary>
        /// <remarks>
        /// SKINR patterns are not SOF patterns. A SKINR pattern component is a
        /// <c>.dds</c> texture plus a user-authored transform, while <c>pattern?</c>'s first
        /// argument must name one of the 489 patterns in SOF's own library.
        ///
        /// Exactly one of those 489 is in the cosmetic namespace, and it is called
        /// <em>blank projection</em>. Building with it creates both custom masks at the right
        /// material indices (4 and 5) and binds both mask textures to
        /// <c>res:/texture/global/black.dds</c> — a mask that paints nothing. It exists so a
        /// client can claim the two mask slots and the two <c>PMtl</c> materials with no
        /// geometry of its own, then supply the shape itself. That is precisely what SKINR
        /// needs, and it is legal on 309 hulls, about the set SKINR supports.
        ///
        /// So the renderer never builds masks from scratch: it redirects the two the engine
        /// already made.
        /// </remarks>
        public const string CarrierPatternName = "cosm_blank_projection";

        internal SkinrResolvedDesign(string skinrId, string name, string line, long creatorId,
            int shipTypeId, SkinrHull? hull, int tierLevel, string patternBlendMode,
            string dna, IReadOnlyList<SkinrResolvedMaterial> nanocoatings,
            IReadOnlyList<SkinrResolvedPattern> patterns,
            SkinrSlotConfiguration slotConfiguration, int designPoints, int computedTier,
            int? pointsToNextTier, IReadOnlyList<string> warnings)
        {
            SkinrId = skinrId;
            Name = name;
            Line = line;
            CreatorId = creatorId;
            ShipTypeId = shipTypeId;
            Hull = hull;
            TierLevel = tierLevel;
            PatternBlendMode = patternBlendMode;
            Dna = dna;
            Nanocoatings = nanocoatings;
            Patterns = patterns;
            SlotConfiguration = slotConfiguration;
            DesignPoints = designPoints;
            ComputedTier = computedTier;
            PointsToNextTier = pointsToNextTier;
            Warnings = warnings;
        }

        /// <summary>The opaque SKINR identifier. A string, not a number — ESI is explicit.</summary>
        public string SkinrId { get; }

        /// <summary>
        /// The design's name as ESI returned it, already localized by the request's
        /// Accept-Language. Unlike component names this is player-authored, so it is not in
        /// the SDE and cannot be re-localized client-side.
        /// </summary>
        public string Name { get; }

        /// <summary>The SKINR line (family), or empty — ESI marks it optional.</summary>
        public string Line { get; }

        public long CreatorId { get; }

        public int ShipTypeId { get; }

        /// <summary>Null when the catalog is unavailable or the hull is not SKINR-capable.</summary>
        public SkinrHull? Hull { get; }

        /// <summary>The tier ESI reports. Compare against <see cref="ComputedTier"/>.</summary>
        public int TierLevel { get; }

        /// <summary>
        /// How the two pattern layers composite: <c>normal</c>, <c>subtract</c>,
        /// <c>exclusion</c>, <c>nested</c> or <c>nested_inverted</c>. This changes the
        /// rendered result, so it is part of the render contract rather than metadata.
        /// </summary>
        public string PatternBlendMode { get; }

        /// <summary>
        /// The SpaceObjectFactory DNA string, or empty when the design cannot be rendered.
        /// </summary>
        public string Dna { get; }

        /// <summary>
        /// True when <see cref="Dna"/> is non-empty. The renderer still validates it against
        /// the live SOF library — this only says we had enough static data to compose one.
        /// </summary>
        public bool IsRenderable => !string.IsNullOrEmpty(Dna);

        /// <summary>
        /// Slots 1-4, in <em>DNA material order</em> (m1..m4), not in ESI slot order. The
        /// faction remap has already been applied, so index 0 is the material the shader
        /// calls <c>Mtl1</c>. Always exactly four entries; an empty slot is still present so
        /// the arity is right.
        /// </summary>
        public IReadOnlyList<SkinrResolvedMaterial> Nanocoatings { get; }

        /// <summary>
        /// Zero, one or two pattern layers, in layer order. A design may use the secondary
        /// pattern without the primary, in which case there is a single entry with
        /// <see cref="SkinrResolvedPattern.LayerIndex"/> 1.
        /// </summary>
        public IReadOnlyList<SkinrResolvedPattern> Patterns { get; }

        /// <summary>
        /// The pattern layers that can actually be painted — the subset of
        /// <see cref="Patterns"/> whose mask texture resolved.
        /// </summary>
        /// <remarks>
        /// This exists so exactly one place in the codebase decides what "drawable" means. The
        /// renderer host used to filter with an inline predicate when composing the build
        /// request, and then re-derive "how many layers did we expect" from <em>the same
        /// predicate</em> when deciding whether to warn. That made the reporting arithmetically
        /// incapable of noticing the drop: if every layer failed to resolve, expected became 0,
        /// "supports patterns" became true, and a design that rendered with no pattern at all
        /// reported complete success. Compare against <see cref="Patterns"/> — the recipe ESI
        /// actually sent — and the blind spot cannot be reconstructed.
        /// </remarks>
        public IEnumerable<SkinrResolvedPattern> DrawablePatterns =>
            Patterns.Where(p => p.IsDrawable);

        /// <summary>
        /// The pattern layers ESI asked for that we cannot paint, because their pattern
        /// component is not in the catalog. Non-empty means the preview is missing paint the
        /// design specifies, which is a static-data problem worth telling the user about.
        /// </summary>
        public IEnumerable<SkinrResolvedPattern> UndrawablePatterns =>
            Patterns.Where(p => !p.IsDrawable);

        /// <summary>Which slots this hull offers. Never null.</summary>
        public SkinrSlotConfiguration SlotConfiguration { get; }

        /// <summary>
        /// Total design points, summed from each filled component's category/rarity value.
        /// This is what tier is computed from.
        /// </summary>
        public int DesignPoints { get; }

        /// <summary>
        /// Tier derived from <see cref="DesignPoints"/> and the hull class's thresholds. Should
        /// equal <see cref="TierLevel"/>; a mismatch means our point table has drifted from
        /// the server's, which is worth surfacing rather than hiding.
        /// </summary>
        public int ComputedTier { get; }

        /// <summary>Points needed for the next tier, or null at the top tier.</summary>
        public int? PointsToNextTier { get; }

        /// <summary>
        /// True when ESI's tier and ours disagree. Diagnostic: it means the SDE point values
        /// or thresholds no longer match live, not that the design is invalid.
        /// </summary>
        public bool TierMismatch => ComputedTier != TierLevel;

        /// <summary>
        /// Everything the resolver had to work around: unknown component IDs, components in
        /// slots the hull does not offer, a pattern with no material. Empty for a clean
        /// resolve. Surfaced in the UI so a stale catalog is visible rather than silent.
        /// </summary>
        public IReadOnlyList<string> Warnings { get; }

        public override string ToString() =>
            $"{Name} ({SkinrId}) tier {TierLevel}, {DesignPoints} pts, " +
            $"{Patterns.Count} pattern(s){(IsRenderable ? string.Empty : ", not renderable")}";
    }

    /// <summary>One resolved nanocoating: an ESI slot joined to its catalog component.</summary>
    public sealed class SkinrResolvedMaterial
    {
        internal SkinrResolvedMaterial(int slotId, int materialPosition, int componentId,
            SkinrComponent? component)
        {
            SlotId = slotId;
            MaterialPosition = materialPosition;
            ComponentId = componentId;
            Component = component;
        }

        /// <summary>The ESI slot, 1-4. See <see cref="SkinrSlot"/> for the names.</summary>
        public int SlotId { get; }

        /// <summary>
        /// Where this lands in the DNA, 1-4, after the faction remap. The shader parameter
        /// prefix is <c>Mtl{MaterialPosition}</c>.
        /// </summary>
        public int MaterialPosition { get; }

        /// <summary>Zero when the designer left this slot empty.</summary>
        public int ComponentId { get; }

        /// <summary>Null when the slot is empty or the component is not in the catalog.</summary>
        public SkinrComponent? Component { get; }

        public bool IsEmpty => ComponentId == 0;

        /// <summary>
        /// The token this contributes to the <c>material?</c> command. Falls back to
        /// <see cref="SkinrDna.EmptyMaterialToken"/> so the command keeps its required arity.
        /// </summary>
        public string DnaToken =>
            string.IsNullOrEmpty(Component?.DnaToken)
                ? SkinrDna.EmptyMaterialToken
                : Component!.DnaToken;

        public override string ToString() =>
            $"slot {SlotId} -> Mtl{MaterialPosition}: {Component?.Name.English ?? "(empty)"}";
    }

    /// <summary>
    /// One resolved pattern layer: the mask texture, the material painted through it, and the
    /// transform that places it — everything one <c>EveCustomMask</c> needs.
    /// </summary>
    public sealed class SkinrResolvedPattern
    {
        internal SkinrResolvedPattern(int layerIndex, int patternSlotId, int materialSlotId,
            SkinrComponent? pattern, SkinrComponent? material, int patternComponentId,
            int materialComponentId, IReadOnlyList<double> position,
            IReadOnlyList<double> rotation, IReadOnlyList<double> scaling, bool isMirrored,
            IReadOnlyList<double> targetMaterials, string patternBlendMode = "normal")
        {
            LayerIndex = layerIndex;
            PatternSlotId = patternSlotId;
            MaterialSlotId = materialSlotId;
            Pattern = pattern;
            Material = material;
            PatternComponentId = patternComponentId;
            MaterialComponentId = materialComponentId;
            Position = position;
            Rotation = rotation;
            Scaling = scaling;
            IsMirrored = isMirrored;
            TargetMaterials = targetMaterials;
            PatternBlendMode = patternBlendMode;
        }

        /// <summary>0 for the primary pattern (slots 5/6), 1 for the secondary (slots 7/8).</summary>
        public int LayerIndex { get; }

        /// <summary><see cref="SkinrSlot.Pattern"/> or <see cref="SkinrSlot.SecondaryPattern"/>.</summary>
        public int PatternSlotId { get; }

        /// <summary>The paired pattern-material slot, 6 or 8.</summary>
        public int MaterialSlotId { get; }

        /// <summary>The pattern component — a <c>.dds</c> mask. Null if not in the catalog.</summary>
        public SkinrComponent? Pattern { get; }

        /// <summary>The material painted through the mask. Null if the slot is empty.</summary>
        public SkinrComponent? Material { get; }

        public int PatternComponentId { get; }
        public int MaterialComponentId { get; }

        /// <summary>
        /// Which mask texture slot this layer binds: <c>PatternMask1Map</c> for layer 0,
        /// <c>PatternMask2Map</c> for layer 1. The engine supports exactly two
        /// (<c>EVE_SPACEOBJECT_CUSTOWMASK_MAX</c> is 2, CCP's typo included), which is why
        /// SKINR offers exactly two pattern slots.
        /// </summary>
        public string TextureName => LayerIndex == 0 ? "PatternMask1Map" : "PatternMask2Map";

        /// <summary>The mask texture to bind, or empty when the component is unknown.</summary>
        public string TextureResourcePath => Pattern?.ResourceFile ?? string.Empty;

        /// <summary>
        /// Whether this layer can be painted at all: false when the pattern component is not in
        /// the catalog, so there is no mask texture to bind.
        /// </summary>
        /// <remarks>
        /// A layer with no texture must not be sent to the renderer — binding an empty path to a
        /// sampler is how a hull ends up drawing no pattern and reporting no error. But the drop
        /// has to be <em>countable</em>, which is why this is a named property on the model
        /// rather than a predicate written out at each call site. See
        /// <see cref="SkinrResolvedDesign.DrawablePatterns"/> for why that distinction cost us a
        /// silently patternless Astero.
        /// </remarks>
        public bool IsDrawable => !string.IsNullOrEmpty(TextureResourcePath);

        /// <summary>
        /// The design's <c>pattern_blend_mode</c>, verbatim from ESI. Carried per-layer only
        /// so the resolver's warnings can tell a layer that paints its own material from one
        /// the shader composites away; the renderer receives the mode once, per design.
        /// </summary>
        public string PatternBlendMode { get; }

        /// <summary>
        /// The value for <c>EveCustomMask.materialIndex</c>: 4 for layer 0, 5 for layer 1 —
        /// always, in every blend mode.
        /// </summary>
        /// <remarks>
        /// <para>These are <c>EveSOFDataPatternLayer::MaterialSource</c>:
        /// <c>SOURCE_MATERIAL1 = 0 .. SOURCE_MATERIAL4 = 3, SOURCE_PATTERN1 = 4,
        /// SOURCE_PATTERN2 = 5</c>. CCP's own Blue attribute table names 4 and 5
        /// "PatternMaterial1" and "PatternMaterial2".</para>
        ///
        /// <para><b>Blend modes do not live here, and that is measured twice.</b> An earlier
        /// version expressed <c>exclusion</c> by pointing layer 1 at a base material — an
        /// "eraser" that repaints the coating layer 0 covered. It produced exactly half the
        /// effect: the Tristan "TwoFace" got its brass swirl on the black panel and lost the
        /// black swirl on the orange side, because exclusion is XOR and two sequential lerps
        /// of constant sources cannot depend on both masks at once. The real compositing is
        /// compiled into CCP's shader binaries as permutations —
        /// <c>BLEND_MODE_OVERLAY/_SUBTRACT/_EXCLUSION/_NESTED/_NESTED_INVERTED</c>, found by
        /// a strings-scan of the live <c>quadv5.sm_hi</c> — and selected by a
        /// <c>BLEND_MODE</c> entry on each hull effect's <c>options</c> list, the same
        /// mechanism <c>EveSOF</c> uses for <c>SPACE_OBJECT_TRANSPARENCY</c>. A/B/C cells on
        /// the Tristan confirmed the permutation paints both directions of the exchange with
        /// the masks left at 4/5. The sidecar owns that selection
        /// (<c>_apply_blend_mode</c>); the host sends <c>patternBlendMode</c> on the build
        /// request.</para>
        ///
        /// <para>Worth knowing: <c>EveCustomMask</c>'s constructor defaults this to 0, i.e.
        /// the hull's primary colour. Anything that forgets to set it paints something
        /// plausible rather than failing, which is exactly the kind of bug that ships.</para>
        /// </remarks>
        public int MaterialIndex => LayerIndex == 0 ? 4 : 5;

        /// <summary>ESI's transform, verbatim: x, y, z.</summary>
        public IReadOnlyList<double> Position { get; }

        /// <summary>ESI's rotation quaternion: x, y, z, w.</summary>
        public IReadOnlyList<double> Rotation { get; }

        /// <summary>ESI's scaling: x, y, z.</summary>
        public IReadOnlyList<double> Scaling { get; }

        public bool IsMirrored { get; }

        /// <summary>
        /// Which of the four hull materials the pattern paints over, as a 4-vector of 1 or 0
        /// in DNA material order — the faction remap is already applied, so element 0 means
        /// <c>Mtl1</c>. Built from ESI's <c>projection.slot1..slot4</c> the same way
        /// <c>EveSOFUtils::CreateMaterialApplicationVector</c> does it.
        /// </summary>
        public IReadOnlyList<double> TargetMaterials { get; }

        /// <summary>
        /// Whether the mask clamps in U. True only for <c>clamp-to-edge</c>.
        /// </summary>
        /// <remarks>
        /// The catalog's projection type has three values but <c>EveCustomMask</c> has a bool,
        /// and CCP collapses the two with <c>projectionAddressModeU == TA_CLAMP</c> — an
        /// equality test, not a not-equal. So <c>clamp-to-border</c> maps to false, exactly
        /// like <c>repeat</c>. The intuitive reading (anything that is not repeat clamps) puts
        /// a hard border on patterns that should tile.
        /// </remarks>
        public bool ClampU => Pattern?.ProjectionTypeU == SkinrProjectionType.ClampToEdge;

        /// <inheritdoc cref="ClampU"/>
        public bool ClampV => Pattern?.ProjectionTypeV == SkinrProjectionType.ClampToEdge;

        /// <summary>
        /// The texture address mode the renderer writes into this layer's
        /// <c>PatternMask{N}MapSampler</c> override: clamp-to-edge 3 (<c>TA_CLAMP</c>),
        /// repeat 1 (<c>TA_WRAP</c>), clamp-to-border 4 (<c>TA_BORDER</c>).
        /// </summary>
        /// <remarks>
        /// <para>This — not <see cref="ClampU"/> — is how projection edges actually work.
        /// CCP's studio (<c>cosmeticsManager.ConfigurePatternBorderSettings</c>, decompiled
        /// from the client) never touches the custom mask's clamp flags; it rewrites the
        /// effect's sampler override and repopulates parameters.</para>
        ///
        /// <para><b>The values here are the ENGINE's, not the client python's.</b>
        /// <c>Tr2RenderContextEnum::TextureAddressMode</c> is WRAP=1, MIRROR=2, CLAMP=3,
        /// BORDER=4, and <c>EveSOFUtils::GetTextureAddressMode</c> maps SOF's projection
        /// types accordingly. The decompiled studio python maps CLAMP→4 and BORDER→3 —
        /// swapped relative to the engine enum. Measured on the Astero "Circe" (Division:
        /// border U, clamp-to-edge V): the python values cut the spine stripe at its
        /// projection box; the engine values (U=4, V=3) extend it nose to tail, matching
        /// the game. When a decompiled constant and the engine's own enum disagree, the
        /// pixels get the vote.</para>
        /// </remarks>
        public int SamplerAddressU => SamplerAddressFor(Pattern?.ProjectionTypeU);

        /// <inheritdoc cref="SamplerAddressU"/>
        public int SamplerAddressV => SamplerAddressFor(Pattern?.ProjectionTypeV);

        private static int SamplerAddressFor(string? projectionType) =>
            projectionType switch
            {
                SkinrProjectionType.Repeat => 1,
                SkinrProjectionType.ClampToEdge => 3,
                // clamp-to-border, and the carrier masks' authored default for anything else
                _ => 4,
            };

        /// <summary>
        /// The token this layer contributes to the <c>pattern?</c> command — its material, not
        /// its texture. The texture is bound separately as <see cref="TextureName"/>.
        /// </summary>
        public string MaterialDnaToken =>
            string.IsNullOrEmpty(Material?.DnaToken)
                ? SkinrDna.EmptyMaterialToken
                : Material!.DnaToken;

        public override string ToString() =>
            $"layer {LayerIndex}: {Pattern?.Name.English ?? "(unknown)"} in " +
            $"{Material?.Name.English ?? "(no material)"}";
    }

    /// <summary>
    /// The three values a component's <c>projectionTypeU</c>/<c>projectionTypeV</c> can take,
    /// mirroring <c>EveSOFDataPatternLayer::ProjectionType</c>.
    /// </summary>
    public static class SkinrProjectionType
    {
        /// <summary><c>PROJECTION_REPEAT</c> = 0. Tiles.</summary>
        public const string Repeat = "repeat";

        /// <summary><c>PROJECTION_CLAMP</c> = 1. The only value that sets the clamp flag.</summary>
        public const string ClampToEdge = "clamp-to-edge";

        /// <summary><c>PROJECTION_BORDER</c> = 2. Clamps to a border colour, flag stays false.</summary>
        public const string ClampToBorder = "clamp-to-border";
    }

    /// <summary>
    /// Composes SpaceObjectFactory DNA strings. Separated from the resolver so the format is
    /// stated in one place and testable without a catalog.
    /// </summary>
    /// <remarks>
    /// The grammar, from <c>EveSOFDNA</c>: colon-separated fields, the first three being hull,
    /// faction and race, then zero or more <c>command?arg;arg;arg</c> groups.
    ///
    /// Arities are not advisory. <c>material?</c> validates at exactly four arguments and
    /// <c>pattern?</c> at exactly three; anything else is rejected outright by
    /// <c>ValidateDNA</c>. So an unfilled slot still has to emit a token.
    /// </remarks>
    public static class SkinrDna
    {
        /// <summary>
        /// What an empty material slot emits. Resolves to no material, so
        /// <c>EveSOFDNA::GetMeshAreaParameter</c> falls through to the race and then faction
        /// defaults — the hull's stock colour, which is what the SKINR editor shows for an
        /// empty slot.
        /// </summary>
        /// <remarks>
        /// This is a sentinel the engine recognizes, not a string that happens to slip
        /// through: a DNA carrying <c>none</c> in any material position validates and builds,
        /// while one carrying an unrecognized token fails validation outright. An empty string
        /// also fails, so padding has to use this and not <see cref="string.Empty"/>.
        /// </remarks>
        public const string EmptyMaterialToken = "none";

        /// <summary>How many arguments <c>material?</c> requires.</summary>
        public const int MaterialArity = 4;

        /// <summary>
        /// Builds the DNA for a design. Returns empty when <paramref name="hull"/> lacks a SOF
        /// identity, since there is nothing to build from.
        /// </summary>
        /// <param name="hull">The hull, for its SOF hull/faction/race names.</param>
        /// <param name="materialTokens">
        /// Exactly <see cref="MaterialArity"/> tokens in DNA material order. Fewer are padded
        /// with <see cref="EmptyMaterialToken"/> and extras dropped, because emitting the
        /// wrong arity fails validation rather than degrading.
        /// </param>
        /// <param name="patternMaterialTokens">
        /// The two pattern materials, or null/empty to omit <c>pattern?</c> entirely and let
        /// the faction's default livery apply.
        /// </param>
        public static string Build(SkinrHull? hull, IReadOnlyList<string> materialTokens,
            IReadOnlyList<string>? patternMaterialTokens)
        {
            if (hull == null || !hull.HasSofIdentity)
                return string.Empty;

            string[] materials = Normalize(materialTokens, MaterialArity);
            string dna = $"{hull.SofHullName}:{hull.SofFactionName}:{hull.SofRaceName}" +
                         $":material?{string.Join(";", materials)}";

            // pattern? is all-or-nothing: its two material arguments are mandatory, so a
            // design with no pattern layers omits the command and inherits the faction livery.
            if (patternMaterialTokens != null && patternMaterialTokens.Count > 0)
            {
                string[] patternMaterials = Normalize(patternMaterialTokens, 2);
                dna += $":pattern?{SkinrResolvedDesign.CarrierPatternName}" +
                       $";{string.Join(";", patternMaterials)}";
            }

            return dna;
        }

        private static string[] Normalize(IReadOnlyList<string>? tokens, int arity)
        {
            string[] result = new string[arity];
            for (int i = 0; i < arity; i++)
            {
                string? token = tokens != null && i < tokens.Count ? tokens[i] : null;
                result[i] = string.IsNullOrWhiteSpace(token) ? EmptyMaterialToken : token!;
            }
            return result;
        }
    }
}
