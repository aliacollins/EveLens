// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using EveLens.Common.Collections;

namespace EveLens.Common.Data
{
    public sealed class MaterialCollection : ReadonlyCollection<Material>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialCollection"/> class.
        /// </summary>
        /// <param name="materials">The materials.</param>
        internal MaterialCollection(ICollection<Material> materials)
            : base(materials?.Count ?? 0)
        {
            if (materials == null)
                return;

            foreach (Material material in materials)
            {
                Items.Add(material);
            }
        }
    }
}