// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EveLens.Common.Constants;
using EveLens.Common.Data;
using EveLens.Common.Helpers;
using EveLens.Common.Net;
using EveLens.Common.Serialization.PatchXml;
using EveLens.Common.Services;
using EveLens.Common.CustomEventArgs;
using CommonEvents = EveLens.Common.Events;

namespace EveLens.Common
{
    /// <summary>
    /// Takes care of looking for new versions of EveLens and its datafiles.
    /// </summary>
    public static class UpdateManager
    {
        private static readonly TimeSpan s_frequency = TimeSpan.FromMinutes(Settings.Updates.
            UpdateFrequency);

        private static bool s_checkScheduled;
        private static bool s_enabled;
        private static int s_errorRetryCount;

        /// <summary>
        /// Gets or sets whether the autoupdater is enabled.
        /// </summary>
        public static bool Enabled
        {
            get { return s_enabled; }
            set
            {
                s_enabled = value;

                if (!s_enabled)
                    return;

                if (s_checkScheduled)
                    return;

                // Schedule a check in 10 seconds
                ScheduleCheck(TimeSpan.FromSeconds(10));
            }
        }

        /// <summary>
        /// Forces an immediate update check, regardless of the current schedule.
        /// Temporarily enables the update manager if it was disabled.
        /// </summary>
        public static void CheckNow()
        {
            s_enabled = true;
            s_checkScheduled = false;
            s_errorRetryCount = 0;
            ScheduleCheck(TimeSpan.Zero);
        }

        /// <summary>
        /// Deletes the installation files.
        /// </summary>
        public static void DeleteInstallationFiles()
        {
            foreach (string file in Directory.GetFiles(AppServices.ApplicationPaths.DataDirectory,
                "EveLens-install-*.exe", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    FileInfo installationFile = new FileInfo(file);
                    if (!installationFile.Exists)
                        continue;

                    FileHelper.DeleteFile(installationFile.FullName);
                }
                catch (UnauthorizedAccessException e)
                {
                    ExceptionHandler.LogException(e, false);
                }
            }
        }

        /// <summary>
        /// Deletes the data files.
        /// </summary>
        public static void DeleteDataFiles()
        {
            foreach (string file in Datafile.GetFilesFrom(AppServices.ApplicationPaths.DataDirectory,
                Datafile.DatafilesExtension).Concat(Datafile.GetFilesFrom(EveLensClient.
                EveLensDataDir, Datafile.OldDatafileExtension)))
            {
                try
                {
                    FileInfo dataFile = new FileInfo(file);
                    if (dataFile.Exists)
                        FileHelper.DeleteFile(dataFile.FullName);
                }
                catch (UnauthorizedAccessException e)
                {
                    ExceptionHandler.LogException(e, false);
                }
            }
        }

        /// <summary>
        /// Returns an exponentially increasing delay for error retries,
        /// capped at 60 minutes: 1, 2, 4, 8, 16, 32, 60.
        /// </summary>
        private static TimeSpan GetBackoffDelay()
        {
            int minutes = Math.Min(60, (int)Math.Pow(2, s_errorRetryCount));
            s_errorRetryCount++;
            return TimeSpan.FromMinutes(minutes);
        }

        /// <summary>
        /// Schedules a check a specified time period in the future.
        /// </summary>
        /// <param name="time">Time period in the future to start check.</param>
        private static void ScheduleCheck(TimeSpan time)
        {
            s_checkScheduled = true;
            AppServices.Dispatcher?.Schedule(time, () => _ = BeginCheckWithErrorHandlingAsync());
            AppServices.TraceService?.Trace("in " + time);
        }

        /// <summary>
        /// Wrapper that handles errors from the async update check.
        /// </summary>
        private static async Task BeginCheckWithErrorHandlingAsync()
        {
            try
            {
                await BeginCheckAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"UpdateManager - Error during update check: {ex.Message}",
                    printMethod: false);
                ExceptionHandler.LogException(ex, false);
                // Reschedule after error with exponential backoff
                ScheduleCheck(GetBackoffDelay());
            }
        }

