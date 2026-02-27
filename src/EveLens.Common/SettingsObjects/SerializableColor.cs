// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Runtime.InteropServices;
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


        #region Color conversion helpers

        /// <summary>
        /// Converts to a (A, R, G, B) tuple. Platform-safe replacement for System.Drawing.Color operators.
        /// </summary>
        public (byte A, byte R, byte G, byte B) ToArgb() => (A, R, G, B);

        /// <summary>
        /// Creates from ARGB components. Platform-safe replacement for System.Drawing.Color operators.
        /// </summary>
        public static SerializableColor FromArgb(byte a, byte r, byte g, byte b)
            => new SerializableColor { A = a, R = r, G = g, B = b };

        #endregion
    }
}