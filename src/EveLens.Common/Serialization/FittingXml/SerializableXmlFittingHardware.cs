// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;
using EveLens.Common.Data;

namespace EveLens.Common.Serialization.FittingXml
{
    public sealed class SerializableXmlFittingHardware
    {
        [XmlAttribute("qty")]
        public int Quantity { get; set; }

        [XmlAttribute("slot")]
        public string? Slot { get; set; }

        [XmlAttribute("type")]
        public string? ItemXml
        {
            get { return Item?.Name; }
            set
            {
                Item = value != null ? StaticItems.GetItemByName(value) ?? Item.UnknownItem : Item.UnknownItem;
            }
        }

        [XmlIgnore]
        public Item? Item { get; set; }
    }
}