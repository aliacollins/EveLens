// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Exportation
{
    /// <summary>
    /// A serialization class designed for HTML exportation.
    /// </summary>
    [XmlRoot("outputCharacter")]
    public sealed class OutputCharacter
    {
        private readonly Collection<OutputAttributeEnhancer> m_attributeEnhancers;
        private readonly Collection<OutputSkillGroup> m_skillGroups;

        public OutputCharacter()
        {
            m_attributeEnhancers = new Collection<OutputAttributeEnhancer>();
            m_skillGroups = new Collection<OutputSkillGroup>();
        }

        [XmlAttribute("name")]
        public string? Name { get; set; }

        [XmlAttribute("characterID")]
        public long CharacterID { get; set; }

        [XmlElement("race")]
        public string? Race { get; set; }

        [XmlElement("bloodLine")]
        public string? BloodLine { get; set; }

        [XmlElement("ancestry")]
        public string? Ancestry { get; set; }

        [XmlElement("gender")]
        public string? Gender { get; set; }

        [XmlElement("corporationName")]
        public string? CorporationName { get; set; }

        [XmlElement("balance")]
        public string? Balance { get; set; }

        [XmlElement("birthday")]
        public string? Birthday { get; set; }

        [XmlElement("intelligence")]
        public float Intelligence { get; set; }

        [XmlElement("charisma")]
        public float Charisma { get; set; }

        [XmlElement("perception")]
        public float Perception { get; set; }

        [XmlElement("memory")]
        public float Memory { get; set; }

        [XmlElement("willpower")]
        public float Willpower { get; set; }

        [XmlArray("attributeEnhancers")]
        [XmlArrayItem("implant")]
        public Collection<OutputAttributeEnhancer> AttributeEnhancers => m_attributeEnhancers;

        [XmlArray("skills")]
        [XmlArrayItem("skillGroup")]
        public Collection<OutputSkillGroup> SkillGroups => m_skillGroups;
    }
}