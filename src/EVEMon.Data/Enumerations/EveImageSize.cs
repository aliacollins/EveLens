// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Represents the image size of an EVE icon.
    /// </summary>
    [Flags]
    public enum EveImageSize
    {
        None = 0,
        x0 = 1,
        x16 = 16,
        x32 = 32,
        x64 = 64,
        x128 = 128,
        x256 = 256
    }
}