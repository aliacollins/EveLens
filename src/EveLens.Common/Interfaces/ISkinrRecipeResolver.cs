// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using EveLens.Common.Data;
using EveLens.Common.Serialization.Esi;

namespace EveLens.Common.Interfaces
{
    /// <summary>
    /// Joins an ESI SKINR recipe to the SDE catalog, producing a design with names, points,
    /// tier and a renderable SpaceObjectFactory DNA string.
    /// </summary>
    /// <remarks>
    /// ESI's SKINR routes carry IDs only — CCP's devblog is explicit that this is because names
    /// are localized and belong in static data. So every SKINR feature needs this join: without
    /// it a recipe is eight integers, and with it a recipe is a named, priced, drawable design.
    ///
    /// This lives in <c>EveLens.Common.Interfaces</c> rather than <c>EveLens.Core</c> because it
    /// takes an <see cref="EsiSkinrRecipe"/>, and Core has no project references at all — it is
    /// the leaf of the dependency DAG (Law 3). The neighbouring interfaces here exist for the
    /// same reason: they name types Core cannot see.
    ///
    /// Resolution never throws. A missing catalog, an unpublished hull or a component ID the SDE
    /// has not caught up with all yield a design with
    /// <see cref="SkinrResolvedDesign.Warnings"/> populated and, where the DNA could not be
    /// composed, <see cref="SkinrResolvedDesign.IsRenderable"/> false. A design we cannot draw is
    /// still one we can describe, and a SKINR tab that renders nothing is better than one that
    /// crashes.
    ///
    /// Production: <c>SkinrRecipeResolver</c>, holding one <see cref="SkinrCatalog"/> loaded from
    /// <c>Resources/skinr-catalog.json.gz</c>, reached through
    /// <c>AppServices.SkinrRecipeResolver</c>.
    /// Testing: construct <c>SkinrRecipeResolver</c> directly with a catalog built by
    /// <see cref="SkinrCatalog.FromJson"/>, or substitute this interface.
    /// </remarks>
    public interface ISkinrRecipeResolver
    {
        /// <summary>The catalog backing this resolver. Never null; may be empty.</summary>
        SkinrCatalog Catalog { get; }

        /// <summary>
        /// False when the catalog failed to load. Callers should surface "static data
        /// unavailable" rather than showing bare IDs.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Resolves a recipe against the catalog. Never throws and never returns null; a null
        /// recipe yields an empty design carrying a warning.
        /// </summary>
        SkinrResolvedDesign Resolve(EsiSkinrRecipe? recipe);

        /// <summary>
        /// The component behind an ID, or null if the catalog does not know it — a component
        /// added to live before the bundled SDE build, most likely.
        /// </summary>
        SkinrComponent? GetComponent(int componentId);

        /// <summary>The hull behind a ship type ID, or null.</summary>
        SkinrHull? GetHull(int shipTypeId);

        /// <summary>
        /// The localized display name for a component, falling back to
        /// <c>"Component {id}"</c> so the UI always has something to show.
        /// </summary>
        string GetComponentName(int componentId);

        /// <summary>
        /// Design points for a set of component IDs, for the design editor's running total.
        /// Unknown IDs contribute nothing and are not an error.
        /// </summary>
        int GetDesignPoints(IEnumerable<int> componentIds);
    }
}
