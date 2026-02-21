// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using EVEMon.Common.Serialization.Eve;

namespace EVEMon.Common.Serialization.Settings
{
    /// <summary>
    /// Represents a base for character serialization in the settings.
    /// </summary>
    [JsonDerivedType(typeof(SerializableCCPCharacter), "ccp")]
    [JsonDerivedType(typeof(SerializableUriCharacter), "uri")]
    public class SerializableSettingsCharacter : SerializableCharacterSheetBase
    {
        [XmlAttribute("guid")]
        public Guid Guid { get; set; }

        [XmlAttribute("label")]
        public string? Label { get; set; }

        [XmlElement("implants")]
        public SerializableImplantSetCollection? ImplantSets { get; set; }
    }
}
