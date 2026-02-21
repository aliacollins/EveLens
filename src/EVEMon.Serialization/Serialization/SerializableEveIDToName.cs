// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization
{
    [XmlRoot("EveIDToName")]
    public sealed class SerializableEveIDToName
    {
        private readonly Collection<SerializableEveIDToNameListItem> m_entities;

        public SerializableEveIDToName()
        {
            m_entities = new Collection<SerializableEveIDToNameListItem>();
        }

        [XmlArray("entities")]
        [XmlArrayItem("entity")]
        public Collection<SerializableEveIDToNameListItem> Entities => m_entities;
    }
}