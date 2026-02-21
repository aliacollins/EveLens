// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Xml.Serialization;
using EVEMon.Common.Enumerations.UISettings;

namespace EVEMon.Common.SettingsObjects
{
    public sealed class CalendarSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarSettings"/> class.
        /// </summary>
        public CalendarSettings()
        {
            UseOutlookDefaultCalendar = true;
            GoogleEventReminder = GoogleCalendarReminder.Email;
            RemindingInterval = 10;

            EarlyReminding = DateTime.Now.Date.AddHours(8);
            LateReminding = DateTime.Now.Date.AddHours(20);
            LastQueuedSkillOnly = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="CalendarSettings"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        [XmlAttribute("enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the provider.
        /// </summary>
        /// <value>The provider.</value>
        [XmlElement("provider")]
        public CalendarProvider Provider { get; set; }

        /// <summary>
        /// Gets or sets the usage of the MSOutlook default calendar.
        /// </summary>
        /// <value><c>true</c> if use default calendar; otherwise, <c>false</c>.</value>
        [XmlElement("useOutlookDefaultCalendar")]
        public bool UseOutlookDefaultCalendar { get; set; }

        /// <summary>
        /// Gets or sets the MSOutlook custom calendar path.
        /// </summary>
        /// <value>The custom calendar path.</value>
        [XmlElement("outlookCustomCalendarPath")]
        public string OutlookCustomCalendarPath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use reminding.
        /// </summary>
        /// <value><c>true</c> if [use reminding]; otherwise, <c>false</c>.</value>
        [XmlElement("useReminding")]
        public bool UseReminding { get; set; }

        /// <summary>
        /// Interval between remindings, in minutes
        /// </summary>
        [XmlElement("remindingInterval")]
        public int RemindingInterval { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use alternate reminding.
        /// </summary>
        /// <value>
        /// <c>true</c> if to use alternate reminding; otherwise, <c>false</c>.
        /// </value>
        [XmlElement("useAlternateReminding")]
        public bool UseAlternateReminding { get; set; }

        /// <summary>
        /// Gets or sets the early reminding.
        /// </summary>
        /// <value>The early reminding.</value>
        [XmlElement("earlyReminding")]
        public DateTime EarlyReminding { get; set; }

        /// <summary>
        /// Gets or sets the late reminding.
        /// </summary>
        /// <value>The late reminding.</value>
        [XmlElement("lateReminding")]
        public DateTime LateReminding { get; set; }

        /// <summary>
        /// Gets or sets the name of the google calendar.
        /// </summary>
        /// <value>
        /// The name of the google calendar.
        /// </value>
        [XmlElement("googleCalendarName")]
        public string GoogleCalendarName { get; set; }

        /// <summary>
        /// Gets or sets the google reminder.
        /// </summary>
        /// <value>The google reminder.</value>
        [XmlElement("googleEventReminder")]
        public GoogleCalendarReminder GoogleEventReminder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [last queued skill only].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [last queued skill only]; otherwise, <c>false</c>.
        /// </value>
        [XmlElement("lastQueuedSkillOnly")]
        public bool LastQueuedSkillOnly { get; set; }
    }
}