// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Extensions;
using EveLens.Common.Serialization.Esi;
using EveLens.Core;

namespace EveLens.Common.Models
{
    public sealed class CalendarEventAttendee
    {
        private string m_characterName;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarEventAttendee"/> class.
        /// </summary>
        /// <param name="src">The SRC.</param>
        internal CalendarEventAttendee(EsiCalendarEventAttendeeListItem src)
        {
            CharacterID = src.CharacterID;
            m_characterName = ServiceLocator.NameResolver.GetName(src.CharacterID);
            Response = src.Response;
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// Gets the character ID.
        /// </summary>
        public long CharacterID { get; }

        /// <summary>
        /// Gets the name of the character.
        /// </summary>
        public string CharacterName => (m_characterName.IsEmptyOrUnknown()) ?
            (m_characterName = ServiceLocator.NameResolver.GetName(CharacterID)) : m_characterName;

        /// <summary>
        /// Gets the response.
        /// </summary>
        public string Response { get; }

        #endregion

    }
}
