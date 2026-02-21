// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Drawing;
using System.Windows.Forms;

namespace EVEMon.Common.CustomEventArgs
{
    public sealed class DaySelectedEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="datetime">The datetime.</param>
        /// <param name="dateTimeIsSameMonthAsPrevious">if set to <c>true</c> [date time is same month as previous].</param>
        /// <param name="mouse">The <see cref="System.Windows.Forms.MouseEventArgs" /> instance containing the event data.</param>
        /// <param name="location">The location.</param>
        public DaySelectedEventArgs(DateTime datetime, bool dateTimeIsSameMonthAsPrevious, MouseEventArgs mouse, Point location)
        {
            DateTime = datetime;
            DateTimeIsSameMonthAsPrevious = dateTimeIsSameMonthAsPrevious;
            Mouse = mouse;
            Location = location;
        }

        /// <summary>
        /// Gets the date time.
        /// </summary>
        /// <value>The date time.</value>
        public DateTime DateTime { get; }

        /// <summary>
        /// Gets a value indicating whether this date is in the same month as the previous.
        /// </summary>
        /// <value>
        /// <c>true</c> if this date is in the same month as the previous; otherwise, <c>false</c>.
        /// </value>
        public bool DateTimeIsSameMonthAsPrevious { get; }

        /// <summary>
        /// Gets the mouse.
        /// </summary>
        /// <value>The mouse.</value>
        public MouseEventArgs Mouse { get; }

        /// <summary>
        /// Gets the location.
        /// </summary>
        /// <value>The location.</value>
        public Point Location { get; }
    }
}
