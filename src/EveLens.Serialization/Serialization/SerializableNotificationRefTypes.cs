// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EveLens.Common.Serialization
{
    [XmlRoot("NotificationRefTypes")]
    public sealed class SerializableNotificationRefTypes
    {
        private readonly Collection<SerializableNotificationRefTypesListItem> m_types;

        public SerializableNotificationRefTypes()
        {
            m_types = new Collection<SerializableNotificationRefTypesListItem>();
        }

        [XmlArray("refTypes")]
        [XmlArrayItem("refType")]
        public Collection<SerializableNotificationRefTypesListItem> Types => m_types;
    }
}