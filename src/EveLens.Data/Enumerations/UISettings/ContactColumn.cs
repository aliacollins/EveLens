// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;
using EveLens.Common.Attributes;

namespace EveLens.Common.Enumerations.UISettings
{
    /// <summary>
    /// Enumeration for the contact columns.
    /// </summary>
    public enum ContactColumn
    {
        None = -1,

        [Header("Name")]
        [Description("Contact Name")]
        Name = 0,

        [Header("Standing")]
        [Description("Standing")]
        Standing = 1,

        [Header("Group")]
        [Description("Contact Group")]
        Group = 2,

        [Header("In Watchlist")]
        [Description("In Watchlist")]
        InWatchlist = 3
    }
}
