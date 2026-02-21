// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.EveMarketData.MarketPricer
{
    public sealed class SerializableEMDItemPriceListItem
    {
        [XmlAttribute("typeID")]
        public int ID { get; set; }

        [XmlAttribute("buysell")]
        public string? BuySell { get; set; }

        [XmlAttribute("price")]
        public double Price { get; set; }
    }
}
