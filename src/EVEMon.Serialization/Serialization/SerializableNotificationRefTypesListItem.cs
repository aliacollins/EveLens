// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EVEMon.Common.Serialization
{
    public sealed class SerializableNotificationRefTypesListItem
    {
        [XmlAttribute("refTypeID")]
        public int TypeID { get; set; }

        [XmlAttribute("refTypeCode")]
        public string? TypeCode { get; set; }

        [XmlAttribute("refTypeName")]
        public string? TypeName { get; set; }

        [XmlAttribute("subjectLayout")]
        public string? SubjectLayout { get; set; }

        [XmlAttribute("textLayout")]
        public string? TextLayout { get; set; }
    }
}
