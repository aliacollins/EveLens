// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using EVEMon.Common.Data;
using EVEMon.Common.Helpers;

namespace EVEMon.Common.Interfaces
{
    public interface ILoadoutInfo
    {
        /// <summary>
        /// Gets or sets the ship of the loadout.
        /// </summary>
        /// <value>
        /// The ship.
        /// </value>
        Item Ship { get; set; }

        /// <summary>
        /// Gets or sets the loadouts.
        /// </summary>
        /// <value>
        /// The loadouts.
        /// </value>
        Collection<Loadout> Loadouts { get; set; }
    }
}