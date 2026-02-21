// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Eve
{
    /// <summary>
    /// Represents a serializable version of upcoming calendar events. Used for querying CCP.
    /// </summary>
    public sealed class SerializableAPIUpcomingCalendarEvents
    {
        private readonly Collection<SerializableUpcomingCalendarEventsListItem> m_upcomingEvents;

        public SerializableAPIUpcomingCalendarEvents()
        {
            m_upcomingEvents = new Collection<SerializableUpcomingCalendarEventsListItem>();
        }

        [XmlArray("upcomingEvents")]
        [XmlArrayItem("upcomingEvent")]
        public Collection<SerializableUpcomingCalendarEventsListItem> UpcomingEvents => m_upcomingEvents;
    }
}
