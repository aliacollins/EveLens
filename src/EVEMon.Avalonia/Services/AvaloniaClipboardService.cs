using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using EVEMon.Core.Interfaces;

namespace EVEMon.Avalonia.Services
{
    /// <summary>
    /// Avalonia implementation of <see cref="IClipboardService"/>.
    /// Uses Avalonia's clipboard API via TopLevel.
    /// Handles both UI-thread and background-thread callers without deadlocking.
    /// </summary>
    internal sealed class AvaloniaClipboardService : IClipboardService
    {
        public void SetText(string text)
        {
            RunOnUIThread(async () =>
            {
                var clipboard = GetClipboard();
                if (clipboard != null)
                    await clipboard.SetTextAsync(text);
                return true;
            });
        }

        public string? GetText()
        {
            return RunOnUIThread(async () =>
            {
                var clipboard = GetClipboard();
                return clipboard != null ? await clipboard.GetTextAsync() : null;
            });
        }

        private static T RunOnUIThread<T>(Func<Task<T>> asyncFunc)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                return asyncFunc().GetAwaiter().GetResult();
            }

            T result = default!;
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                result = await asyncFunc();
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
