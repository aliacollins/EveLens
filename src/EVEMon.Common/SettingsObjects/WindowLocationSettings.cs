// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Drawing;
using System.Xml.Serialization;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.SettingsObjects
{
    /// <summary>
    /// Represents a serializable version of a rectangle.
    /// </summary>
    [Serializable]
    [XmlRoot("location")]
    public sealed class WindowLocationSettings
    {
        [XmlAttribute("left")]
        public int Left { get; set; }

        [XmlAttribute("top")]
        public int Top { get; set; }

        [XmlAttribute("width")]
        public int Width { get; set; }

        [XmlAttribute("height")]
        public int Height { get; set; }

        /// <summary>
        /// Performs an explicit conversion from <see cref="WindowLocationSettings" /> to <see cref="System.Drawing.Rectangle" />.
        /// </summary>
        /// <param name="rect">The rect.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">rect</exception>
        /// <remarks>
        /// Do not make the conversion operators implicit, there is a bug with XML serialization
        /// </remarks>
        public static explicit operator Rectangle(WindowLocationSettings rect)
        {
            rect.ThrowIfNull(nameof(rect));

            return new Rectangle(rect.Left, rect.Top, rect.Width, rect.Height);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="System.Drawing.Rectangle"/> to <see cref="WindowLocationSettings"/>.
        /// </summary>
        /// <param name="rect">The rect.</param>
        /// <returns>The result of the conversion.</returns>
        /// <remarks>Do not make the conversion operators implicit, there is a bug with XML serialization</remarks>
        public static explicit operator WindowLocationSettings(Rectangle rect)
            => new WindowLocationSettings { Left = rect.Left, Top = rect.Top, Width = rect.Width, Height = rect.Height };
    }
}