// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Attributes;

namespace EveLens.Common.Enumerations
{
    /// <summary>
    /// The status of an EVE mail message.
    /// </summary>
    /// <remarks>The integer value determines the sort order in "Group by...".</remarks>
    public enum EveMailState
    {
        [Header("Inbox")]
        Inbox = 0,

        [Header("Sent Items")]
        SentItem = 1,
    }
}