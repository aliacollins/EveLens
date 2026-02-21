// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;
using EVEMon.Common.Constants;

namespace EVEMon.Common.Serialization.Eve
{
    public sealed class SerializableKillLogAttackersListItem : SerializableCharacterListItem
    {
        [XmlAttribute("damageDone")]
        public int DamageDone { get; set; }

        [XmlAttribute("finalBlow")]
        public bool FinalBlow { get; set; }

        [XmlAttribute("securityStatus")]
        public double SecurityStatus { get; set; }

        [XmlAttribute("weaponTypeID")]
        public int WeaponTypeID { get; set; }


        [XmlIgnore]
        public string WeaponTypeName { get; set; } = string.Empty;
    }
}