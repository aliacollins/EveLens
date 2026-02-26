// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Core.Enumerations;
using EveLens.Core.Interfaces;

namespace EveLens.Common.Services
{
    /// <summary>
    /// No-op fallback <see cref="IDialogService"/>. Returns default values for all dialogs.
    /// In production, the UI layer (Avalonia) replaces this via <c>AppServices.SetDialogService()</c>.
    /// </summary>
    internal sealed class NullDialogService : IDialogService
    {
        public DialogChoice ShowMessage(string text, string caption,
            DialogButtons buttons = DialogButtons.OK,
            DialogIcon icon = DialogIcon.None)
            => DialogChoice.OK;

        public string? ShowSaveDialog(string title, string filter,
            string? defaultFileName = null, string? initialDirectory = null)
            => null;

        public string? ShowOpenDialog(string title, string filter,
            string? initialDirectory = null)
            => null;

        public string? ShowFolderBrowser(string description, string? selectedPath = null)
            => null;
    }
}
