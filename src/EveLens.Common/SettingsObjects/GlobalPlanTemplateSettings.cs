// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EveLens.Common.SettingsObjects
{
    public sealed class GlobalPlanTemplateEntry
    {
        [XmlAttribute("skillID")]
        public int SkillID { get; set; }

        [XmlAttribute("skillName")]
        public string SkillName { get; set; } = string.Empty;

        [XmlAttribute("level")]
        public int Level { get; set; }
    }

    public sealed class GlobalPlanTemplate
    {
        [XmlAttribute("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        [XmlAttribute("name")]
        public string Name { get; set; } = string.Empty;

        [XmlAttribute("description")]
        public string Description { get; set; } = string.Empty;

        [XmlAttribute("created")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [XmlArray("subscribedCharacters")]
        [XmlArrayItem("guid")]
        public List<Guid> SubscribedCharacterGuids { get; set; } = new();

        [XmlArray("entries")]
        [XmlArrayItem("entry")]
        public List<GlobalPlanTemplateEntry> Entries { get; set; } = new();
    }
}
