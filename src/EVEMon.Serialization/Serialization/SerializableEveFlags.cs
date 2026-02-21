// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization
{
    [XmlRoot("invFlags")]
    public sealed class SerializableEveFlags
    {
        private readonly Collection<SerializableEveFlagsListItem> m_eveFlags;

        public SerializableEveFlags()
        {
            m_eveFlags = new Collection<SerializableEveFlagsListItem>();
        }

        [XmlArray("flags")]
        [XmlArrayItem("flag")]
        public Collection<SerializableEveFlagsListItem> EVEFlags => m_eveFlags;
    }
}
