// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Serialization.Datafiles;

namespace EveLens.Common.Data
{
    /// <summary>
    /// Represents a readonly ship definition.
    /// </summary>
    public class Ship : Item
    {
        # region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="src">The source.</param>
        internal Ship(MarketGroup group, SerializableItem src)
            : base(group, src)
        {
            Recommendations = new StaticRecommendations<StaticCertificate>();
        }

        #endregion


        # region Public Properties

        /// <summary>
        /// Gets the recommended certificates.
        /// </summary>
        public StaticRecommendations<StaticCertificate> Recommendations { get; }

        #endregion
    }
}