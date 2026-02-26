// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EveLens.Common.SettingsObjects
{
    /// <summary>
    /// Stores a named group of characters for organizing the Overview.
    /// </summary>
    public sealed class CharacterGroupSettings
    {
        private readonly Collection<Guid> m_characterGuids;

        public CharacterGroupSettings()
        {
            Name = string.Empty;
            m_characterGuids = new Collection<Guid>();
        }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlArray("members")]
        [XmlArrayItem("guid")]
        public Collection<Guid> CharacterGuids => m_characterGuids;
    }
}
