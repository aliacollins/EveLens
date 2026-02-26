// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;
using EveLens.Common.Attributes;

namespace EveLens.Common.Enumerations.UISettings
{
    [XmlRoot("period")]
    public enum UpdatePeriod
    {
        [Header("Never")]
        Never,

        [Header("30 Seconds")]
        Seconds30,

        [Header("1 Minute")]
        Minutes1,

        [Header("2 Minutes")]
        Minutes2,

        [Header("5 Minutes")]
        Minutes5,

        [Header("10 Minutes")]
        Minutes10,

        [Header("15 Minutes")]
        Minutes15,

        [Header("30 Minutes")]
        Minutes30,

        [Header("1 Hour")]
        Hours1,

        [Header("2 Hours")]
        Hours2,

        [Header("3 Hours")]
        Hours3,

        [Header("6 Hours")]
        Hours6,

        [Header("12 Hours")]
        Hours12,

        [Header("Day")]
        Day,

        [Header("Week")]
        Week
    }
}