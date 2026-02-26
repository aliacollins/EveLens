// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Drawing;
using System.Xml.Serialization;

namespace EveLens.Common.SettingsObjects
{
    public sealed class SchedulerUISettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SchedulerUISettings"/> class.
        /// </summary>
        public SchedulerUISettings()
        {
            TextColor = (SerializableColor)Color.White;
            BlockingColor = (SerializableColor)Color.Red;
            SimpleEventGradientStart = (SerializableColor)Color.Blue;
            SimpleEventGradientEnd = (SerializableColor)Color.LightBlue;
            RecurringEventGradientStart = (SerializableColor)Color.Green;
            RecurringEventGradientEnd = (SerializableColor)Color.LightGreen;
        }

        /// <summary>
        /// Gets or sets the color of the text.
        /// </summary>
        /// <value>The color of the text.</value>
        [XmlElement("textColor")]
        public SerializableColor TextColor { get; set; }

        /// <summary>
        /// Gets or sets the color of the blocking.
        /// </summary>
        /// <value>The color of the blocking.</value>
        [XmlElement("blockColor")]
        public SerializableColor BlockingColor { get; set; }

        /// <summary>
        /// Gets or sets the recurring event gradient start.
        /// </summary>
        /// <value>The recurring event gradient start.</value>
        [XmlElement("recurringEventGradientStart")]
        public SerializableColor RecurringEventGradientStart { get; set; }

        /// <summary>
        /// Gets or sets the recurring event gradient end.
        /// </summary>
        /// <value>The recurring event gradient end.</value>
        [XmlElement("recurringEventGradientEnd")]
        public SerializableColor RecurringEventGradientEnd { get; set; }

        /// <summary>
        /// Gets or sets the simple event gradient start.
        /// </summary>
        /// <value>The simple event gradient start.</value>
        [XmlElement("simpleEventGradientStart")]
        public SerializableColor SimpleEventGradientStart { get; set; }

        /// <summary>
        /// Gets or sets the simple event gradient end.
        /// </summary>
        /// <value>The simple event gradient end.</value>
        [XmlElement("simpleEventGradientEnd")]
        public SerializableColor SimpleEventGradientEnd { get; set; }
    }
}