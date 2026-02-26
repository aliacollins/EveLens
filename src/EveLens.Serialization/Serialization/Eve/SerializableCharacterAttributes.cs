// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Xml.Serialization;

namespace EveLens.Common.Serialization.Eve
{
    /// <summary>
    /// Represents the character's attributes
    /// </summary>
    public sealed class SerializableCharacterAttributes
    {
        public SerializableCharacterAttributes()
        {
            Intelligence = Memory = Perception = Charisma = Willpower = 1;
        }

        [XmlElement("intelligence")]
        public long Intelligence { get; set; }

        [XmlElement("memory")]
        public long Memory { get; set; }

        [XmlElement("perception")]
        public long Perception { get; set; }

        [XmlElement("willpower")]
        public long Willpower { get; set; }

        [XmlElement("charisma")]
        public long Charisma { get; set; }
    }
}