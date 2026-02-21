// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;

namespace EVEMon.Common.Enumerations.UISettings
{
    public enum GoogleCalendarReminder
    {
        [Description("Email")]
        Email,

        [Description("Pop-up")]
        PopUp,

        [Description("SMS")]
        Sms,
    }
}