// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Enumerations.UISettings
{
    /// <summary>
    /// Controls whether game names (ships, items, skills, market groups) are shown in the
    /// UI language's translated SDE names or in English. Communities differ: Korean players
    /// navigate by the English names they know from the client, killboards, and fitting tools
    /// (Discussion #79), while Chinese players expect the translated names.
    /// </summary>
    public enum GameNameMode
    {
        /// <summary>
        /// Follow the current language's community convention
        /// (LanguageRegistry.LocalizedGameNamesDefault).
        /// </summary>
        Auto = 0,

        /// <summary>
        /// Always use the translated SDE names when available.
        /// </summary>
        Localized = 1,

        /// <summary>
        /// Always use the English names, regardless of UI language.
        /// </summary>
        English = 2
    }
}
