// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Eve
{
    public sealed class SerializableAPIEveFactionalWarfareStats
    {
        private readonly Collection<SerializableEveFactionalWarfareStatsListItem> m_factionalWarfareStats;
        private readonly Collection<SerializableEveFactionWarsListItem> m_factionsWars;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableAPIEveFactionalWarfareStats"/> class.
        /// </summary>
        public SerializableAPIEveFactionalWarfareStats()
        {
            m_factionalWarfareStats = new Collection<SerializableEveFactionalWarfareStatsListItem>();
            m_factionsWars = new Collection<SerializableEveFactionWarsListItem>();
        }

        [XmlElement("totals")]
        public SerializableEveFacWarfareTotals? Totals { get; set; }

        [XmlArray("factions")]
        [XmlArrayItem("faction")]
        public Collection<SerializableEveFactionalWarfareStatsListItem> FactionalWarfareStats => m_factionalWarfareStats;

        [XmlArray("factionWars")]
        [XmlArrayItem("factionWar")]
        public Collection<SerializableEveFactionWarsListItem> FactionWars => m_factionsWars;
    }
}
