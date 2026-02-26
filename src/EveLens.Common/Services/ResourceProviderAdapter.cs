// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Core.Interfaces;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Adapter that bridges IResourceProvider to Properties.Resources in EveLens.Common.
    /// </summary>
    public sealed class ResourceProviderAdapter : IResourceProvider
    {
        public string DatafilesXSLT => Properties.Resources.DatafilesXSLT;

        public string ChrFactions => Properties.Resources.chrFactions;
    }
}
