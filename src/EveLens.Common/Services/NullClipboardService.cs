// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Core.Interfaces;

namespace EveLens.Common.Services
{
    /// <summary>
    /// No-op fallback <see cref="IClipboardService"/>.
    /// In production, the UI layer (Avalonia) replaces this via <c>AppServices.SetClipboardService()</c>.
    /// </summary>
    internal sealed class NullClipboardService : IClipboardService
    {
        private string? _text;

        public void SetText(string text) => _text = text;

        public string? GetText() => _text;
    }
}
