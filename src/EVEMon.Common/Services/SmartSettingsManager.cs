// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EVEMon.Common.Helpers;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Smart settings manager with save coalescing and fork migration detection.
    /// Owns the full Export-Serialize-Write pipeline when the UseSmartSettings flag is ON.
    /// </summary>
    internal sealed class SmartSettingsManager : IDisposable
    {
        private readonly string _dataDirectory;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDispatcher _dispatcher;
        private readonly Func<SerializableSettings> _exportFunc;
        private readonly Timer _saveTimer;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private volatile bool _dirty;
        private volatile bool _disposed;
        private int _saveCallCount;
        private int _actualWriteCount;

        // Save coalescing interval
        internal const int SaveCoalesceIntervalMs = 10_000; // 10 seconds

        // Fork detection constants (must match Settings.cs)
        internal const string OurForkId = "aliacollins";
        internal const int PeterhaneveRevisionThreshold = 1000;

        /// <summary>
        /// Number of times <see cref="Save"/> was called.
        /// </summary>
        public int SaveCallCount => _saveCallCount;

        /// <summary>
        /// Number of actual file writes performed.
        /// </summary>
        public int ActualWriteCount => _actualWriteCount;

        /// <summary>
        /// Whether there are unsaved changes pending.
        /// </summary>
        public bool IsDirty => _dirty;

        /// <summary>
        /// Gets the write lock used to synchronize file operations.
        /// Callers can pass this to <see cref="SettingsFileManager.ClearAllJsonFiles"/>
        /// to prevent races with in-flight saves.
        /// </summary>
        internal SemaphoreSlim WriteLock => _writeLock;

        /// <summary>
        /// Creates a new SmartSettingsManager that owns the full save pipeline.
        /// </summary>
        /// <param name="dataDirectory">The EVEMon data directory.</param>
        /// <param name="eventAggregator">Event aggregator for publish/subscribe.</param>
        /// <param name="dispatcher">Dispatcher for UI thread marshaling.</param>
        /// <param name="exportFunc">Delegate that calls Settings.Export() on the UI thread.</param>
        public SmartSettingsManager(
            string dataDirectory,
            IEventAggregator eventAggregator,
            IDispatcher dispatcher,
            Func<SerializableSettings> exportFunc)
        {
            _dataDirectory = dataDirectory ?? throw new ArgumentNullException(nameof(dataDirectory));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _exportFunc = exportFunc ?? throw new ArgumentNullException(nameof(exportFunc));

            if (!Directory.Exists(_dataDirectory))
                Directory.CreateDirectory(_dataDirectory);

            // One-shot timer: fires once, then re-arms after callback completes.
            // This prevents overlapping callbacks if PerformSaveAsync takes longer than the interval.
            _saveTimer = new Timer(OnTimerElapsed, null, SaveCoalesceIntervalMs, Timeout.Infinite);

            // Flush any unsaved state if the process exits unexpectedly.
            // ProcessExit gives us ~2 seconds — enough for a synchronous save.
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        /// <summary>
        /// Best-effort flush of dirty settings when the process is exiting.
        /// </summary>
        private void OnProcessExit(object? sender, EventArgs e)
        {
            if (!_dirty || _disposed)
                return;

            try
            {
                _dirty = false;
                var settings = _exportFunc();
                if (settings != null)
                {
                    SettingsFileManager.SaveFromSerializableSettingsAsync(settings)
                        .GetAwaiter().GetResult(); // OK here — ProcessExit has ~2s
                }
            }
            catch
            {
                // Best effort — swallow all exceptions during shutdown
            }
        }

        /// <summary>
        /// Marks settings as dirty. The actual write will happen on the next timer tick.
        /// This is the main entry point for save coalescing.
        /// </summary>
        public void Save()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SmartSettingsManager));

            Interlocked.Increment(ref _saveCallCount);
            _dirty = true;
        }

        /// <summary>
        /// Immediately performs the full save pipeline, bypassing coalescing.
        /// Used for critical saves (shutdown, explicit user action).
        /// </summary>
        public async Task SaveImmediateAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SmartSettingsManager));

            _dirty = false;
            await PerformSaveAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Re-arms the one-shot timer for the next coalescing interval.
        /// </summary>
        private void RearmTimer()
        {
            if (!_disposed)
            {
                try
                {
                    _saveTimer.Change(SaveCoalesceIntervalMs, Timeout.Infinite);
                }
                catch (ObjectDisposedException)
                {
                    // Timer was disposed between the _disposed check and Change() call
                }
            }
        }

        /// <summary>
        /// Timer callback - checks dirty flag and performs save if needed.
        /// Uses one-shot pattern: timer fires once, callback re-arms after work completes.
        /// This guarantees no overlapping callbacks.
        /// </summary>
        private async void OnTimerElapsed(object state)
        {
            try
            {
                if (_disposed)
                    return;

                if (!_dirty)
                {
                    RearmTimer();
                    return;
                }

                _dirty = false;

                try
                {
                    await PerformSaveAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    ExceptionHandler.LogException(ex, false);
                    System.Diagnostics.Debug.WriteLine($"Async error in OnTimerElapsed: {ex}");
                    // Mark dirty again so it retries on next tick
                    _dirty = true;
                }
                finally
                {
                    RearmTimer();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Async error in OnTimerElapsed: {ex}");
            }
        }

        /// <summary>
        /// Core save pipeline: Export on UI thread, then write via SettingsFileManager on background.
        /// </summary>
        private async Task PerformSaveAsync()
        {
            // Step 1: Marshal Export() to the UI thread to get a snapshot of current settings.
            // Uses Post (non-blocking) + TaskCompletionSource to avoid blocking the thread pool
            // thread with Send(), which can deadlock if the UI thread is waiting on thread pool work.
            var tcs = new TaskCompletionSource<SerializableSettings>(TaskCreationOptions.RunContinuationsAsynchronously);
            _dispatcher.Post(() =>
            {
                try
                {
                    tcs.SetResult(_exportFunc());
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            SerializableSettings settings = await tcs.Task.ConfigureAwait(false);

            if (settings == null)
                return;

            // Step 2: Write to disk via SettingsFileManager (runs on background thread)
            await _writeLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await SettingsFileManager.SaveFromSerializableSettingsAsync(settings).ConfigureAwait(false);
                Interlocked.Increment(ref _actualWriteCount);
                _eventAggregator.Publish(new SettingsSavedEvent());
            }
            finally
            {
                _writeLock.Release();
            }
        }

        /// <summary>
        /// Writes a file atomically using a temp file and rename.
        /// </summary>
        internal static async Task WriteFileAtomicAsync(string filePath, string content)
        {
            string directory = Path.GetDirectoryName(filePath) ?? Path.GetTempPath();
            string tempPath = Path.Combine(directory, $".{Path.GetFileName(filePath)}.tmp");

            try
            {
                await File.WriteAllTextAsync(tempPath, content).ConfigureAwait(false);

                if (File.Exists(filePath))
                {
                    string backupPath = filePath + ".bak";
                    File.Replace(tempPath, filePath, backupPath);
                }
                else
                {
                    File.Move(tempPath, filePath);
                }
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); }
                    catch { /* Ignore cleanup errors */ }
                }
            }
        }

        #region Fork Migration Detection

        /// <summary>
        /// Result of fork migration detection.
        /// </summary>
        public sealed class MigrationDetectionResult
        {
            public bool MigrationDetected { get; set; }
            public bool NeedsForkIdAdded { get; set; }
            public bool HasEsiKeys { get; set; }
            public string DetectedForkId { get; set; }
            public int DetectedRevision { get; set; }
        }

        /// <summary>
        /// Detects if the settings file content is from another EVEMon fork.
        /// This is a pure function that can be tested without side effects.
        /// </summary>
        /// <param name="fileContent">The raw settings XML content.</param>
        /// <returns>Detection result.</returns>
        public static MigrationDetectionResult DetectForkMigration(string fileContent)
        {
            if (fileContent == null)
                throw new ArgumentNullException(nameof(fileContent));

            var result = new MigrationDetectionResult();

            // Check for forkId attribute in the Settings root element
            var forkIdMatch = Regex.Match(fileContent, @"<Settings[^>]*\sforkId=""([^""]+)""",
                RegexOptions.IgnoreCase);
            string forkId = forkIdMatch.Success ? forkIdMatch.Groups[1].Value : null;
            result.DetectedForkId = forkId;

            // Get revision number using the same regex pattern as Util.GetRevisionNumber
            int revision = ParseRevisionNumber(fileContent);
            result.DetectedRevision = revision;

            // Check if there are any ESI keys with refresh tokens
            var hasEsiKeys = Regex.IsMatch(fileContent, @"<esikey[^>]+refreshToken=""[^""]+""",
                RegexOptions.IgnoreCase);
            result.HasEsiKeys = hasEsiKeys;

            // Detection logic (mirrors Settings.cs lines 127-173):
            // 1. forkId == "aliacollins" -> Our user, no migration
            // 2. forkId present AND different -> Migration from that fork (if ESI keys present)
            // 3. forkId missing:
            //    - revision > 1000 -> peterhaneve user -> Migration (if ESI keys present)
            //    - revision <= 1000 -> Our existing user -> Just add forkId silently

            if (forkId == OurForkId)
            {
                // Case 1: Our fork with forkId - no migration needed
                result.MigrationDetected = false;
            }
            else if (forkId != null && forkId != OurForkId)
            {
                // Case 2: Different forkId explicitly set
                if (hasEsiKeys)
                {
                    result.MigrationDetected = true;
                }
                else
                {
                    // Different fork but no ESI keys - just update forkId
                    result.MigrationDetected = false;
                    result.NeedsForkIdAdded = true;
                }
            }
            else if (forkId == null)
            {
                // Case 3: forkId missing - use revision to distinguish
                if (revision > PeterhaneveRevisionThreshold && hasEsiKeys)
                {
                    // High revision = peterhaneve user
                    result.MigrationDetected = true;
                }
                else
                {
                    // Low revision = our existing user pre-forkId
                    result.MigrationDetected = false;
                    result.NeedsForkIdAdded = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Parses the revision number from XML content.
        /// Matches the logic in Util.GetRevisionNumber but operates on string content directly.
        /// </summary>
        internal static int ParseRevisionNumber(string content)
        {
            var match = Regex.Match(content, @"revision=""([0-9]+)""",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

            if (!match.Success || match.Groups.Count < 2)
                return -1;

            return int.TryParse(match.Groups[1].Value, out int revision) ? revision : -1;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            // Unregister ProcessExit handler to avoid double-flush
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;

            // Stop the timer — no new callbacks will fire
            _saveTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _saveTimer.Dispose();

            // Acquire the write lock to ensure no in-flight callback is mid-write,
            // then flush any pending dirty state, then release and dispose.
            try
            {
                _writeLock.Wait();
            }
            catch (ObjectDisposedException)
            {
                return; // Already disposed by another path
            }

            try
            {
                if (_dirty)
                {
                    _dirty = false;
                    try
                    {
                        // Export on UI thread + write — runs inside the lock so no race.
                        // Use Invoke here since Dispose is called from the UI thread
                        // (Settings.Shutdown → Dispose), so Invoke short-circuits to direct call.
                        SerializableSettings settings = null;
                        try
                        {
                            _dispatcher.Invoke(() => settings = _exportFunc());
                        }
                        catch
                        {
                            // If Invoke fails (e.g., dispatcher already shut down), skip
                        }

                        if (settings != null)
                        {
                            Task.Run(() => SettingsFileManager.SaveFromSerializableSettingsAsync(settings))
                                .GetAwaiter().GetResult();
                            Interlocked.Increment(ref _actualWriteCount);
                        }
                    }
                    catch
                    {
                        // Dispose must not throw — best-effort flush only
                    }
                }
            }
            finally
            {
                _writeLock.Release();
                _writeLock.Dispose();
            }
        }

        #endregion
    }

    /// <summary>
    /// Event published when settings are saved to disk.
    /// </summary>
    public sealed class SettingsSavedEvent
    {
        public DateTime SavedAt { get; } = DateTime.UtcNow;
    }
}
