// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Xml.Serialization;
using EveLens.Common.Extensions;
using EveLens.Common.Models;

namespace EveLens.Common.SettingsObjects
{
    public sealed class MonitoredCharacterSettings
    {
        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        public MonitoredCharacterSettings()
        {
            Settings = new CharacterUISettings();
        }

        /// <summary>
        /// Creation for new settings for this character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <exception cref="System.ArgumentNullException">character</exception>
        public MonitoredCharacterSettings(Character character)
        {
            character.ThrowIfNull(nameof(character));

            CharacterGuid = character.Guid;
            Name = character.Name;
            Settings = character.UISettings;
        }

        /// <summary>
        /// Gets or sets the character GUID.
        /// </summary>
        /// <value>The character GUID.</value>
        [XmlAttribute("characterGuid")]
        public Guid CharacterGuid { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the settings.
        /// </summary>
        /// <value>The settings.</value>
        [XmlElement("settings")]
        public CharacterUISettings Settings { get; set; }
    }
}