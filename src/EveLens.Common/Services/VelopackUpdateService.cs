// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Threading;
using System.Threading.Tasks;
using EveLens.Common.Extensions;
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

        // Lazy because the UpdateManager constructor demands VelopackLocator.Current,
        // which only exists once the startup hook has run — constructing this service
        // (in tests, or before the hook) must not depend on that global being set.
        private readonly Lazy<VelopackUpdateManager> _manager;
        private readonly VelopackTraceLogger _velopackLog = AppServices.VelopackLogger;
        private readonly IEventAggregator? _eventAggregator;
        private readonly IDispatcher? _dispatcher;
        private CancellationTokenSource? _cts;
        private VelopackUpdateInfo? _pendingUpdate;
        private GitHubReleaseInfo? _pendingGitHub;
        private bool _disposed;

        /// <summary>Whether the Velopack VelopackUpdateManager reports this is an installed app (not portable/dev).</summary>
        public bool IsInstalled => _manager.Value.IsInstalled;

        /// <summary>The current app version: Velopack's when installed through it,
        /// otherwise the informational version (which carries the -beta/-alpha
        /// channel; the numeric file version does not and misclassifies builds).</summary>
        public string? CurrentVersion => _manager.Value.CurrentVersion?.ToString()
            ?? AppServices.AppVersion.ProductVersion;

        /// <summary>
        /// The hand-packaged platforms: the macOS .app and Linux archives are not
        /// Velopack installs, so Velopack sits inert there. Updates on these
        /// platforms mean "tell the user and open the release page" — an archive
        /// cannot swap itself in place — via <see cref="GitHubReleaseChecker"/>.
        /// </summary>
        public bool UsesGitHubFallback =>
            !_manager.Value.IsInstalled &&
            (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux());

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

        /// <summary>Whether an update is pending (downloaded, or found on GitHub).</summary>
        public bool IsUpdateReady => _pendingUpdate != null || _pendingGitHub != null;

        /// <summary>Version string of the pending update, or null.</summary>
        public string? PendingVersion => _pendingUpdate?.TargetFullRelease?.Version?.ToString()
            ?? _pendingGitHub?.Version;

        /// <summary>Release notes (markdown) for the pending update, from the GitHub Release body.</summary>
        public string? PendingReleaseNotes => _pendingUpdate?.TargetFullRelease?.NotesMarkdown
            ?? _pendingGitHub?.NotesMarkdown;

        /// <summary>Release page for a GitHub-fallback update, or null on Velopack
        /// installs (which download and apply themselves).</summary>
        public string? PendingUrl => _pendingGitHub?.Url;

        public VelopackUpdateService(
            IEventAggregator? eventAggregator = null,
            IDispatcher? dispatcher = null)
        {
            _eventAggregator = eventAggregator;
            _dispatcher = dispatcher;

            var source = new GithubSource(GitHubRepoUrl, null, prerelease: true);

            _manager = new Lazy<VelopackUpdateManager>(
                () => new VelopackUpdateManager(source));

            // Velopack routes its logging through the locator the startup hook
            // installed. Attach there if that hook did not run (tests, dev builds) so
            // the account of an update is never silently discarded — that missing
            // account is what made a failed macOS in-place update undiagnosable.
            if (!Velopack.Locators.VelopackLocator.IsCurrentSet)
                AppServices.TraceService?.Trace(
                    "VelopackUpdate: no Velopack locator installed — update logging unavailable");
        }

        /// <summary>
        /// Why the last download or apply failed, or null if nothing has failed.
        /// Shown to the user rather than swallowed, so an update that cannot install
        /// says so instead of leaving the app quietly on the old version.
        /// </summary>
        public string? LastError { get; private set; }

        /// <summary>The retained tail of Velopack's own log, for failure reports.</summary>
        public string UpdateLogTail() => _velopackLog.RecentLog();

        /// <summary>
        /// Starts the background update check loop. Call once at app startup.
        /// </summary>
        public void StartBackgroundChecks()
        {
            if (_disposed || (!_manager.Value.IsInstalled && !UsesGitHubFallback))
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
                if (!_manager.Value.IsInstalled)
                {
                    if (!UsesGitHubFallback)
                    {
                        AppServices.TraceService?.Trace(
                            "VelopackUpdate: Not installed (dev mode), skipping check");
                        return false;
                    }
                    GitHubReleaseInfo? gh = await GitHubReleaseChecker
                        .CheckAsync(CurrentVersion).ConfigureAwait(false);
                    if (gh == null)
                    {
                        AppServices.TraceService?.Trace(
                            "VelopackUpdate: GitHub check — no newer release");
                        return false;
                    }
                    AppServices.TraceService?.Trace(
                        $"VelopackUpdate: GitHub release available: {gh.Version}");
                    _pendingGitHub = gh;
                    PublishGitHubUpdateAvailable(gh);
                    return true;
                }

                AppServices.TraceService?.Trace("VelopackUpdate: Checking for updates...");
                var info = await _manager.Value.CheckForUpdatesAsync().ConfigureAwait(false);

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
            {
                LastError = "No update has been found to download.";
                return false;
            }

            try
            {
                LastError = null;
                AppServices.TraceService?.Trace(
                    $"VelopackUpdate: Downloading {_pendingUpdate.TargetFullRelease?.Version}...");

                await _manager.Value.DownloadUpdatesAsync(_pendingUpdate, progress).ConfigureAwait(false);

                AppServices.TraceService?.Trace("VelopackUpdate: Download complete");
                return true;
            }
            catch (Exception ex)
            {
                // Redacted: exception messages and stack traces carry absolute paths
                // (and thus the OS account name), and both this trace line and LastError
                // end up in places users share — the trace log and the failure dialog.
                LastError = ex.Message.RedactUserName();
                AppServices.TraceService?.Trace(
                    $"VelopackUpdate: Download failed: {ex.ToString().RedactUserName()}");
                return false;
            }
        }

        /// <summary>
        /// Applies the downloaded update and restarts the app.
        /// </summary>
        /// <returns>
        /// Never returns on success — Velopack hands off to the updater and exits the
        /// process. A return of false therefore means the handoff itself failed, and
        /// <see cref="LastError"/> says why.
        /// </returns>
        public bool ApplyAndRestart()
        {
            // Pre-flight: a translocated app runs from a read-only Gatekeeper mount, so
            // the apply is doomed — and it fails inside the spawned updater AFTER this
            // process has exited, where no catch block can ever see it. The only honest
            // move is to refuse up front. The downloaded package stays staged: Velopack
            // auto-applies it at next launch once the install is healed.
            if (AppServices.MacInstall.IsTranslocated)
            {
                LastError = Loc.Get("MacInstall.UpdateRefused");
                AppServices.TraceService?.Trace(
                    "VelopackUpdate: refused apply — app is running from an App Translocation mount");
                return false;
            }

            if (_pendingUpdate?.TargetFullRelease == null)
            {
                LastError = "No downloaded update is ready to apply.";
                return false;
            }

            try
            {
                LastError = null;
                AppServices.TraceService?.Trace("VelopackUpdate: Applying update and restarting...");
                _manager.Value.ApplyUpdatesAndRestart(_pendingUpdate.TargetFullRelease);
                return true;
            }
            catch (Exception ex)
            {
                // Reaching here means the updater never took over: it could not be
                // launched, or refused the package. Silence here is what left macOS
                // users staring at an unchanged version number. LastProblem is already
                // redacted by VelopackTraceLogger; the raw exception is not.
                LastError = _velopackLog.LastProblem ?? ex.Message.RedactUserName();
                AppServices.TraceService?.Trace(
                    $"VelopackUpdate: Apply failed: {ex.ToString().RedactUserName()}");
                return false;
            }
        }

        /// <summary>
        /// Applies the downloaded update on next app exit (no immediate restart).
        /// </summary>
        public void ApplyOnExit()
        {
            if (_pendingUpdate?.TargetFullRelease == null)
                return;

            AppServices.TraceService?.Trace("VelopackUpdate: Will apply update on exit");
            _manager.Value.WaitExitThenApplyUpdates(_pendingUpdate.TargetFullRelease, silent: true);
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
                        bool downloaded = await DownloadUpdateAsync().ConfigureAwait(false);

                        // Auto-install only when the user explicitly opted in (Discussion #100).
                        // The opt-in is asked once at startup and editable in Settings > Data —
                        // transparency first, EveLens never silently decides this.
                        if (downloaded &&
                            Settings.Updates.AutoInstallUpdates ==
                                Enumerations.UISettings.AutoInstallUpdates.Automatic)
                        {
                            AppServices.TraceService?.Trace(
                                "VelopackUpdate: auto-install opted in — will apply on exit");
                            ApplyOnExit();
                        }
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

        private void PublishGitHubUpdateAvailable(GitHubReleaseInfo info)
        {
            var notification = new Notifications.NotificationEventArgs(
                null, Notifications.NotificationCategory.QueryingError)
            {
                // An archive cannot swap itself in place, so the honest promise
                // is a download, not a restart.
                Description =
                    $"EveLens {info.Version} is available. Help > Check for Updates to download.",
                Behaviour = Notifications.NotificationBehaviour.Overwrite,
                Priority = Notifications.NotificationPriority.Information
            };
            if (_dispatcher != null)
                _dispatcher.Post(() => AppServices.Notifications?.Notify(notification));
            else
                AppServices.Notifications?.Notify(notification);
        }

        private void PublishUpdateAvailable(VelopackUpdateInfo info)
        {
            var version = info.TargetFullRelease?.Version?.ToString() ?? "unknown";
            bool autoInstall = Settings.Updates.AutoInstallUpdates ==
                Enumerations.UISettings.AutoInstallUpdates.Automatic;
            var notification = new Notifications.NotificationEventArgs(
                null, Notifications.NotificationCategory.QueryingError)
            {
                // Wording matches what will actually happen — silent-on-exit vs manual restart
                Description = autoInstall
                    ? $"EveLens {version} downloaded. It installs when you close the app."
                    : $"EveLens {version} is available. Restart to update.",
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
