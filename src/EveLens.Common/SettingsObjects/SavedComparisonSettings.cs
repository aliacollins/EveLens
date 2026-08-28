// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Xml.Serialization;

namespace EveLens.Common.SettingsObjects
{
    /// <summary>
    /// A named set of characters for the Skill Comparison window, so routinely
    /// compared groups can be reloaded in one click (Discussion #105).
    /// </summary>
    public sealed class SavedComparisonSettings
    {
        [XmlAttribute("name")]
        public string Name { get; set; } = string.Empty;

        [XmlArray("characters")]
        [XmlArrayItem("id")]
        public List<long> CharacterIDs { get; set; } = new();
    }
}
