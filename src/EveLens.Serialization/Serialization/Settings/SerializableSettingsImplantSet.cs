// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;
using EveLens.Common.Enumerations;

namespace EveLens.Common.Serialization.Settings
{
    /// <summary>
    /// Represents the set of attributes enhancers
    /// </summary>
    public sealed class SerializableSettingsImplantSet
    {
        public SerializableSettingsImplantSet()
        {
            Name = "Custom";
            Intelligence = ImplantSlots.None.ToString();
            Perception = ImplantSlots.None.ToString();
            Willpower = ImplantSlots.None.ToString();
            Charisma = ImplantSlots.None.ToString();
            Memory = ImplantSlots.None.ToString();
            Slot6 = ImplantSlots.None.ToString();
            Slot7 = ImplantSlots.None.ToString();
            Slot8 = ImplantSlots.None.ToString();
            Slot9 = ImplantSlots.None.ToString();
            Slot10 = ImplantSlots.None.ToString();
        }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlElement("intelligence")]
        public string Intelligence { get; set; }

        [XmlElement("memory")]
        public string Memory { get; set; }

        [XmlElement("willpower")]
        public string Willpower { get; set; }

        [XmlElement("perception")]
        public string Perception { get; set; }

        [XmlElement("charisma")]
        public string Charisma { get; set; }

        [XmlElement("slot6")]
        public string Slot6 { get; set; }

        [XmlElement("slot7")]
        public string Slot7 { get; set; }

        [XmlElement("slot8")]
        public string Slot8 { get; set; }

        [XmlElement("slot9")]
        public string Slot9 { get; set; }

        [XmlElement("slot10")]
        public string Slot10 { get; set; }

        public override string ToString() => Name;
    }
}