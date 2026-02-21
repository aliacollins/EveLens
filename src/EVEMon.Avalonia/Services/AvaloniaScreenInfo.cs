// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using EVEMon.Core.Interfaces;

namespace EVEMon.Avalonia.Services
{
    /// <summary>
    /// Avalonia implementation of <see cref="IScreenInfo"/>.
    /// Uses Avalonia's Screens API.
    /// </summary>
    internal sealed class AvaloniaScreenInfo : IScreenInfo
    {
        public (int X, int Y, int Width, int Height) PrimaryWorkingArea
        {
            get
            {
                var screen = GetPrimaryScreen();
                if (screen == null)
                    return (0, 0, 1920, 1080);

                var area = screen.WorkingArea;
                return (area.X, area.Y, area.Width, area.Height);
            }
        }

        public IReadOnlyList<(int X, int Y, int Width, int Height)> AllScreenBounds
        {
            get
            {
                var screens = GetScreens();
                if (screens == null)
                    return new List<(int, int, int, int)> { (0, 0, 1920, 1080) }.AsReadOnly();

                return screens.All
                    .Select(s => (s.Bounds.X, s.Bounds.Y, s.Bounds.Width, s.Bounds.Height))
                    .ToList()
                    .AsReadOnly();
            }
        }

        private static global::Avalonia.Platform.Screen? GetPrimaryScreen()
        {
            return GetScreens()?.Primary;
        }

        private static Screens? GetScreens()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.MainWindow?.Screens;
            return null;
        }
    }
}
