// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Enumeration of the attributes in Eve. None is -1, other range from 0 to 4,
    /// matching the attributes order on the ingame character sheets.
    /// </summary>
    public enum EveAttribute
    {
        [XmlEnum("none")]
        None = -1,

        [XmlEnum("intelligence")]
        Intelligence = 0,

        [XmlEnum("perception")]
        Perception = 1,

        [XmlEnum("charisma")]
        Charisma = 2,

        [XmlEnum("willpower")]
        Willpower = 3,

        [XmlEnum("memory")]
        Memory = 4
    }
}