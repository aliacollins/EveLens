// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EveLens.Common.Serialization.EveMarketer.MarketPricer
{
    public sealed class SerializableECItemPriceListItem
    {
        [XmlAttribute("id")]
        public int ID { get; set; }

        [XmlElement("sell")]
        public SerializableECItemPriceItem? Prices { get; set; }
    }
}
