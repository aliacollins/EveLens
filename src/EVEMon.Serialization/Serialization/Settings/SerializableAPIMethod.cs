// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Settings
{
    /// <summary>
    /// Represents a serializable version of an API method. Used for settings persistence.
    /// </summary>
    public sealed class SerializableAPIMethod
    {
        [XmlAttribute("name")]
        public string? MethodName { get; set; }

        [XmlAttribute("path")]
        public string? Path { get; set; }
    }
}