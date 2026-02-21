// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.Sales
{
    public sealed class MineralPrice
    {
        /// <summary>
        /// Gets or sets the name of the mineral.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the price of the mineral.
        /// </summary>
        /// <value>The price.</value>
        public decimal Price { get; set; }
    }
}