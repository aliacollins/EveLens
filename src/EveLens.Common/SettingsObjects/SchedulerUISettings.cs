// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

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
            // Use raw ARGB values instead of System.Drawing.Color which is
            // not supported on Linux/macOS (.NET 6+).
            TextColor = new SerializableColor { A = 255, R = 255, G = 255, B = 255 }; // White
            BlockingColor = new SerializableColor { A = 255, R = 255, G = 0, B = 0 }; // Red
            SimpleEventGradientStart = new SerializableColor { A = 255, R = 0, G = 0, B = 255 }; // Blue
            SimpleEventGradientEnd = new SerializableColor { A = 255, R = 173, G = 216, B = 230 }; // LightBlue
            RecurringEventGradientStart = new SerializableColor { A = 255, R = 0, G = 128, B = 0 }; // Green
            RecurringEventGradientEnd = new SerializableColor { A = 255, R = 144, G = 238, B = 144 }; // LightGreen
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