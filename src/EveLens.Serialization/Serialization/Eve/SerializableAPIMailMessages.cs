// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EveLens.Common.Serialization.Eve
{
    /// <summary>
    /// Represents a serializable version of a characters' eve mail messages headers. Used for querying CCP.
    /// </summary>
    public sealed class SerializableAPIMailMessages
    {
        private readonly Collection<SerializableMailMessagesListItem> m_messages;

        public SerializableAPIMailMessages()
        {
            m_messages = new Collection<SerializableMailMessagesListItem>();
        }

        [XmlArray("messages")]
        [XmlArrayItem("message")]
        public Collection<SerializableMailMessagesListItem> Messages => m_messages;
    }
}