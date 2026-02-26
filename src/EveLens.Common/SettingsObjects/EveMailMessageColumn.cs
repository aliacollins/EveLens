// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;
using EveLens.Common.Attributes;

namespace EveLens.Common.SettingsObjects
{
    /// <summary>
    /// Represents the available column types.
    /// </summary>
    public enum EveMailMessageColumn
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
        Title = 2,

        [Header("To")]
        [Description("To ( Recipient(s) )")]
        ToCharacters = 3,

        [Header("To Corp Or Alliance")]
        [Description("To Corp Or Alliance")]
        ToCorpOrAlliance = 4,

        [Header("To Mailing List")]
        [Description("To Mailing List(s)")]
        ToMailingList = 5,
    }
}