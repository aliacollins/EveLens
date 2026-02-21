// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.Core.Enumerations
{
    /// <summary>
    /// Severity levels for diagnostic trace messages.
    /// </summary>
    public enum TraceLevel
    {
        /// <summary>Verbose diagnostic detail, useful during development.</summary>
        Debug = 0,
        /// <summary>Normal operational messages.</summary>
        Info = 1,
        /// <summary>Non-critical issues that may require attention.</summary>
        Warning = 2,
        /// <summary>Errors that affect functionality.</summary>
        Error = 3
    }
}
