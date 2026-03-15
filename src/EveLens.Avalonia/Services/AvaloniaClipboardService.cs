// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using EveLens.Core.Interfaces;

namespace EveLens.Avalonia.Services
{
    /// <summary>
    /// Avalonia implementation of <see cref="IClipboardService"/>.
    /// Uses Avalonia's clipboard API via TopLevel.
    /// Handles both UI-thread and background-thread callers without deadlocking.
    /// </summary>
    internal sealed class AvaloniaClipboardService : IClipboardService
    {
        // --- Async methods (preferred, safe on all platforms) ---

        public async Task SetTextAsync(string text)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                await Dispatcher.UIThread.InvokeAsync(() => SetTextAsync(text));
                return;
            }

            var clipboard = GetClipboard();
            if (clipboard != null)
                await clipboard.SetTextAsync(text);
        }

        public async Task<string?> GetTextAsync()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                return await Dispatcher.UIThread.InvokeAsync(() => GetTextAsync());
            }

            var clipboard = GetClipboard();
            return clipboard != null ? await clipboard.GetTextAsync() : null;
        }

        // --- Sync methods (Windows-only safe, kept for backward compatibility) ---

        public void SetText(string text)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                // On UI thread: fire-and-forget to avoid deadlock on Linux/macOS
                _ = SetTextAsync(text);
                return;
            }

            Dispatcher.UIThread.InvokeAsync(() => SetTextAsync(text)).Wait();
        }

        public string? GetText()
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                // On UI thread: cannot safely block — return null, callers should use GetTextAsync
                return null;
            }

            string? result = null;
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                result = await GetTextAsync();
            }).Wait();
            return result;
        }

        private static IClipboard? GetClipboard()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.MainWindow?.Clipboard;
            return null;
        }
    }
}
