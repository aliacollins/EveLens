// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// No-op fallback <see cref="IScreenInfo"/> that returns a default 1920x1080 screen.
    /// In production, the UI layer (Avalonia) replaces this via <c>AppServices.SetScreenInfo()</c>.
    /// </summary>
    internal sealed class NullScreenInfo : IScreenInfo
    {
        public (int X, int Y, int Width, int Height) PrimaryWorkingArea => (0, 0, 1920, 1080);

        public IReadOnlyList<(int X, int Y, int Width, int Height)> AllScreenBounds { get; } =
            new[] { (0, 0, 1920, 1080) };
    }
}
