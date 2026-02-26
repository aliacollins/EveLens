// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;
using EveLens.Common.Serialization.Settings;

namespace EveLens.Common.Serialization.Exportation
{
    [XmlRoot("plans")]
    public sealed class OutputPlans
    {
        private readonly Collection<SerializablePlan> m_plans;

        public OutputPlans()
        {
            m_plans = new Collection<SerializablePlan>();
        }

        [XmlAttribute("revision")]
        public int Revision { get; set; }

        [XmlElement("plan")]
        public Collection<SerializablePlan> Plans => m_plans;
    }
}