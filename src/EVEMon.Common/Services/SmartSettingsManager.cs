using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Smart settings manager with save coalescing and fork migration detection.
    /// Replaces the save logic in Settings.cs with testable, coalesced I/O.
    /// </summary>
    internal sealed class SmartSettingsManager : IDisposable
    {
        private readonly string _dataDirectory;
        private readonly IEventAggregator _eventAggregator;
        private readonly Timer _saveTimer;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private volatile bool _dirty;
        private volatile bool _disposed;
        private int _saveCallCount;
        private int _actualWriteCount;

        // File names matching SettingsFileManager conventions
        private const string ConfigFileName = "config.json";
        private const string CredentialsFileName = "credentials.json";

        // Save coalescing interval
        internal const int SaveCoalesceIntervalMs = 10_000; // 10 seconds

        // Fork detection constants (must match Settings.cs)
        internal const string OurForkId = "aliacollins";
        internal const int PeterhaneveRevisionThreshold = 1000;

        // Current settings data (set via MarkDirty)
        private object _pendingConfig;
        private object _pendingCredentials;

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
        /// Path to config.json.
        /// </summary>
        public string ConfigFilePath => Path.Combine(_dataDirectory, ConfigFileName);

        /// <summary>
        /// Path to credentials.json.
        /// </summary>
        public string CredentialsFilePath => Path.Combine(_dataDirectory, CredentialsFileName);

        /// <summary>
        /// JSON serializer options matching SettingsFileManager.
        /// </summary>
        private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        public SmartSettingsManager(string dataDirectory, IEventAggregator eventAggregator)
        {
            _dataDirectory = dataDirectory ?? throw new ArgumentNullException(nameof(dataDirectory));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            if (!Directory.Exists(_dataDirectory))
                Directory.CreateDirectory(_dataDirectory);

            _saveTimer = new Timer(OnTimerElapsed, null, SaveCoalesceIntervalMs, SaveCoalesceIntervalMs);
        }

        /// <summary>
        /// Marks settings as dirty. The actual write will happen on the next timer tick.
        /// This is the main entry point for save coalescing.
        /// </summary>
        public void Save(object config = null, object credentials = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SmartSettingsManager));

            Interlocked.Increment(ref _saveCallCount);

            if (config != null)
                _pendingConfig = config;
            if (credentials != null)
                _pendingCredentials = credentials;

            _dirty = true;
        }

        /// <summary>
        /// Immediately writes pending data, bypassing save coalescing.
        /// Used for critical saves (shutdown, explicit user action).
        /// </summary>
        public async Task SaveImmediateAsync(object config = null, object credentials = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SmartSettingsManager));

            if (config != null)
                _pendingConfig = config;
            if (credentials != null)
                _pendingCredentials = credentials;

            _dirty = false;
            await FlushAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Timer callback - checks dirty flag and writes if needed.
        /// </summary>
        private async void OnTimerElapsed(object state)
        {
            if (!_dirty || _disposed)
                return;

            _dirty = false;

            try
            {
                await FlushAsync().ConfigureAwait(false);
            }
            catch
            {
                // Mark dirty again so it retries on next tick
                _dirty = true;
            }
        }

        /// <summary>
        /// Performs the actual file writes with thread safety.
        /// </summary>
        private async Task FlushAsync()
        {
            await _writeLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_pendingConfig != null)
                {
                    string json = JsonSerializer.Serialize(_pendingConfig, s_jsonOptions);
                    await WriteFileAtomicAsync(ConfigFilePath, json).ConfigureAwait(false);
                }

                if (_pendingCredentials != null)
                {
                    string json = JsonSerializer.Serialize(_pendingCredentials, s_jsonOptions);
                    await WriteFileAtomicAsync(CredentialsFilePath, json).ConfigureAwait(false);
                }

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
                    File.Delete(filePath);

                File.Move(tempPath, filePath);
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

            // Stop the timer
            _saveTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _saveTimer.Dispose();

            // Flush any pending saves synchronously
            if (_dirty)
            {
                _dirty = false;
                FlushAsync().GetAwaiter().GetResult();
            }

            _writeLock.Dispose();
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
