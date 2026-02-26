// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Core.Interfaces
{
    /// <summary>
    /// Provides access to embedded resources (XSLT transforms, static CSV data) without
    /// coupling to a specific <c>Properties.Resources</c> class.
    /// Breaks the Data layer to <c>Properties.Resources</c> dependency (7 call sites, 6 files).
    /// </summary>
    /// <remarks>
    /// Resources are compiled into the assembly at build time. This interface abstracts
    /// the retrieval so that the Core and Data layers do not directly reference the
    /// <c>EveLens.Common.Properties.Resources</c> generated class.
    ///
    /// Production: <c>ResourceProviderAdapter</c> in <c>EveLens.Common/Services/ResourceProviderAdapter.cs</c>
    /// (delegates to <c>Properties.Resources.DatafilesXSLT</c> and <c>Properties.Resources.chrFactions</c>).
    /// Testing: Provide a stub returning test XML/CSV strings, or return empty strings
    /// if resource content is not relevant to the test.
    /// </remarks>
    public interface IResourceProvider
    {
        /// <summary>
        /// Gets the XSLT transform string used for datafile deserialization.
        /// Applied when loading static data XML files (skills, items, blueprints, etc.)
        /// to transform them into the expected schema.
        /// </summary>
        string DatafilesXSLT { get; }

        /// <summary>
        /// Gets the CSV data for NPC factions (<c>chrFactions</c> table).
        /// Used during static data initialization to populate the faction list.
        /// </summary>
        string ChrFactions { get; }
    }
}
