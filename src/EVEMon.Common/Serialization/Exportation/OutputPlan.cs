// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;
using EVEMon.Common.Serialization.Settings;

namespace EVEMon.Common.Serialization.Exportation
{
    [XmlRoot("plan")]
    public sealed class OutputPlan : SerializablePlan
    {
        [XmlAttribute("revision")]
        public int Revision { get; set; }
    }
}