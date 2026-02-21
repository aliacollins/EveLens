// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Eve
{
    public sealed class SerializableAPIRefTypes
    {
        private readonly Collection<SerializableRefTypesListItem> m_refTypes;

        public SerializableAPIRefTypes()
        {
            m_refTypes = new Collection<SerializableRefTypesListItem>();
        }

        [XmlArray("refTypes")]
        [XmlArrayItem("refType")]
        public Collection<SerializableRefTypesListItem> RefTypes => m_refTypes;
    }
}
