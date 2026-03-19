// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Threading;
using System.Threading.Tasks;
using EveLens.Core.Interfaces;
using Velopack.Sources;
using VelopackUpdateManager = Velopack.UpdateManager;
using VelopackUpdateInfo = Velopack.UpdateInfo;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Velopack-based auto-update service. Replaces the custom AutoUpdateService + VelopackUpdateManager
    /// with Velopack's cross-platform delta update system.
    /// Channels (alpha/beta/stable) are handled automatically via GitHub Releases.
    /// </summary>
    public sealed class VelopackUpdateService : IDisposable
    {
        private const string GitHubRepoUrl = "https://github.com/aliacollins/evelens";
        private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(15);

        private readonly VelopackUpdateManager _manager;
        private readonly IEventAggregator? _eventAggregator;
        private readonly IDispatcher? _dispatcher;
        private CancellationTokenSource? _cts;
        private VelopackUpdateInfo? _pendingUpdate;
        private bool _disposed;

        /// <summary>Whether the Velopack VelopackUpdateManager reports this is an installed app (not portable/dev).</summary>
        public bool IsInstalled => _manager.IsInstalled;

        /// <summary>The current app version as reported by Velopack.</summary>
        public string? CurrentVersion => _manager.CurrentVersion?.ToString();

        /// <summary>The update channel this build belongs to (derived from version).</summary>
        public string Channel => CurrentVersion?.Contains("-alpha") == true ? "alpha"
            : CurrentVersion?.Contains("-beta") == true ? "beta" : "stable";

        /// <summary>
        /// Check interval based on channel: alpha=1h, beta=3h, stable=6h.
        /// More frequent for testers, less disruptive for production users.
        /// </summary>
        public TimeSpan CheckInterval => Channel switch
        {
            "alpha" => TimeSpan.FromHours(1),
            "beta" => TimeSpan.FromHours(3),
            _ => TimeSpan.FromHours(6)
        };

        /// <summary>Whether an update has been downloaded and is ready to apply.</summary>
        public bool IsUpdateReady => _pendingUpdate != null;

        /// <summary>Version string of the pending update, or null.</summary>
        public string? PendingVersion => _pendingUpdate?.TargetFullRelease?.Version?.ToString();

        /// <summary>Release notes (markdown) for the pending update, from the GitHub Release body.</summary>
        public string? PendingReleaseNotes => _pendingUpdate?.TargetFullRelease?.NotesMarkdown;

        public VelopackUpdateService(
            IEventAggregator? eventAggregator = null,
            IDispatcher? dispatcher = null)
        {
            _eventAggregator = eventAggregator;
            _dispatcher = dispatcher;

            var source = new GithubSource(GitHubRepoUrl, null, prerelease: true);
            _manager = new VelopackUpdateManager(source);
        }

        /// <summary>
        /// Starts the background update check loop. Call once at app startup.
        /// </summary>
        public void StartBackgroundChecks()
        {
            if (_disposed || !_manager.IsInstalled)
                return;

            _cts = new CancellationTokenSource();
            _ = BackgroundCheckLoop(_cts.Token);
        }

        /// <summary>
        /// Checks for updates immediately. Returns true if an update is available.
        /// </summary>
        public async Task<bool> CheckNowAsync()
        {
            try
            {
                if (!_manager.IsInstalled)
                {
                    AppServices.TraceService?.Trace("VelopackUpdate: Not installed (dev mode), skipping check");
                    return false;
                }

                AppServices.TraceService?.Trace("VelopackUpdate: Checking for updates...");
                var info = await _manager.CheckForUpdatesAsync().ConfigureAwait(false);

                if (info != null)
                {
                    AppServices.TraceService?.Trace(
                        $"VelopackUpdate: Update available: {info.TargetFullRelease?.Version}");
                    _pendingUpdate = info;

                    // Publish event so UI can show notification
                    PublishUpdateAvailable(info);
                    return true;
                }

                AppServices.TraceService?.Trace("VelopackUpdate: No updates available");
                return false;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"VelopackUpdate: Check failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Downloads the pending update with progress reporting.
        /// </summary>
        public async Task<bool> DownloadUpdateAsync(Action<int>? progress = null)
        {
            if (_pendingUpdate == null)
                return false;

            try
            {
                AppServices.TraceService?.Trace(
                    $"VelopackUpdate: Downloading {_pendingUpdate.TargetFullRelease?.Version}...");

                await _manager.DownloadUpdatesAsync(_pendingUpdate, progress).ConfigureAwait(false);

                AppServices.TraceService?.Trace("VelopackUpdate: Download complete");
                return true;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"VelopackUpdate: Download failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Applies the downloaded update and restarts the app.
        /// </summary>
        public void ApplyAndRestart()
        {
            if (_pendingUpdate?.TargetFullRelease == null)
                return;

            AppServices.TraceService?.Trace("VelopackUpdate: Applying update and restarting...");
            _manager.ApplyUpdatesAndRestart(_pendingUpdate.TargetFullRelease);
        }

        /// <summary>
        /// Applies the downloaded update on next app exit (no immediate restart).
        /// </summary>
        public void ApplyOnExit()
        {
            if (_pendingUpdate?.TargetFullRelease == null)
                return;

            AppServices.TraceService?.Trace("VelopackUpdate: Will apply update on exit");
            _manager.WaitExitThenApplyUpdates(_pendingUpdate.TargetFullRelease, silent: true);
        }

        private async Task BackgroundCheckLoop(CancellationToken ct)
        {
            try
            {
                // Wait before first check to let the app stabilize
                await Task.Delay(InitialDelay, ct).ConfigureAwait(false);

                while (!ct.IsCancellationRequested)
                {
                    await CheckNowAsync().ConfigureAwait(false);

                    // If update found and downloaded, auto-download in background
                    if (_pendingUpdate != null)
                    {
                        await DownloadUpdateAsync().ConfigureAwait(false);
                    }

                    await Task.Delay(CheckInterval, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"VelopackUpdate: Background loop error: {ex.Message}");
            }
        }

        private void PublishUpdateAvailable(VelopackUpdateInfo info)
        {
            var version = info.TargetFullRelease?.Version?.ToString() ?? "unknown";
            var notification = new Notifications.NotificationEventArgs(
                null, Notifications.NotificationCategory.QueryingError)
            {
                Description = $"EveLens {version} is available. Restart to update.",
                Behaviour = Notifications.NotificationBehaviour.Overwrite,
                Priority = Notifications.NotificationPriority.Information
            };

            if (_dispatcher != null)
                _dispatcher.Post(() => AppServices.Notifications?.Notify(notification));
            else
                AppServices.Notifications?.Notify(notification);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
