// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using EveLens.Common.Collections;
using EveLens.Common.Serialization.Datafiles;

namespace EveLens.Common.Data
{
    /// <summary>
    /// Represents a read-only collection of blueprint groups
    /// </summary>
    public sealed class BlueprintMarketGroupCollection : ReadonlyCollection<BlueprintMarketGroup>
    {
        #region Constructor

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="group">The blueprint market group.</param>
        /// <param name="src">The source.</param>
        internal BlueprintMarketGroupCollection(BlueprintMarketGroup group, ICollection<SerializableBlueprintMarketGroup> src)
            : base(src?.Count ?? 0)
        {
            if (src == null)
                return;

            foreach (SerializableBlueprintMarketGroup subGroup in src)
            {
                Items.Add(new BlueprintMarketGroup(group, subGroup));
            }
        }

        #endregion
    }
}