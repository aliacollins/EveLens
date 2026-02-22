// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Constants;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Resolves the current ESI scope preset and custom scopes into the
    /// space-separated scope string used for SSO authentication.
    /// </summary>
    public static class EsiScopeResolver
    {
        /// <summary>
        /// Returns the active ESI scopes as a space-separated string,
        /// based on the current preset in Settings.
        /// </summary>
        public static string GetActiveScopes()
        {
            return string.Join(" ", GetActiveScopesList());
        }

        /// <summary>
        /// Returns the active ESI scopes as a list of individual scope strings,
        /// based on the current preset in Settings. Used to store per-character scopes.
        /// </summary>
        public static List<string> GetActiveScopesList()
        {
            string preset = Settings.EsiScopePreset;

            if (preset == EsiScopePresets.Custom)
            {
                var customScopes = Settings.EsiCustomScopes;
                if (customScopes != null && customScopes.Count > 0)
                    return customScopes.ToList();
            }

            if (preset == EsiScopePresets.FullMonitoring)
                return new List<string>(EsiScopePresets.AllScopes);

            return EsiScopePresets.GetScopesForPreset(preset).ToList();
        }
    }
}
