// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Diagnostics;
using Avalonia.Controls.ApplicationLifetimes;
using EVEMon.Core.Interfaces;

namespace EVEMon.Avalonia.Services
{
    /// <summary>
    /// Avalonia implementation of <see cref="IApplicationLifecycle"/>.
    /// Wraps <see cref="IClassicDesktopStyleApplicationLifetime"/> for exit/restart.
    /// </summary>
    internal sealed class AvaloniaApplicationLifecycle : IApplicationLifecycle
    {
        private readonly IClassicDesktopStyleApplicationLifetime _lifetime;

        public AvaloniaApplicationLifecycle(IClassicDesktopStyleApplicationLifetime lifetime)
        {
            _lifetime = lifetime;
        }

        public void Exit()
        {
            _lifetime.Shutdown();
        }

        public void Restart()
        {
            string? exePath = Environment.ProcessPath;
            if (exePath != null)
                Process.Start(exePath);

            _lifetime.Shutdown();
        }
    }
}
