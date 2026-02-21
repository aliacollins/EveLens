// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Settings
{
    /// <summary>
    /// Represents a serializable character defined from an uri file
    /// </summary>
    public sealed class SerializableUriCharacter : SerializableSettingsCharacter
    {
        [XmlElement("uri")]
        public string? Address { get; set; }
    }
}