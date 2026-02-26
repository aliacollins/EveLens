// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using EveLens.Common.CustomEventArgs;
using EveLens.Common.Serialization.PatchXml;
using EveLens.Common.Services;

namespace EveLens.Avalonia.Views.Dialogs
{
    public partial class DataUpdateNotifyWindow : Window
    {
        private DataUpdateAvailableEventArgs? _args;

        /// <summary>
        /// Gets whether the user chose to update.
        /// </summary>
        public bool UpdateRequested { get; private set; }

        public DataUpdateNotifyWindow()
        {
            InitializeComponent();

            UpdateBtn.Click += OnUpdateClick;
            RemindBtn.Click += (_, _) => Close();
        }

        public void Initialize(DataUpdateAvailableEventArgs args)
        {
            _args = args;

            var fileNames = args.ChangedFiles
                .Select(f => f.Name ?? "Unknown file")
                .ToList();

            FilesList.ItemsSource = fileNames;
        }

        private async void OnUpdateClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_args == null) return;

                UpdateBtn.IsEnabled = false;
                UpdateBtn.Content = "Downloading...";
                UpdateRequested = true;

                bool success = await DownloadAndReplaceFilesAsync();

                if (success)
                {
                    // Restart the application
                    try
                    {
                        var exePath = Environment.ProcessPath;
                        if (exePath != null)
                        {
                            Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });
                        }
                        AppServices.ApplicationLifecycle.Exit();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error restarting app: {ex}");
                    }
                }
                else
                {
                    UpdateBtn.Content = "Update Failed";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initiating data update: {ex}");
                UpdateBtn.Content = "Update Failed";
            }
        }

        private async Task<bool> DownloadAndReplaceFilesAsync()
        {
            if (_args == null) return false;

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(60);
            int downloaded = 0;

            foreach (SerializableDatafile datafile in _args.ChangedFiles)
            {
                try
                {
                    if (string.IsNullOrEmpty(datafile.Address) || string.IsNullOrEmpty(datafile.Name))
                        continue;

                    string url = $"{datafile.Address}/{datafile.Name}";
                    string destPath = Path.Combine(
                        AppServices.ApplicationPaths.DataDirectory, datafile.Name);
                    string tempPath = $"{destPath}.tmp";

                    // Download to temp file
                    var data = await httpClient.GetByteArrayAsync(url);
                    await File.WriteAllBytesAsync(tempPath, data);

                    // Replace original with downloaded file
                    Common.UpdateManager.ReplaceDatafile(destPath, tempPath);
                    downloaded++;

                    await Dispatcher.UIThread.InvokeAsync(() =>
                        UpdateBtn.Content = $"Downloaded {downloaded}/{_args.ChangedFiles.Count}...");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error downloading {datafile.Name}: {ex}");
                }
            }

            return downloaded > 0;
        }
    }
}
