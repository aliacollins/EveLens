// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;
using EveLens.Common.Enumerations;
using EveLens.Common.Enumerations.UISettings;

namespace EveLens.Common.SettingsObjects
{
    public sealed class PlanSorting
    {
        public PlanSorting()
        {
            Criteria = PlanEntrySort.None;
            Order = ThreeStateSortOrder.None;
        }

        [XmlAttribute("criteria")]
        public PlanEntrySort Criteria { get; set; }

        [XmlAttribute("order")]
        public ThreeStateSortOrder Order { get; set; }

        [XmlAttribute("groupByPriority")]
        public bool GroupByPriority { get; set; }
    }
}