        /// <summary>
        /// Performs the check asynchronously.
        /// </summary>
        /// <remarks>
        /// Invoked on the UI thread the first time and on some background thread the rest of the time.
        /// </remarks>
        private static async Task BeginCheckAsync()
        {
            // If update manager has been disabled since the last
            // update was triggered quit out here
            if (!s_enabled)
            {
                s_checkScheduled = false;
                return;
            }

            // No connection? Recheck in one minute
            if (!NetworkMonitor.IsNetworkAvailable)
            {
                AppServices.TraceService?.Trace("UpdateManager - No network available, rescheduling",
                    printMethod: false);
                ScheduleCheck(GetBackoffDelay());
                return;
            }

            // Determine update channel based on version type
            // Alpha/Beta users check their specific channel first, then fall back to stable
            string baseUpdatePath = NetworkConstants.EveLensUpdates; // /updates/evelens-patch.xml
            string channelSuffix = AppServices.IsAlphaVersion ? "-alpha"
                                 : AppServices.IsBetaVersion ? "-beta"
                                 : "";

            // For pre-release, use channel-specific patch file (e.g., patch-alpha.xml)
            // Also use branch-specific URL (alpha branch for alpha, beta branch for beta)
            string gitHubBase = NetworkConstants.GitHubBase;
            if (AppServices.IsAlphaVersion)
                gitHubBase = gitHubBase.Replace("/main", "/alpha");
            else if (AppServices.IsBetaVersion)
                gitHubBase = gitHubBase.Replace("/main", "/beta");

            string updateAddress = gitHubBase +
                (string.IsNullOrEmpty(channelSuffix)
                    ? baseUpdatePath
                    : baseUpdatePath.Replace(".xml", $"{channelSuffix}.xml"));
            string emergAddress = updateAddress.Replace(".xml", string.Empty) + "-emergency.xml";

            AppServices.TraceService?.Trace($"UpdateManager - Checking {updateAddress} (channel: {(string.IsNullOrEmpty(channelSuffix) ? "stable" : channelSuffix.TrimStart('-'))})", printMethod: false);

            // First look up for an emergency patch
            var result = await Util.DownloadXmlAsync<SerializablePatch>(new Uri(emergAddress))
                .ConfigureAwait(false);

            // If no emergency patch found, proceed with the regular patch file
            if (result.Error != null)
            {
                result = await Util.DownloadXmlAsync<SerializablePatch>(new Uri(updateAddress))
                    .ConfigureAwait(false);
            }

            // Process the result on the UI thread
            AppServices.Dispatcher?.Invoke(() => OnCheckCompleted(result));
        }

        /// <summary>
        /// Called when patch file check completed.
        /// </summary>
        /// <param name="result">The result.</param>
        private static void OnCheckCompleted(DownloadResult<SerializablePatch> result)
        {
            // If update manager has been disabled since the last
            // update was triggered quit out here
            if (!s_enabled)
            {
                s_checkScheduled = false;
                return;
            }

            // Was there an error ?
            if (result.Error != null)
            {
                // Logs the error and reschedule
                AppServices.TraceService?.Trace($"UpdateManager - {result.Error.Message}",
                    printMethod: false);
                ScheduleCheck(GetBackoffDelay());
                return;
            }

            try
            {
                // No error, let's try to deserialize
                if (result.Result != null)
                    ScanUpdateFeed(result.Result);
            }
            catch (InvalidOperationException exc)
            {
                // An error occurred during the deserialization
                ExceptionHandler.LogException(exc, true);
            }
            finally
            {
                // Reset backoff on successful check
                s_errorRetryCount = 0;

                AppServices.TraceService?.Trace((string)null);

                // Reschedule
                ScheduleCheck(s_frequency);
            }
        }

