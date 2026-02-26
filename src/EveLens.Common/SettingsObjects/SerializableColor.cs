// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Drawing;
using System.Xml.Serialization;
using EveLens.Common.Extensions;

namespace EveLens.Common.SettingsObjects
{
    /// <summary>
    /// Represents a color in the settings
    /// </summary>
    public sealed class SerializableColor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableColor"/> class.
        /// </summary>
        public SerializableColor()
        {
            A = 255;
        }

        /// <summary>
        /// Gets or sets a.
        /// </summary>
        /// <value>
        /// a.
        /// </value>
        [XmlAttribute]
        public byte A { get; set; }

        /// <summary>
        /// </summary>
        /// <value>
        /// The r.
        /// </value>
        [XmlAttribute]
        public byte R { get; set; }

        /// <summary>
        /// </summary>
        /// <value>
        /// The g.
        /// </value>
        [XmlAttribute]
        public byte G { get; set; }

        /// <summary>
        /// </summary>
        /// <value>
        /// The b.
        /// </value>
        [XmlAttribute]
        public byte B { get; set; }


        #region Explicit conversion operators with System.Drawing.Color

        /// <summary>
        /// Performs an explicit conversion from <see cref="EveLens.Common.SettingsObjects.SerializableColor" /> to <see cref="System.Drawing.Color" />.
        /// </summary>
        /// <param name="src">The SRC.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">src</exception>
        /// <remarks>
        /// Do not make the conversion operators implicit, there is a bug with XML serialization
        /// </remarks>
        public static explicit operator Color(SerializableColor src)
        {
            src.ThrowIfNull(nameof(src));

            return Color.FromArgb(src.A, src.R, src.G, src.B);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="System.Drawing.Color"/> to <see cref="EveLens.Common.SettingsObjects.SerializableColor"/>.
        /// </summary>
        /// <param name="src">The SRC.</param>
        /// <returns>The result of the conversion.</returns>
        /// <remarks>Do not make the conversion operators implicit, there is a bug with XML serialization</remarks>
        public static explicit operator SerializableColor(Color src)
            => new SerializableColor { A = src.A, R = src.R, G = src.G, B = src.B };

        #endregion
    }
}