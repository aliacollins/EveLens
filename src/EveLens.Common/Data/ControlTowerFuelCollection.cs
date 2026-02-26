// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using EveLens.Common.Collections;
using EveLens.Common.Serialization.Datafiles;

namespace EveLens.Common.Data
{
    public sealed class ControlTowerFuelCollection : ReadonlyCollection<SerializableControlTowerFuel>
    {     
        /// <summary>
        /// Initializes a new instance of the <see cref="ControlTowerFuelCollection"/> class.
        /// </summary>
        /// <param name="controlTowerFuelInfo">The controlTowerFuelInfo.</param>
        internal ControlTowerFuelCollection(ICollection<SerializableControlTowerFuel> controlTowerFuelInfo)
            : base(controlTowerFuelInfo?.Count ?? 0)
        {
            if (controlTowerFuelInfo == null)
                return;

            foreach (SerializableControlTowerFuel reaction in controlTowerFuelInfo)
            {
                Items.Add(reaction);
            }
        }
    }
}