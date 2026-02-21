// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using EVEMon.Common.Data;
using EVEMon.Common.Interfaces;

namespace EVEMon.Common.Helpers
{
    public sealed class LoadoutInfo : ILoadoutInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoadoutInfo"/> class.
        /// </summary>
        public LoadoutInfo()
        {
            Ship = Item.UnknownItem;
            Loadouts = new Collection<Loadout>();
        }

        /// <summary>
        /// Gets or sets the ship of the loadout.
        /// </summary>
        /// <value>
        /// The ship.
        /// </value>
        public Item Ship { get; set; }

        /// <summary>
        /// Gets or sets the loadouts.
        /// </summary>
        /// <value>
        /// The items.
        /// </value>
        public Collection<Loadout> Loadouts { get; set; }
    }
}