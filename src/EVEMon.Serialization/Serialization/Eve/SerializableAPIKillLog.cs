// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Eve
{
    /// <summary>
    /// Represents a serializable version of a kill log. Used for querying CCP.
    /// </summary>
    public sealed class SerializableAPIKillLog
    {
        private readonly Collection<SerializableKillLogListItem> m_kills;

        public SerializableAPIKillLog()
        {
            m_kills = new Collection<SerializableKillLogListItem>();
        }

        [XmlArray("ArrayOfEsiKillLogListItem")]
        [XmlArrayItem("EsiKillLogListItem")]
        public Collection<SerializableKillLogListItem> Kills => m_kills;
    }
}
