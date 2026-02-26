// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Serialization.Esi;

namespace EveLens.Common.Models
{
    public sealed class PlanetaryLink
    {

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanetaryLink"/> class.
        /// </summary>
        /// <param name="colony">The colony.</param>
        /// <param name="src">The source.</param>
        internal PlanetaryLink(PlanetaryColony colony, EsiPlanetaryLink src)
        {
            Colony = colony;
            SourcePinID = src.SourcePinID;
            DestinationPinID = src.DestinationPinID;
            LinkLevel = src.LinkLevel;
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// Gets or sets the colony.
        /// </summary>
        /// <value>
        /// The colony.
        /// </value>
        public PlanetaryColony Colony { get; private set; }

        /// <summary>
        /// Gets or sets the source pin identifier.
        /// </summary>
        /// <value>
        /// The source pin identifier.
        /// </value>
        public long SourcePinID { get; private set; }

        /// <summary>
        /// Gets or sets the destination pin identifier.
        /// </summary>
        /// <value>
        /// The destination pin identifier.
        /// </value>
        public long DestinationPinID { get; private set; }

        /// <summary>
        /// Gets or sets the link level.
        /// </summary>
        /// <value>
        /// The link level.
        /// </value>
        public short LinkLevel { get; private set; }

        #endregion

    }
}
