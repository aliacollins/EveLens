// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Enumerations.UISettings
{
    /// <summary>
    /// Describes the target platform to allow EveLens to apply different tweaks at runtime
    /// </summary>
    public enum CompatibilityMode
    {
        /// <summary>
        /// Windows and Linux + Wine
        /// </summary>
        Default = 0,

        /// <summary>
        /// Wine
        /// </summary>
        Wine = 1
    }
}