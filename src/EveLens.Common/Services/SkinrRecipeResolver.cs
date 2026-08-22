// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EveLens.Common.Data;
using EveLens.Common.Interfaces;
using EveLens.Common.Serialization.Esi;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Joins ESI SKINR recipes to the SDE catalog. See <see cref="ISkinrRecipeResolver"/>.
    /// </summary>
    /// <remarks>
    /// The interesting work here is the two remaps, both of which fail silently when wrong:
    ///
    /// <list type="number">
    /// <item>
    /// ESI slot → DNA material position, via <c>skinrSlotsToMaterials[factionID]</c>. A
    /// bijection over 1-4, authored only for the 16 factions that ship their own hulls; absent
    /// factions are identity. Skip it and the design renders the right four colours in the
    /// wrong four places.
    /// </item>
    /// <item>
    /// The same remap applied to a pattern's <c>projection.slot1..slot4</c> flags, because
    /// those name ESI slots but the shader's <c>customMaskTargets</c> vector is indexed by DNA
    /// material position. Skip it and patterns paint over the wrong panels.
    /// </item>
    /// </list>
    ///
    /// There is a third remap that is deliberately <em>not</em> here:
    /// <c>EveSOFDataFaction.materialUsageMtl1..4</c>, DNA position → shader material index.
    /// That one is turret-only — the engine reads it exclusively in
    /// <c>EveSOF::SetupTurretMaterial</c> (EveSOF.cpp:3845/3865, via EveSOFDNA.cpp:1378) —
    /// so it never applies to hull areas at all and doing it here would apply a turret remap
    /// to a hull.
    /// </remarks>
    public sealed class SkinrRecipeResolver : ISkinrRecipeResolver
    {
        private readonly SkinrCatalog _catalog;

        /// <summary>Production constructor: loads the bundled catalog.</summary>
        public SkinrRecipeResolver()
            : this(SkinrCatalog.Load())
        {
        }

        /// <summary>Test constructor: takes a catalog directly, no file needed.</summary>
        public SkinrRecipeResolver(SkinrCatalog catalog)
        {
            _catalog = catalog ?? SkinrCatalog.Empty;
        }

        public SkinrCatalog Catalog => _catalog;

        public bool IsAvailable => _catalog.IsAvailable;

        public SkinrComponent? GetComponent(int componentId) => _catalog.GetComponent(componentId);

        public SkinrHull? GetHull(int shipTypeId) => _catalog.GetHull(shipTypeId);

        public string GetComponentName(int componentId)
        {
            SkinrComponent? component = _catalog.GetComponent(componentId);
            if (component != null)
                return component.Name.Localized;

            return string.Format(CultureInfo.CurrentCulture, "Component {0}", componentId);
        }

        public int GetDesignPoints(IEnumerable<int> componentIds)
        {
            if (componentIds == null)
                return 0;

            int total = 0;
            foreach (int id in componentIds)
            {
                SkinrComponent? component = _catalog.GetComponent(id);
                if (component != null)
                    total += _catalog.GetPointValue(component.CategoryId, component.RarityId);
            }
            return total;
        }

        public SkinrResolvedDesign Resolve(EsiSkinrRecipe? recipe)
        {
            var warnings = new List<string>();

            if (recipe == null)
            {
                warnings.Add("No recipe to resolve.");
                return Empty(string.Empty, warnings);
            }

            if (!_catalog.IsAvailable)
                warnings.Add("SKINR static data is unavailable, so names and rendering are limited.");

            SkinrHull? hull = _catalog.GetHull(recipe.ShipTypeId);
            if (hull == null && _catalog.IsAvailable)
            {
                warnings.Add(string.Format(CultureInfo.CurrentCulture,
                    "Ship type {0} is not in the SKINR hull table.", recipe.ShipTypeId));
            }

            SkinrSlotConfiguration configuration = _catalog.GetSlotConfiguration(recipe.ShipTypeId);
            IReadOnlyDictionary<int, int> slotToMaterial = _catalog.GetSlotToMaterialMap(hull?.FactionId);

            List<EsiSkinrSlot> slots = recipe.Layout?.Slots?.Where(s => s != null).ToList()
                                       ?? new List<EsiSkinrSlot>();

            List<SkinrResolvedMaterial> nanocoatings =
                ResolveNanocoatings(slots, slotToMaterial, configuration, warnings);

            List<SkinrResolvedPattern> patterns = ResolvePatterns(slots, slotToMaterial,
                configuration, recipe.Layout?.PatternBlendMode ?? "normal", warnings);

            WarnAboutUnconsumedSlots(slots, warnings);

            int designPoints = SumPoints(nanocoatings, patterns);
            int computedTier = _catalog.GetTierForPoints(hull?.ShipTreeGroupId, designPoints);
            int? pointsToNext = _catalog.GetPointsToNextTier(hull?.ShipTreeGroupId, designPoints);

            string dna = SkinrDna.Build(hull,
                nanocoatings.Select(m => m.DnaToken).ToList(),
                BuildPatternMaterialTokens(patterns));

            if (string.IsNullOrEmpty(dna) && hull != null)
            {
                warnings.Add(string.Format(CultureInfo.CurrentCulture,
                    "Hull {0} has no SpaceObjectFactory identity, so the design cannot be rendered.",
                    hull.Name.English));
            }

            return new SkinrResolvedDesign(
                recipe.Id ?? string.Empty,
                recipe.Name ?? string.Empty,
                recipe.Line ?? string.Empty,
                recipe.CreatorId,
                recipe.ShipTypeId,
                hull,
                recipe.Tier?.Level ?? 0,
                recipe.Layout?.PatternBlendMode ?? "normal",
                dna,
                nanocoatings,
                patterns,
                configuration,
                designPoints,
                computedTier,
                pointsToNext,
                warnings);
        }

        /// <summary>
        /// Resolves slots 1-4 and returns them ordered by DNA material position, so the caller
        /// can emit <c>material?</c> tokens straight off the list. Always four entries.
        /// </summary>
        private List<SkinrResolvedMaterial> ResolveNanocoatings(List<EsiSkinrSlot> slots,
            IReadOnlyDictionary<int, int> slotToMaterial, SkinrSlotConfiguration configuration,
            List<string> warnings)
        {
            var resolved = new List<SkinrResolvedMaterial>(SkinrDna.MaterialArity);

            foreach (int slotId in SkinrSlot.NanocoatingSlots)
            {
                int componentId = slots
                    .FirstOrDefault(s => s.Id == slotId)?.Configuration?.Nanocoating?.Id ?? 0;

                SkinrComponent? component = componentId != 0
                    ? _catalog.GetComponent(componentId)
                    : null;

                if (componentId != 0 && component == null && _catalog.IsAvailable)
                {
                    warnings.Add(string.Format(CultureInfo.CurrentCulture,
                        "Component {0} in slot {1} is not in the catalog; the bundled SDE build " +
                        "may predate it.", componentId, SlotLabel(slotId)));
                }

                if (componentId != 0 && !configuration.Allows(slotId))
                {
                    warnings.Add(string.Format(CultureInfo.CurrentCulture,
                        "Slot {0} is filled but this hull's configuration ({1}) does not offer it.",
                        SlotLabel(slotId), configuration.Name));
                }

                resolved.Add(new SkinrResolvedMaterial(slotId,
                    MaterialPositionFor(slotId, slotToMaterial), componentId, component));
            }

            // DNA material order, not ESI slot order. This is the remap actually taking effect;
            // everything downstream reads position from the list index.
            resolved.Sort((a, b) => a.MaterialPosition.CompareTo(b.MaterialPosition));
            return resolved;
        }

        private List<SkinrResolvedPattern> ResolvePatterns(List<EsiSkinrSlot> slots,
            IReadOnlyDictionary<int, int> slotToMaterial, SkinrSlotConfiguration configuration,
            string patternBlendMode, List<string> warnings)
        {
            var resolved = new List<SkinrResolvedPattern>(2);
            int[] patternSlots = { SkinrSlot.Pattern, SkinrSlot.SecondaryPattern };

            for (int layerIndex = 0; layerIndex < patternSlots.Length; layerIndex++)
            {
                int patternSlotId = patternSlots[layerIndex];
                EsiSkinrPattern? esiPattern = slots
                    .FirstOrDefault(s => s.Id == patternSlotId)?.Configuration?.Pattern;
                if (esiPattern == null)
                    continue;

                int materialSlotId = SkinrSlot.MaterialSlotForPattern(patternSlotId);

                // The pattern material sits in its own slot (6 or 8) and is a material, so ESI
                // sends it as a nanocoating configuration.
                int materialComponentId = slots
                    .FirstOrDefault(s => s.Id == materialSlotId)?.Configuration?.Nanocoating?.Id ?? 0;

                SkinrComponent? pattern = _catalog.GetComponent(esiPattern.Id);
                SkinrComponent? material = materialComponentId != 0
                    ? _catalog.GetComponent(materialComponentId)
                    : null;

                if (pattern == null && _catalog.IsAvailable)
                {
                    warnings.Add(string.Format(CultureInfo.CurrentCulture,
                        "Pattern component {0} in slot {1} is not in the catalog, so its mask " +
                        "texture is unknown.", esiPattern.Id, SlotLabel(patternSlotId)));
                }

                if (!configuration.Allows(patternSlotId))
                {
                    warnings.Add(string.Format(CultureInfo.CurrentCulture,
                        "Slot {0} is filled but this hull's configuration ({1}) does not offer it.",
                        SlotLabel(patternSlotId), configuration.Name));
                }

                EsiSkinrPatternConfiguration? config = esiPattern.Configuration;

                var layer = new SkinrResolvedPattern(
                    layerIndex,
                    patternSlotId,
                    materialSlotId,
                    pattern,
                    material,
                    esiPattern.Id,
                    materialComponentId,
                    ToVector(config?.Transform?.Position, 0d),
                    ToQuaternion(config?.Transform?.Rotation),
                    ToVector(config?.Transform?.Scaling, 1d),
                    config?.Mirrored ?? false,
                    BuildTargetMaterials(config?.Projection, slotToMaterial),
                    patternBlendMode);
                resolved.Add(layer);

                // An empty material slot only degrades a layer whose own material actually
                // paints. Under subtract/exclusion/nested_inverted the shader's blend
                // permutation composites the secondary layer against the primary rather than
                // painting its material — ESI legitimately ships exclusion recipes with slot 8
                // absent (the Tristan "TwoFace"), and warning about those accused every one of
                // them of being broken.
                bool secondaryIsComposited = layerIndex == 1 &&
                    (patternBlendMode == "subtract" || patternBlendMode == "exclusion" ||
                     patternBlendMode == "nested_inverted");
                if (materialComponentId == 0 && !secondaryIsComposited)
                {
                    warnings.Add(string.Format(CultureInfo.CurrentCulture,
                        "Slot {0} has a pattern but slot {1} has no material, so the layer will " +
                        "paint the faction default.", SlotLabel(patternSlotId),
                        SlotLabel(materialSlotId)));
                }
            }

            return resolved;
        }

        /// <summary>
        /// Warns about any slot in the recipe that carried content nothing above consumed.
        /// </summary>
        /// <remarks>
        /// <para>The two resolvers above are both <i>pull</i> loops: they walk the slot IDs they
        /// know about and take what is there. That is the right shape — it makes an absent slot
        /// and an empty slot the same thing, which is what the DNA needs. But it has one blind
        /// spot: content in a slot neither loop asks for is dropped in total silence, and the
        /// design still resolves, still validates, and still renders. A pattern in a material
        /// slot produces a perfectly good picture of a hull wearing no pattern.</para>
        ///
        /// <para>That can happen two ways, and both are worth saying out loud. Either the recipe
        /// is malformed, or — far more likely over this feature's life — CCP has added a ninth
        /// slot and our bundled slot table predates it. The second case is the one that matters:
        /// it is the exact moment a user's brand-new design starts rendering subtly wrong, and
        /// without this warning the only symptom is a picture that looks fine to everyone who
        /// hasn't seen it in-game. Cheap check, and it turns a silent wrong answer into a
        /// visible "this design uses something this build doesn't understand".</para>
        /// </remarks>
        private void WarnAboutUnconsumedSlots(List<EsiSkinrSlot> slots, List<string> warnings)
        {
            foreach (EsiSkinrSlot slot in slots)
            {
                EsiSkinrSlotConfiguration? config = slot.Configuration;
                if (config == null)
                    continue;

                bool wantsPattern = slot.Id == SkinrSlot.Pattern ||
                                    slot.Id == SkinrSlot.SecondaryPattern;
                bool wantsMaterial = Array.IndexOf(SkinrSlot.NanocoatingSlots, slot.Id) >= 0 ||
                                     slot.Id == SkinrSlot.PatternMaterial ||
                                     slot.Id == SkinrSlot.SecondaryPatternMaterial;

                if (config.Pattern != null && !wantsPattern)
                {
                    warnings.Add(string.Format(CultureInfo.CurrentCulture,
                        "Slot {0} holds pattern component {1}, but this build treats that slot as " +
                        "{2}, so the pattern was not applied. The bundled SKINR static data may " +
                        "predate this design.", slot.Id, config.Pattern.Id,
                        wantsMaterial ? "a material slot" : "unknown"));
                }

                if (config.Nanocoating != null && !wantsMaterial)
                {
                    warnings.Add(string.Format(CultureInfo.CurrentCulture,
                        "Slot {0} holds material component {1}, but this build treats that slot as " +
                        "{2}, so the material was not applied. The bundled SKINR static data may " +
                        "predate this design.", slot.Id, config.Nanocoating.Id,
                        wantsPattern ? "a pattern slot" : "unknown"));
                }
            }
        }

        /// <summary>
        /// Turns ESI's four projection flags into the shader's <c>customMaskTargets</c> vector,
        /// remapped from ESI slot order into DNA material order.
        /// </summary>
        /// <remarks>
        /// This mirrors <c>EveSOFUtils::CreateMaterialApplicationVector</c>, which builds a
        /// <c>Vector4</c> of 1.0/0.0 from four bools. The remap is the part ESI cannot do for
        /// us: <c>slot1</c> means "the material in ESI slot 1", and on a Minmatar hull that is
        /// the shader's fourth material, not its first.
        /// </remarks>
        private static IReadOnlyList<double> BuildTargetMaterials(EsiSkinrProjection? projection,
            IReadOnlyDictionary<int, int> slotToMaterial)
        {
            double[] targets = new double[SkinrDna.MaterialArity];
            if (projection == null)
                return targets;

            bool[] bySlot =
            {
                projection.Slot1, projection.Slot2, projection.Slot3, projection.Slot4
            };

            for (int i = 0; i < bySlot.Length; i++)
            {
                int position = MaterialPositionFor(i + 1, slotToMaterial);
                targets[position - 1] = bySlot[i] ? 1d : 0d;
            }

            return targets;
        }

        /// <summary>
        /// The two <c>pattern?</c> material arguments, or null when the design has no pattern
        /// layers at all — in which case the command is omitted and the faction's own livery
        /// applies.
        /// </summary>
        private static IReadOnlyList<string>? BuildPatternMaterialTokens(
            List<SkinrResolvedPattern> patterns)
        {
            if (patterns.Count == 0)
                return null;

            // Indexed by layer, not by list position: a design may use the secondary pattern
            // without the primary, and the second argument still has to be the second one.
            string[] tokens = { SkinrDna.EmptyMaterialToken, SkinrDna.EmptyMaterialToken };
            foreach (SkinrResolvedPattern pattern in patterns)
            {
                if (pattern.LayerIndex >= 0 && pattern.LayerIndex < tokens.Length)
                    tokens[pattern.LayerIndex] = pattern.MaterialDnaToken;
            }
            return tokens;
        }

        private int SumPoints(List<SkinrResolvedMaterial> nanocoatings,
            List<SkinrResolvedPattern> patterns)
        {
            int total = 0;

            foreach (SkinrResolvedMaterial material in nanocoatings)
            {
                if (material.Component != null)
                    total += _catalog.GetPointValue(material.Component.CategoryId,
                        material.Component.RarityId);
            }

            foreach (SkinrResolvedPattern pattern in patterns)
            {
                if (pattern.Pattern != null)
                    total += _catalog.GetPointValue(pattern.Pattern.CategoryId,
                        pattern.Pattern.RarityId);
                if (pattern.Material != null)
                    total += _catalog.GetPointValue(pattern.Material.CategoryId,
                        pattern.Material.RarityId);
            }

            return total;
        }

        /// <summary>
        /// ESI slot 1-4 → DNA material position 1-4. Absent means identity: CCP only authors a
        /// map for factions that ship their own hulls.
        /// </summary>
        private static int MaterialPositionFor(int slotId,
            IReadOnlyDictionary<int, int> slotToMaterial) =>
            slotToMaterial.TryGetValue(slotId, out int position) ? position : slotId;

        private string SlotLabel(int slotId)
        {
            SkinrSlotDefinition? definition = _catalog.GetSlot(slotId);
            return definition != null
                ? string.Format(CultureInfo.CurrentCulture, "{0} ({1})",
                    definition.DisplayName.Localized, slotId)
                : slotId.ToString(CultureInfo.CurrentCulture);
        }

        private static IReadOnlyList<double> ToVector(EsiSkinrVector? v, double fallback) =>
            v == null
                ? new[] { fallback, fallback, fallback }
                : new[] { v.X, v.Y, v.Z };

        private static IReadOnlyList<double> ToQuaternion(EsiSkinrQuaternion? q) =>
            q == null
                ? new[] { 0d, 0d, 0d, 1d }
                : new[] { q.X, q.Y, q.Z, q.W };

        private SkinrResolvedDesign Empty(string skinrId, List<string> warnings) =>
            new(skinrId, string.Empty, string.Empty, 0L, 0, null, 0, "normal", string.Empty,
                Array.Empty<SkinrResolvedMaterial>(), Array.Empty<SkinrResolvedPattern>(),
                SkinrSlotConfiguration.Unknown, 0, 0, null, warnings);
    }
}
