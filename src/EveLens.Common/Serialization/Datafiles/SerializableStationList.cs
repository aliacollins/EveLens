// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Serialization.Eve;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EveLens.Common.Serialization.Datafiles
{
    /// <summary>
    /// Represents a serializable version of the stations list. Used for data files only.
    /// </summary>
    public sealed class SerializableStationList
    {
        private readonly Collection<SerializableOutpost> m_stations;

        public SerializableStationList()
        {
            m_stations = new Collection<SerializableOutpost>();
        }

        [XmlArray("stations")]
        [XmlArrayItem("station")]
        public Collection<SerializableOutpost> Stations => m_stations;
    }
}
