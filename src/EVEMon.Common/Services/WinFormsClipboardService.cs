// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Windows.Forms;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// WinForms implementation of <see cref="IClipboardService"/>.
    /// Wraps <see cref="System.Windows.Forms.Clipboard"/>.
    /// </summary>
    internal sealed class WinFormsClipboardService : IClipboardService
    {
        public void SetText(string text)
        {
            Clipboard.SetText(text, TextDataFormat.Text);
        }

        public string? GetText()
        {
            return Clipboard.ContainsText() ? Clipboard.GetText() : null;
        }
    }
}
