// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EveLens.Common.Serialization.Eve
{
    public sealed class SerializableCharacterJumpClone
    {
        [XmlAttribute("jumpCloneID")]
        public long JumpCloneID { get; set; }

        [XmlAttribute("typeID")]
        public int TypeID { get; set; }

        [XmlAttribute("locationID")]
        public long LocationID { get; set; }

        [XmlAttribute("cloneName")]
        public string? CloneNameXml
        {
            get { return CloneName; }
            set
            {
                CloneName = value;
            }
        }

        [XmlIgnore]
        public string? CloneName { get; set; }
    }
}
