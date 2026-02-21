// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;
using EVEMon.Common.Attributes;

namespace EVEMon.Common.SettingsObjects
{
    /// <summary>
    /// Represents the available column types.
    /// </summary>
    public enum EveNotificationColumn
    {
        None = -1,

        [Header("Received")]
        [Description("Received Date")]
        SentDate = 0,

        [Header("From")]
        [Description("From ( Sender )")]
        SenderName = 1,

        [Header("Subject")]
        [Description("Subject")]
        Type = 2,
    }
}