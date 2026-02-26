// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Enumerations
{
    /// <summary>
    /// The available formats for a character exportation.
    /// </summary>
    public enum CharacterSaveFormat
    {
        None = 0,
        Text = 1,
        EFTCHR = 2,
        HTML = 3,
        EveLensXML = 4,
        CCPXML = 5,
        PNG = 6,
    }
}