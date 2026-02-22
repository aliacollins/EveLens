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
            App.IsExiting = true;
            _lifetime.Shutdown();
        }

        public void Restart()
        {
            App.IsExiting = true;
            string? exePath = Environment.ProcessPath;
            if (exePath != null)
            {
                // Pass --restart-delay so the new process waits for this instance
                // to fully exit and release the named semaphore before proceeding.
                Process.Start(new ProcessStartInfo(exePath)
                {
                    Arguments = "--restart-delay",
                    UseShellExecute = true
                });
            }

            _lifetime.Shutdown();
        }
    }
}
