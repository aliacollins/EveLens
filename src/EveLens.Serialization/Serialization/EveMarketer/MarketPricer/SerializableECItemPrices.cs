// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EveLens.Common.Serialization.EveMarketer.MarketPricer
{
    [XmlRoot("exec_api")]
    public sealed class SerializableECItemPrices
    {
        private readonly Collection<SerializableECItemPriceListItem> m_itemPrices;

        public SerializableECItemPrices()
        {
            m_itemPrices = new Collection<SerializableECItemPriceListItem>();
        }

        [XmlArray("marketstat")]
        [XmlArrayItem("type")]
        public Collection<SerializableECItemPriceListItem> ItemPrices => m_itemPrices;
    }
}
