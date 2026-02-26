// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Enumerations.CCPAPI;
using EveLens.Common.Serialization.Eve;
using EveLens.Common.Serialization.Esi;
using EveLens.Core;
using EveLens.Common.Extensions;
using EveLens.Common.Net;
using EveLens.Common.Services;
using CommonEvents = EveLens.Common.Events;

namespace EveLens.Common.Models
{
    public sealed class UpcomingCalendarEvent
    {
        #region Fields

        private ResponseParams m_attendResponse;
        private readonly CCPCharacter m_ccpCharacter;
        private readonly List<CalendarEventAttendee> m_eventAttendees;
        private readonly long m_eventID;
        private string m_ownerName;
        private bool m_queryPending;

        #endregion


        #region Constructor

        /// <summary>
        /// Constructor from the API.
        /// </summary>
        /// <param name="ccpCharacter">The CCP character.</param>
        /// <param name="src">The source.</param>
        internal UpcomingCalendarEvent(CCPCharacter ccpCharacter, EsiAPICalendarEvent src)
        {
            m_ccpCharacter = ccpCharacter;

            m_attendResponse = null;
            m_eventID = src.EventID;
            OwnerID = src.OwnerID;
            m_ownerName = ServiceLocator.NameResolver.GetName(OwnerID);
            EventTitle = src.EventTitle;
            EventText = src.EventText;
            Duration = src.Duration;
            Importance = src.Importance != 0;
            Response = src.Response;
            EventDate = src.EventDate;
            m_eventAttendees = new List<CalendarEventAttendee>();
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// Gets the owner ID.
        /// </summary>
        public long OwnerID { get; }

        /// <summary>
        /// Gets the name of the owner.
        /// </summary>
        public string OwnerName => m_ownerName.IsEmptyOrUnknown() ? (m_ownerName =
            ServiceLocator.NameResolver.GetName(OwnerID)) : m_ownerName;

        /// <summary>
        /// Gets the event title.
        /// </summary>
        public string EventTitle { get; }

        /// <summary>
        /// Gets the event text.
        /// </summary>
        public string EventText { get; }

        /// <summary>
        /// Gets the duration.
        /// </summary>
        public int Duration { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="UpcomingCalendarEvent"/> is important.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this <see cref="UpcomingCalendarEvent"/> is important; otherwise, <c>false</c>.
        /// </value>
        public bool Importance { get; }

        /// <summary>
        /// Gets the response.
        /// </summary>
        public string Response { get; }

        /// <summary>
        /// Gets the event date.
        /// </summary>
        public DateTime EventDate { get; }

        /// <summary>
        /// Gets the event attendees.
        /// </summary>
        public IEnumerable<CalendarEventAttendee> Attendees => m_eventAttendees;

        #endregion


        #region Querying

        /// <summary>
        /// Gets the attendees.
        /// </summary>
        public void GetEventAttendees()
        {
            if (!m_queryPending && !EsiErrors.IsErrorCountExceeded)
            {
                var apiKey = m_ccpCharacter.Identity.FindAPIKeyWithAccess(
                    ESIAPICharacterMethods.CalendarEventAttendees);
                m_queryPending = true;
                if (apiKey != null)
                    AppServices.APIProviders.CurrentProvider.QueryEsi
                        <EsiAPICalendarEventAttendees>(ESIAPICharacterMethods.
                        CalendarEventAttendees, OnCalendarEventAttendeesDownloaded,
                        new ESIParams(m_attendResponse, apiKey.AccessToken)
                        {
                            ParamOne = m_ccpCharacter.CharacterID,
                            ParamTwo = m_eventID
                        });
            }
        }

        /// <summary>
        /// Processes the queried calendar event attendees.
        /// </summary>
        /// <param name="result">The result.</param>
        private void OnCalendarEventAttendeesDownloaded(EsiResult<EsiAPICalendarEventAttendees>
            result, object ignore)
        {
            m_queryPending = false;
            m_attendResponse = result.Response;
            // Notify if an error occured
            if (m_ccpCharacter.ShouldNotifyError(result, ESIAPICharacterMethods.
                    CalendarEventAttendees))
                AppServices.Notifications.NotifyCharacterCalendarEventAttendeesError(
                    m_ccpCharacter, result);
            if (result.HasData && !result.HasError && result.Result.Count > 0)
            {
                var attendees = result.Result.Select(attendee => new CalendarEventAttendee(
                    attendee));
                m_eventAttendees.Clear();
                m_eventAttendees.AddRange(attendees);
                AppServices.TraceService?.Trace(m_ccpCharacter.Name);
                AppServices.EventAggregator?.Publish(new CommonEvents.CharacterCalendarEventAttendeesDownloadedEvent(m_ccpCharacter));
            }
        }

        #endregion

    }
}