        /// <summary>
        /// Scans the update feed.
        /// </summary>
        /// <param name="result">The result.</param>
        private static void ScanUpdateFeed(SerializablePatch result)
        {
            string? fileVersion = AppServices.FileVersionInfo.FileVersion;
            if (fileVersion == null)
                return;

            Version currentVersion = Version.Parse(fileVersion);
            SerializableRelease? newestRelease = result.Releases?.FirstOrDefault(
                release => release.Version != null && Version.Parse(release.Version).Major == currentVersion.Major);

            Version newestVersion = (newestRelease?.Version != null) ? Version.Parse(newestRelease.
                Version) : currentVersion;
            Version mostRecentDeniedVersion = !string.IsNullOrEmpty(Settings.Updates.
                MostRecentDeniedUpgrade) ? new Version(Settings.Updates.
                MostRecentDeniedUpgrade) : new Version();

            // Is the program out of date and user has not previously denied this version?
            if (currentVersion < newestVersion && mostRecentDeniedVersion < newestVersion)
            {
                // Quit if newest release is null
                // (Shouldn't happen but it's nice to be prepared)
                if (newestRelease == null)
                    return;

                if (string.IsNullOrEmpty(newestRelease.TopicAddress) ||
                    string.IsNullOrEmpty(newestRelease.PatchAddress))
                    return;

                Uri forumUrl = new Uri(newestRelease.TopicAddress);
                Uri installerUrl = new Uri(newestRelease.PatchAddress);
                string? updateMessage = newestRelease.Message;
                string? installArgs = newestRelease.InstallerArgs;
                string? md5Sum = newestRelease.MD5Sum;
                string? additionalArgs = newestRelease.AdditionalArgs;
                bool canAutoInstall = !string.IsNullOrEmpty(installerUrl.AbsoluteUri) &&
                    !string.IsNullOrEmpty(installArgs);

                if (!string.IsNullOrEmpty(additionalArgs) && additionalArgs.Contains(
                    "%EVELENS_EXECUTABLE_PATH%"))
                {
                    string? appPath = AppContext.BaseDirectory;
                    installArgs = $"{installArgs} {additionalArgs}";
                    installArgs = installArgs.Replace("%EVELENS_EXECUTABLE_PATH%", appPath);
                }

                // Collect intermediate releases between current and newest (same major)
                var releaseHistory = new List<ReleaseSummary>();
                if (result.Releases != null)
                {
                    var intermediateReleases = result.Releases
                        .Where(r => r.Version != null)
                        .Select(r => new { Release = r, Ver = Version.Parse(r.Version!) })
                        .Where(r => r.Ver.Major == currentVersion.Major
                                    && r.Ver > currentVersion
                                    && r.Ver <= newestVersion)
                        .OrderByDescending(r => r.Ver);

                    foreach (var entry in intermediateReleases)
                    {
                        releaseHistory.Add(new ReleaseSummary(
                            entry.Ver,
                            entry.Release.Date ?? string.Empty,
                            entry.Release.Message ?? string.Empty));
                    }
                }

                // Requests a notification to subscribers and quit
                var updateArgs = new UpdateAvailableEventArgs(forumUrl, installerUrl, updateMessage,
                    currentVersion, newestVersion, md5Sum, canAutoInstall, installArgs, releaseHistory);
                AppServices.EventAggregator?.Publish(new CommonEvents.UpdateAvailableEvent(updateArgs));
                return;
            }

            // New data files released?
            if (result.FilesHaveChanged)
            {
                // Requests a notification to subscribers
                var dataUpdateArgs = new DataUpdateAvailableEventArgs(result.ChangedDatafiles);
                AppServices.EventAggregator?.Publish(new CommonEvents.DataUpdateAvailableEvent(dataUpdateArgs));
                return;
            }

            // Notify about a new major version
            Version newestMajorVersion = result.Releases?
                .Where(release => release.Version != null)
                .Max(release => Version.Parse(release.Version!)) ?? new Version();
            SerializableRelease? newestMajorRelease = result.Releases?.FirstOrDefault(release =>
                release.Version != null && Version.Parse(release.Version) == newestMajorVersion);
            if (newestMajorRelease?.Version == null)
                return;
            newestVersion = Version.Parse(newestMajorRelease.Version);
            Version mostRecentDeniedMajorUpgrade = !string.IsNullOrEmpty(Settings.Updates.
                MostRecentDeniedMajorUpgrade)
                ? new Version(Settings.Updates.MostRecentDeniedMajorUpgrade)
                : new Version();

            // Is there is a new major version and the user has not previously denied it?
            if (currentVersion >= newestVersion || mostRecentDeniedMajorUpgrade >= newestVersion)
                return;
            var majorUpdateArgs = new UpdateAvailableEventArgs(null, null, null, currentVersion,
                newestVersion, null, false, null);
            AppServices.EventAggregator?.Publish(new CommonEvents.UpdateAvailableEvent(majorUpdateArgs));
        }

        /// <summary>
        /// Replaces the datafile.
        /// </summary>
        /// <param name="oldFilename">The old filename.</param>
        /// <param name="newFilename">The new filename.</param>
        public static void ReplaceDatafile(string oldFilename, string newFilename)
        {
            try
            {
                FileHelper.DeleteFile($"{oldFilename}.bak");
                File.Copy(oldFilename, $"{oldFilename}.bak");
                FileHelper.DeleteFile(oldFilename);
                File.Move(newFilename, oldFilename);
            }
            catch (ArgumentException ex)
            {
                ExceptionHandler.LogException(ex, false);
            }
            catch (IOException ex)
            {
                ExceptionHandler.LogException(ex, false);
            }
            catch (UnauthorizedAccessException ex)
            {
                ExceptionHandler.LogException(ex, false);
            }
        }
    }
}
