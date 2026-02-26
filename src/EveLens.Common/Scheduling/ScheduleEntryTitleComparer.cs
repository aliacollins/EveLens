// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;

namespace EveLens.Common.Scheduling
{
    public class ScheduleEntryTitleComparer : Comparer<ScheduleEntry>
    {
        public override int Compare(ScheduleEntry e1, ScheduleEntry e2)
        {
            if (e1 != null && e2 != null)
                return string.Compare(e1.Title, e2.Title, StringComparison.CurrentCulture);

            return 0;
        }
    }
}