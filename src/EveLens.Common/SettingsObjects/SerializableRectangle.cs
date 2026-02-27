// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EveLens.Common.SettingsObjects
{
    /// <summary>
    /// Represents a serializable version of a rectangle.
    /// No System.Drawing dependency — uses plain int properties.
    /// </summary>
    public sealed class SerializableRectangle
    {
        [XmlAttribute("left")]
        public int Left { get; set; }

        [XmlAttribute("top")]
        public int Top { get; set; }

        [XmlAttribute("width")]
        public int Width { get; set; }

        [XmlAttribute("height")]
        public int Height { get; set; }
    }
}
