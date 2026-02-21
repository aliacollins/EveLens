// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.Common.Enumerations.UISettings
{
    /// <summary>
    /// Represents what is displayed in the main window title.
    /// </summary>
    public enum MainWindowTitleFormat
    {
        Default = 0,
        NextCharToFinish = 1,
        SelectedChar = 2,
        AllCharacters = 3,
        AllCharactersButSelectedOneAhead = 4
    }
}