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
    /// Settings for the Skill Farm Dashboard.
    /// Stores which characters are designated as farm characters
    /// and their extraction preferences.
    /// </summary>
    public sealed class SkillFarmSettings
    {
        public SkillFarmSettings()
        {
            FarmCharacters = new Collection<SkillFarmCharacterSettings>();
        }

        /// <summary>Minimum SP threshold for extraction (default: 5,000,000).</summary>
        [XmlElement("defaultThreshold")]
        public long DefaultExtractionThreshold { get; set; } = 5_000_000;

        /// <summary>Characters designated as skill farm characters.</summary>
        [XmlArray("farmCharacters")]
        [XmlArrayItem("character")]
        public Collection<SkillFarmCharacterSettings> FarmCharacters { get; set; }
    }

    /// <summary>
    /// Per-character skill farm settings.
    /// </summary>
    public sealed class SkillFarmCharacterSettings
    {
        /// <summary>Character GUID (matches Character.Guid).</summary>
        [XmlAttribute("guid")]
        public Guid CharacterGuid { get; set; }

        /// <summary>SP threshold below which we don't extract (default: 5,000,000).</summary>
        [XmlElement("threshold")]
        public long ExtractionThreshold { get; set; } = 5_000_000;

        /// <summary>User notes (e.g., "PI alt", "cyno alt").</summary>
        [XmlElement("notes")]
        public string Notes { get; set; } = string.Empty;
    }
}
