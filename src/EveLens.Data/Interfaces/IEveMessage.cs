// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;

namespace EveLens.Common.Interfaces
{
    public interface IEveMessage
    {
        string Title { get; }

        string SenderName { get; }

        DateTime SentDate { get; }

        IEnumerable<string> Recipient { get; }

        string Text { get; }
    }
}