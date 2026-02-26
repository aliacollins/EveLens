// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Xml.Serialization;

namespace EveLens.Common.Serialization.Settings
{
    /// <summary>
    /// Represents an API method and the last time we updated it from CCP.
    /// </summary>
    public sealed class SerializableAPIUpdate
    {
        [XmlAttribute("method")]
        public string? Method { get; set; }

        [XmlAttribute("time")]
        public DateTime Time { get; set; }

        [XmlAttribute("etag")]
        public string? ETag { get; set; }

        [XmlAttribute("cachedUntil")]
        public DateTime CachedUntil { get; set; }
    }
}