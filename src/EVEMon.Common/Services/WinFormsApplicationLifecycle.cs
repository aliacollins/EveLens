// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Windows.Forms;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// WinForms implementation of <see cref="IApplicationLifecycle"/>.
    /// Wraps <see cref="Application.Exit()"/> and <see cref="Application.Restart()"/>.
    /// </summary>
    internal sealed class WinFormsApplicationLifecycle : IApplicationLifecycle
    {
        public void Exit()
        {
            Application.Exit();
        }

        public void Restart()
        {
            Application.Restart();
        }
    }
}
