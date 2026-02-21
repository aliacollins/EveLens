// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Xml.Serialization;
using EVEMon.Common.Enumerations;

namespace EVEMon.Common.Serialization.Exportation
{
    /// <summary>
    /// A serialization class designed for HTML exportation.
    /// </summary>
    public sealed class OutputAttributeEnhancer
    {
        [XmlAttribute("attribute")]
        public ImplantSlots Attribute { get; set; }

        [XmlAttribute("description")]
        public string? Description { get; set; }

        [XmlAttribute("bonus")]
        public long Bonus { get; set; }

        [XmlAttribute("name")]
        public string? Name { get; set; }
    }
}