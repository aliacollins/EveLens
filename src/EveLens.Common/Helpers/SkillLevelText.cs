// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EveLens.Common.Helpers
{
    /// <summary>
    /// Parses the level token of a pasted skill line. The game and most third-party
    /// tools write roman numerals ("Amarr Titan V"), players type digits — one parser
    /// accepts both, so every import path agrees on what a level looks like.
    /// </summary>
    public static class SkillLevelText
    {
        /// <summary>
        /// Parses "1"–"5" or "I"–"V" (any case) into a level. False for anything else.
        /// </summary>
        public static bool TryParse(string text, out int level)
        {
            level = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string token = text.Trim();
            if (int.TryParse(token, out int numeric))
            {
                if (numeric < 1 || numeric > 5)
                    return false;
                level = numeric;
                return true;
            }

            level = token.ToUpperInvariant() switch
            {
                "I" => 1,
                "II" => 2,
                "III" => 3,
                "IV" => 4,
                "V" => 5,
                _ => 0
            };
            return level != 0;
        }
    }
}
