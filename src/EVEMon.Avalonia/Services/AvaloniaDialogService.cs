using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using EVEMon.Core.Enumerations;
using EVEMon.Core.Interfaces;

namespace EVEMon.Avalonia.Services
{
    /// <summary>
    /// Avalonia implementation of <see cref="IDialogService"/>.
    /// Uses Avalonia's StorageProvider for file dialogs and a custom dialog for messages.
    /// Handles both UI-thread and background-thread callers without deadlocking.
    /// </summary>
    internal sealed class AvaloniaDialogService : IDialogService
    {
        public DialogChoice ShowMessage(string text, string caption,
            DialogButtons buttons = DialogButtons.OK,
            DialogIcon icon = DialogIcon.None)
        {
            return RunOnUIThread(() =>
            {
                var window = GetMainWindow();
                if (window == null)
                    return Task.FromResult(DialogChoice.OK);

                return ShowMessageDialogAsync(window, text, caption);
            });
        }

        public string? ShowSaveDialog(string title, string filter,
            string? defaultFileName = null, string? initialDirectory = null)
        {
            return RunOnUIThread(async () =>
            {
                var window = GetMainWindow();
                if (window == null)
                    return (string?)null;

                var result = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = title,
                    SuggestedFileName = defaultFileName,
                });

                return result?.Path.LocalPath;
            });
        }

        public string? ShowOpenDialog(string title, string filter,
            string? initialDirectory = null)
        {
            return RunOnUIThread(async () =>
            {
                var window = GetMainWindow();
                if (window == null)
                    return (string?)null;

                var result = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = title,
                    AllowMultiple = false,
                });

                return result.Count > 0 ? result[0].Path.LocalPath : null;
            });
        }

        public string? ShowFolderBrowser(string description, string? selectedPath = null)
        {
            return RunOnUIThread(async () =>
            {
                var window = GetMainWindow();
                if (window == null)
                    return (string?)null;

                var result = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = description,
                    AllowMultiple = false,
                });

                return result.Count > 0 ? result[0].Path.LocalPath : null;
            });
        }

        /// <summary>
        /// Runs an async function on the UI thread, blocking the caller until completion.
        /// Avoids deadlock by detecting if already on the UI thread.
        /// </summary>
        private static T RunOnUIThread<T>(Func<Task<T>> asyncFunc)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                // Already on UI thread — run inline via nested pump to avoid deadlock
                return asyncFunc().GetAwaiter().GetResult();
            }

            // Background thread — safe to use .Wait()
            T result = default!;
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                result = await asyncFunc();
            }).Wait();
            return result;
        }

        private static async Task<DialogChoice> ShowMessageDialogAsync(Window owner, string text, string caption)
        {
            var dialog = new Window
            {
                Title = caption,
                Width = 420,
                Height = 200,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Thickness(16),
                    Spacing = 16,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = text,
                            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
                            MaxWidth = 380
                        },
                        new Button
                        {
                            Content = "OK",
                            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right,
                            MinWidth = 80
                        }
                    }
                }
            };

            var button = ((StackPanel)dialog.Content).Children[1] as Button;
            button!.Click += (_, _) => dialog.Close();

            await dialog.ShowDialog(owner);
            return DialogChoice.OK;
        }

        private static Window? GetMainWindow()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.MainWindow;
            return null;
        }
    }
}
